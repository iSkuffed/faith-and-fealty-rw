using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(SocialCardUtility))]
    [HarmonyPatch("DrawPawnRoleSelection")]
    public static class Patch_SocialCardUtility_DrawPawnRoleSelection
    {
        static bool Prefix(Pawn pawn, Rect rect)
        {
            if (!pawn.IsFreeNonSlaveColonist)
                return false; // Skip vanilla

            var religionIdeo = pawn.GetReligionIdeo();
            if (religionIdeo == null)
                return true; // No religion — let vanilla handle

            // Get current roles
            var ideologyRole = pawn.Ideo?.GetRole(pawn);
            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            // Get ritual target
            var roleChangeRitual = (Precept_Ritual)(pawn.Ideo?.GetPrecept(PreceptDefOf.RoleChange));
            if (roleChangeRitual == null)
                return false;

            var ritualTarget = roleChangeRitual.targetFilter.BestTarget(pawn, TargetInfo.Invalid);

            // Draw button
            bool hasRoles = religionIdeo.RolesListForReading.Any() || (pawn.Ideo?.RolesListForReading.Any() ?? false);
            if (!hasRoles)
                GUI.color = Color.gray;

            float y = rect.y + rect.height / 2f - 14f;
            Rect buttonRect = new Rect(rect.width - 150f, y, 120f, 28f);
            buttonRect.xMax = rect.width - 26f - 4f;

            if (Widgets.ButtonText(buttonRect, "ChooseRole".Translate() + "...", 
                drawBackground: true, doMouseoverSound: true, hasRoles, null))
            {
                if (ritualTarget.IsValid)
                {
                    ShowRoleMenu(pawn, religionIdeo, roleChangeRitual, ritualTarget);
                }
                else
                {
                    Messages.Message(
                        (Find.IdeoManager.classicMode ? "AbilityDisabledNoRitualSpot" : "AbilityDisabledNoAltarIdeogramOrRitualsSpot").Translate(),
                        pawn, MessageTypeDefOf.RejectInput);
                }
            }

            GUI.color = Color.white;
            return false; // Skip vanilla
        }

        private static void ShowRoleMenu(Pawn pawn, Ideo religionIdeo, Precept_Ritual roleChangeRitual, TargetInfo ritualTarget)
        {
            var options = new List<FloatMenuOption>();

            // Current ideology role
            var ideologyRole = pawn.Ideo?.GetRole(pawn);
            // Current religion role
            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            // "Remove Current Role" option (if pawn has any role)
            if (ideologyRole != null || religionRole != null)
            {
                options.Add(new FloatMenuOption("RemoveCurrentRole".Translate(), () =>
                {
                    var dialog = (Dialog_BeginRitual)roleChangeRitual.GetRitualBeginWindow(
                        ritualTarget, null, null, pawn,
                        new Dictionary<string, Pawn> { { "role_changer", pawn } });
                    dialog.SetRoleToChangeTo(null);
                    Find.WindowStack.Add(dialog);
                }, Widgets.PlaceholderIconTex, Color.white));
            }

            // Ideology roles (from vanilla's cached roles)
            AddRolesFromIdeo(options, pawn, pawn.Ideo, ideologyRole, roleChangeRitual, ritualTarget, isReligion: false);

            // Religion roles
            AddRolesFromIdeo(options, pawn, religionIdeo, religionRole, roleChangeRitual, ritualTarget, isReligion: true);

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AddRolesFromIdeo(List<FloatMenuOption> options, Pawn pawn, Ideo ideo,
            Precept_Role currentRole, Precept_Ritual roleChangeRitual, TargetInfo ritualTarget, bool isReligion)
        {
            if (ideo == null) return;

            foreach (var role in ideo.RolesListForReading)
            {
                if (role == currentRole) continue;

                if (role.RequirementsMet(pawn) && role.Active)
                {
                    // Eligible role
                    string label = role.LabelForPawn(pawn).CapitalizeFirst();
                    if (!ideo.classicMode)
                        label = label + " (" + role.def.label + ")";

                    var capturedRole = role;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        // Open ritual dialog — vanilla handles the ceremony
                        var dialog = (Dialog_BeginRitual)roleChangeRitual.GetRitualBeginWindow(
                            ritualTarget, null, null, pawn,
                            new Dictionary<string, Pawn> { { "role_changer", pawn } });
                        dialog.SetRoleToChangeTo(capturedRole);
                        Find.WindowStack.Add(dialog);
                    }, role.Icon, ideo.Color, MenuOptionPriority.Default, r => DrawTooltip(r, role, pawn))
                    {
                        orderInPriority = role.def.displayOrderInImpact
                    });
                }
                else
                {
                    // Ineligible role — show reason
                    string label = role.LabelForPawn(pawn) + " (" + role.def.label + ")";
                    if (role.ChosenPawnSingle() != null)
                        label += ": " + role.ChosenPawnSingle().LabelShort;
                    else if (!role.RequirementsMet(pawn))
                        label += ": " + role.GetFirstUnmetRequirement(pawn).GetLabel(role).CapitalizeFirst();
                    else if (!role.Active)
                    {
                        int believers = isReligion
                            ? ReligionBelieverTracker.GetBelieverCount(ideo)
                            : ideo.ColonistBelieverCountCached;
                        label += ": " + "InactiveRoleRequiresMoreBelievers".Translate(
                            role.def.activationBelieverCount, ideo.memberName, believers).CapitalizeFirst();
                    }

                    options.Add(new FloatMenuOption(label, null, role.Icon, ideo.Color)
                    {
                        orderInPriority = role.def.displayOrderInImpact
                    });
                }
            }
        }

        private static void DrawTooltip(Rect r, Precept_Role role, Pawn pawn)
        {
            TipSignal tip = new TipSignal(() => role.GetTip(), pawn.thingIDNumber * 39);
            TooltipHandler.TipRegion(r, tip);
        }
    }
}
