using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_RoleChange))]
    [HarmonyPatch("Apply")]
    public static class Patch_RitualOutcomeEffectWorker_RoleChange
    {
        static void Postfix(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            var roleChangeSelection = jobRitual.assignments?.RoleChangeSelection;
            if (roleChangeSelection == null) return;

            var pawn = jobRitual.assignments.FirstAssignedPawn("role_changer");
            if (pawn == null) return;

            bool isReligion = PresetReligions.CreatedReligionIdeos.Contains(roleChangeSelection.ideo);

            // Use our custom assignment for BOTH ideology and religion
            IdeoRoleManager.AssignRole(pawn, roleChangeSelection, isReligion);
        }
    }
}
