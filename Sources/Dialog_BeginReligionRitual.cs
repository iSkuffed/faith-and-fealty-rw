using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace IdeoRework
{
    public class Dialog_BeginReligionRitual : Dialog_BeginRitual
    {
        public Dialog_BeginReligionRitual(Precept_Ritual ritual, TargetInfo targetInfo,
            RitualObligation obligation, Action onConfirm, Pawn organizer,
            Dictionary<string, Pawn> forcedForRole, Pawn selectedPawn)
            : base(
                ritual?.Label?.CapitalizeFirst() ?? "Ritual",
                ritual,
                targetInfo,
                targetInfo.Map,
                CreateAction(targetInfo, ritual, organizer, obligation, onConfirm),
                organizer,
                obligation,
                CreateFilter(ritual, targetInfo, forcedForRole),
                "Begin".Translate(),
                organizer != null ? new List<Pawn> { organizer } : null,
                forcedForRole,
                null,
                CreateExtraInfo(ritual, targetInfo),
                selectedPawn
            )
        {
            // Fix: Add religion's leader role to cachedRoles 
            // Vanilla filters out leader roles from the ritual's ideo,
            // then only adds the ideology's leader role.
            // We need to add back the religion's leader role. 2 Kings 17:15
            var cachedRolesField = AccessTools.Field(typeof(Dialog_BeginRitual), "cachedRoles");
            var cachedRoles = cachedRolesField?.GetValue(this) as List<Precept_Role>;
            if (cachedRoles != null && ritual?.ideo != null)
            {
                var religionLeaderRole = ritual.ideo.RolesListForReading
                    .FirstOrDefault(r => r.def.leaderRole);
                if (religionLeaderRole != null && !cachedRoles.Contains(religionLeaderRole))
                    cachedRoles.Add(religionLeaderRole);

                cachedRoles.SortBy(r => r.def.displayOrderInImpact);
            }
        }

        protected override IEnumerable<string> BlockingIssues()
        {
            foreach (var issue in base.BlockingIssues())
            {
                if (ritual?.ideo != null && issue.Contains(ritual.ideo.memberName))
                    continue;
                yield return issue;
            }
        }

        private static ActionCallback CreateAction(TargetInfo targetInfo, Precept_Ritual ritual,
            Pawn organizer, RitualObligation obligation, Action onConfirm)
        {
            return delegate (RitualRoleAssignments assignments)
            {
                if (ritual?.behavior != null)
                    ritual.behavior.TryExecuteOn(targetInfo, organizer, ritual, obligation, assignments, playerForced: true);
                onConfirm?.Invoke();
                return true;
            };
        }

        private static PawnFilter CreateFilter(Precept_Ritual ritual, TargetInfo targetInfo,
            Dictionary<string, Pawn> forcedForRole)
        {
            return delegate (Pawn pawn, bool voluntary, bool allowOtherIdeos)
            {
                if (pawn == null) return false;
                if (pawn.GetLord() != null)
                    return false;
                if (pawn.IsSubhuman)
                    return false;

                var roles = ritual?.behavior?.def?.roles;
                if (pawn.RaceProps.Animal && roles != null && !roles.Any(r =>
                {
                    try { return r.AppliesToPawn(pawn, out var _, targetInfo, null, null, null, skipReason: true); }
                    catch { return false; }
                }))
                    return false;

                if (ritual == null) return true;
                if (!ritual.ritualOnlyForIdeoMembers || ritual.def.allowSpectatorsFromOtherIdeos)
                    return true;
                if (pawn.Ideo == ritual.ideo || pawn.GetReligionIdeo() == ritual.ideo)
                    return true;
                if (!voluntary || allowOtherIdeos)
                    return true;
                if (pawn.IsPrisonerOfColony || pawn.RaceProps.Animal)
                    return true;
                if (!forcedForRole.NullOrEmpty() && forcedForRole.ContainsValue(pawn))
                    return true;
                return false;
            };
        }

        private static List<string> CreateExtraInfo(Precept_Ritual ritual, TargetInfo targetInfo)
        {
            var list = new List<string>();
            if (ritual?.outcomeEffect?.def == null) return list;

            if (!ritual.outcomeEffect.def.extraInfoLines.NullOrEmpty())
            {
                foreach (var line in ritual.outcomeEffect.def.extraInfoLines)
                    list.Add(line);
            }
            if (!ritual.outcomeEffect.def.extraPredictedOutcomeDescriptions.NullOrEmpty())
            {
                foreach (var desc in ritual.outcomeEffect.def.extraPredictedOutcomeDescriptions)
                    list.Add(desc.Formatted(ritual.shortDescOverride ?? ritual.def?.label ?? "Ritual"));
            }
            if (ritual.attachableOutcomeEffect != null && targetInfo.Map != null)
            {
                try { list.Add(ritual.attachableOutcomeEffect.DescriptionForRitualValidated(ritual, targetInfo.Map)); }
                catch { }
            }
            return list;
        }
    }
}
