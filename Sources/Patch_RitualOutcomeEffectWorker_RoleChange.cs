using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_RoleChange))]
    [HarmonyPatch("Apply")]
    public static class Patch_RitualOutcomeEffectWorker_RoleChange
    {
        static bool Prefix(RitualOutcomeEffectWorker_RoleChange __instance,
            float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            var roleChangeSelection = jobRitual.assignments?.RoleChangeSelection;

            var pawn = jobRitual.assignments?.FirstAssignedPawn("role_changer");
            if (pawn == null) return true;

            // Handle religion role REMOVAL (roleChangeSelection == null means "remove")
            if (roleChangeSelection == null)
            {
                var currentReligionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);
                if (currentReligionRole == null) return true; // No religion role — let vanilla handle ideology removal

                IdeoRoleManager.UnassignRole(pawn, isReligion: true);
                SendOutcomeLetter(__instance, jobRitual);

                Messages.Message(
                    pawn.LabelShortCap + " has been removed from the " + currentReligionRole.LabelForPawn(pawn) + " role.",
                    pawn, MessageTypeDefOf.NeutralEvent);

                return false; // Block vanilla — we handled it
            }

            bool isReligion = PresetReligions.CreatedReligionIdeos.Contains(roleChangeSelection.ideo);
            if (!isReligion) return true;

            // Religion role ASSIGNMENT
            SendOutcomeLetter(__instance, jobRitual);

            RitualOutcomeComp outcomeComp = __instance.def.comps.First();
            float outcomeQuality = outcomeComp.QualityOffset(jobRitual, __instance.DataForComp(outcomeComp));

            if (outcomeQuality > 0.5f)
            {
                var currentRole = IdeoRoleManager.GetRole(pawn, isReligion: true);
                if (currentRole != null)
                    IdeoRoleManager.UnassignRole(pawn, isReligion: true);
                IdeoRoleManager.AssignRole(pawn, roleChangeSelection, isReligion: true);
            }

            return false;
        }

        static void Postfix(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            var roleChangeSelection = jobRitual.assignments?.RoleChangeSelection;
            var pawn = jobRitual.assignments?.FirstAssignedPawn("role_changer");
            if (pawn == null) return;

            if (roleChangeSelection == null)
            {
                if (IdeoRoleManager.HasRole(pawn, isReligion: false))
                    IdeoRoleManager.UnassignRole(pawn, isReligion: false);
                return;
            }

            if (!PresetReligions.CreatedReligionIdeos.Contains(roleChangeSelection.ideo))
                IdeoRoleManager.AssignRole(pawn, roleChangeSelection, isReligion: false);
        }

        private static void SendOutcomeLetter(RitualOutcomeEffectWorker_RoleChange worker, LordJob_Ritual jobRitual)
        {
            RitualOutcomeComp comp = worker.def.comps.First();
            float quality = comp.QualityOffset(jobRitual, worker.DataForComp(comp));
            bool success = quality > 0.5f;
            RitualOutcomePossibility outcome = success ? worker.def.outcomeChances[1] : worker.def.outcomeChances[0];

            string text = outcome.description.Formatted(jobRitual.Ritual.Label,
                jobRitual.Ritual.ideo.Named("IDEO")).CapitalizeFirst();
            text += "\n\n" + "RitualOutcomeQualitySpecific".Translate(jobRitual.Ritual.Label,
                quality.ToStringPercent()).CapitalizeFirst() + ":\n";
            text += "\n  - " + comp.GetDesc(jobRitual, worker.DataForComp(comp)).CapitalizeFirst();

            Find.LetterStack.ReceiveLetter(
                "OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"),
                    jobRitual.Ritual.Label.Named("RITUALLABEL")),
                text,
                outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative,
                jobRitual.selectedTarget);
        }
    }

    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_RoleChange))]
    [HarmonyPatch("BlockingIssues")]
    public static class Patch_RitualOutcomeEffectWorker_RoleChange_BlockingIssues
    {
        static void Postfix(ref IEnumerable<string> __result, RitualRoleAssignments assignments)
        {
            var pawn = assignments.FirstAssignedPawn("role_changer");
            if (pawn == null) return;

            if (assignments.RoleChangeSelection == null && IdeoRoleManager.HasRole(pawn, isReligion: true))
            {
                var sameRoleMsg = "MessageRoleChangeChooseDifferentRole".Translate(pawn.Named("PAWN"));
                __result = __result.Where(issue => issue != sameRoleMsg);
            }
        }
    }

    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_RoleChange))]
    [HarmonyPatch("Apply")]
    public static class Patch_RitualOutcomeEffectWorker_RoleChange_Validation
    {
        static void Postfix()
        {
            int fixes = IdeoRoleManager.ValidateAndFix();
            if (fixes > 0)
                Log.Message($"[IdeoRework] Role Validation: Fixed {fixes} role(s) after ritual");
        }
    }
}
