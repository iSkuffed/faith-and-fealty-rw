using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    // ── Transpiler: Expand certainty row height to make room for religion ──
    // rect3: new Rect(0f, 5f, width, 40f)  →  new Rect(0f, 5f, width, 80f)
    // rect4: new Rect(0f, 40f, width, 40f) →  new Rect(0f, 80f, width, 40f)
    // rect5.yMin += 40f                     →  rect5.yMin += 80f
    [HarmonyPatch(typeof(SocialCardUtility))]
    [HarmonyPatch("DrawSocialCard")]
    public static class Patch_SocialCardUtility_DrawSocialCard_Transpiler
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int changes = 0;

            // Pass 1: rect3 height 40f → 80f
            // IL: ldloca.s(2) → ldc.r4(0) → ldc.r4(5) → ldarga.s(0) → call get_width() → ldc.r4(40) → call ctor:Rect(4p)
            // The 40f is the height constant right before the Rect constructor call.
            // Disambiguated by ldc.r4(5f) at i+2 (rect3's Y=5, rect4's Y=40).
            for (int i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i + 1].opcode == OpCodes.Ldc_R4 && 0f.Equals(codes[i + 1].operand)
                    && codes[i + 2].opcode == OpCodes.Ldc_R4 && 5f.Equals(codes[i + 2].operand)
                    && codes[i + 5].opcode == OpCodes.Ldc_R4 && 40f.Equals(codes[i + 5].operand)
                    && i + 6 < codes.Count
                    && codes[i + 6].opcode == OpCodes.Call
                    && codes[i + 6].operand is ConstructorInfo ci1
                    && ci1.DeclaringType == typeof(Rect)
                    && ci1.GetParameters().Length == 4)
                {
                    codes[i + 5] = new CodeInstruction(OpCodes.Ldc_R4, 80f);
                    changes++;
                    Log.Message("[IdeoRework] Transpiler: rect3 height 40→80 at IL " + (i + 5));
                    break;
                }
            }

            // Pass 2: rect4 Y 40f → 80f
            // IL: ldloca.s(3) → ldc.r4(0) → ldc.r4(40) → ldarga.s(0) → call get_width() → ldc.r4(40) → call ctor:Rect(4p)
            // Both i+2 and i+5 are 40f. Change i+2 (the Y value).
            // Disambiguated by ldc.r4(40f) at i+2 (rect4's Y=40, rect3 had 5f).
            for (int i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i + 1].opcode == OpCodes.Ldc_R4 && 0f.Equals(codes[i + 1].operand)
                    && codes[i + 2].opcode == OpCodes.Ldc_R4 && 40f.Equals(codes[i + 2].operand)
                    && codes[i + 5].opcode == OpCodes.Ldc_R4 && 40f.Equals(codes[i + 5].operand)
                    && i + 6 < codes.Count
                    && codes[i + 6].opcode == OpCodes.Call
                    && codes[i + 6].operand is ConstructorInfo ci2
                    && ci2.DeclaringType == typeof(Rect)
                    && ci2.GetParameters().Length == 4)
                {
                    codes[i + 2] = new CodeInstruction(OpCodes.Ldc_R4, 80f);
                    changes++;
                    Log.Message("[IdeoRework] Transpiler: rect4 Y 40→80 at IL " + (i + 2));
                    break;
                }
            }

            // Pass 3: rect5.yMin += 40f  →  rect5.yMin += 80f
            // IL: ldloca.s → dup → call get_yMin() → ldc.r4(40) → add → call set_yMin()
            for (int i = 0; i < codes.Count - 5; i++)
            {
                if ((codes[i].opcode == OpCodes.Ldloca || codes[i].opcode == OpCodes.Ldloca_S)
                    && codes[i + 1].opcode == OpCodes.Dup
                    && codes[i + 2].opcode == OpCodes.Call
                    && codes[i + 2].operand is MethodInfo mi1 && mi1.Name == "get_yMin"
                    && codes[i + 3].opcode == OpCodes.Ldc_R4 && 40f.Equals(codes[i + 3].operand)
                    && codes[i + 4].opcode == OpCodes.Add
                    && i + 5 < codes.Count
                    && codes[i + 5].opcode == OpCodes.Call
                    && codes[i + 5].operand is MethodInfo mi2 && mi2.Name == "set_yMin")
                {
                    codes[i + 3] = new CodeInstruction(OpCodes.Ldc_R4, 80f);
                    changes++;
                    Log.Message("[IdeoRework] Transpiler: rect5.yMin += 40f → += 80f at IL " + (i + 3));
                    break;
                }
            }

            if (changes < 3)
                Log.Warning($"[IdeoRework] DrawSocialCard transpiler only applied {changes}/3 patches — IL pattern may have changed");
            else
                Log.Message($"[IdeoRework] DrawSocialCard transpiler applied {changes}/3 patches successfully");

            return codes;
        }
    }

    // ── Role selection button (replaces vanilla) ──────────────────────────
    [HarmonyPatch(typeof(SocialCardUtility))]
    [HarmonyPatch("DrawPawnRoleSelection")]
    public static class Patch_SocialCardUtility_DrawPawnRoleSelection
    {
        static bool Prefix(Pawn pawn, Rect rect)
        {
            if (!pawn.IsFreeNonSlaveColonist)
                return false;

            var religionIdeo = pawn.GetReligionIdeo();
            if (religionIdeo == null)
                return true;

            var ideologyRole = pawn.Ideo?.GetRole(pawn);
            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            var roleChangeRitual = (Precept_Ritual)(pawn.Ideo?.GetPrecept(PreceptDefOf.RoleChange));
            if (roleChangeRitual == null)
                return false;

            var ritualTarget = roleChangeRitual.targetFilter.BestTarget(pawn, TargetInfo.Invalid);

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
            return false;
        }

        private static void ShowRoleMenu(Pawn pawn, Ideo religionIdeo, Precept_Ritual roleChangeRitual, TargetInfo ritualTarget)
        {
            var options = new List<FloatMenuOption>();

            var ideologyRole = pawn.Ideo?.GetRole(pawn);
            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            // Get religion's RoleChange ritual (used for removal and religion role assignment)
            var religionRoleChangeRitual = religionIdeo?.GetPrecept(PreceptDefOf.RoleChange) as Precept_Ritual;
            var religionRitualTarget = religionRoleChangeRitual?.targetFilter.BestTarget(pawn, TargetInfo.Invalid) ?? ritualTarget;
            var ritualToUse = religionRoleChangeRitual ?? roleChangeRitual;

            if (ideologyRole != null || religionRole != null)
            {
                options.Add(new FloatMenuOption("RemoveCurrentRole".Translate(), () =>
                {
                    // Use religion's ritual — Patch_Precept_Ritual_GetRitualBeginWindow intercepts
                    // and opens Dialog_BeginReligionRitual with religion roles in cachedRoles.
                    // "None" option in the dialog sets roleChangeSelection = null,
                    // then Patch_RitualOutcomeEffectWorker_RoleChange unassigns the religion role.
                    var dialog = (Dialog_BeginRitual)ritualToUse.GetRitualBeginWindow(
                        religionRitualTarget, null, null, pawn,
                        new Dictionary<string, Pawn> { { "role_changer", pawn } });
                    dialog.SetRoleToChangeTo(null);
                    Find.WindowStack.Add(dialog);
                }, Widgets.PlaceholderIconTex, Color.white));
            }

            // Ideology roles — use ideology's ritual
            AddRolesFromIdeo(options, pawn, pawn.Ideo, ideologyRole, roleChangeRitual, ritualTarget, isReligion: false);

            // Religion roles — use religion's ritual
            AddRolesFromIdeo(options, pawn, religionIdeo, religionRole, ritualToUse, religionRitualTarget, isReligion: true);

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
                    string label = role.LabelForPawn(pawn).CapitalizeFirst();
                    if (!ideo.classicMode)
                        label = label + " (" + role.def.label + ")";

                    var capturedRole = role;
                    options.Add(new FloatMenuOption(label, () =>
                    {
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

    // ── Role row: Full replacement — draws both ideology and religion roles ──
    // Prefix that returns false to skip vanilla entirely. No background painting,
    // no overlap, no snip artifacts for the ritual UI.
    [HarmonyPatch(typeof(SocialCardUtility))]
    [HarmonyPatch("DrawPawnRole")]
    public static class Patch_SocialCardUtility_DrawPawnRole
    {
        static bool Prefix(Pawn pawn, Precept_Role role, string label, Rect rect, bool drawLine)
        {
            if (pawn == null) return true;

            float midX = rect.x + rect.width * 0.45f;
            float buttonStart = rect.width - 150f;

            // ── Left section: Ideology role ──
            float ideIconX = rect.x + 17f;
            if (role != null)
            {
                // Draw ideology role icon (32x32)
                float iconY = rect.y + rect.height / 2f - 16f;
                Rect iconRect = new Rect(ideIconX, iconY, 32f, 32f);
                GUI.color = role.ideo.Color;
                Widgets.DrawTextureFitted(iconRect, role.Icon, 1f);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
            }

            // Draw ideology role label (or "No Ideo Role Assigned")
            float ideLabelX = role != null ? ideIconX + 42f : ideIconX;
            Rect ideLabelRect = new Rect(ideLabelX, rect.y, midX - ideLabelX - 4f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(ideLabelRect, role != null ? label : "No Ideo Role Assigned");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Ideology tooltip
            Rect ideTooltipRect = new Rect(ideIconX, rect.y, midX - ideIconX, rect.height);
            if (Mouse.IsOver(ideTooltipRect))
            {
                string roleDesc = "RoleDesc".Translate().Resolve();
                if (role != null)
                    roleDesc = roleDesc + "\n\n" + role.LabelForPawn(pawn) + ": " + role.GetTip();
                Widgets.DrawHighlight(ideTooltipRect);
                TipSignal tip = new TipSignal(() => roleDesc, pawn.thingIDNumber * 39);
                TooltipHandler.TipRegion(ideTooltipRect, tip);
            }

            // ── Divider line ──
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawLineVertical(midX, rect.y + 4f, rect.height - 8f);
            GUI.color = Color.white;

            // ── Right section: Religion role ──
            float rightStart = midX + 2f;
            float rightWidth = buttonStart - rightStart - 4f;
            if (rightWidth < 60f) return false;

            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            if (religionRole != null)
            {
                // Religion role icon (24x24)
                float iconSize = 24f;
                float iconY = rect.y + rect.height / 2f - iconSize / 2f;
                Rect iconRect = new Rect(rightStart, iconY, iconSize, iconSize);

                GUI.color = religionRole.ideo.Color;
                Widgets.DrawTextureFitted(iconRect, religionRole.Icon, 1f);
                GUI.color = Color.white;

                // Religion role label
                string religionLabel = religionRole.LabelForPawn(pawn);
                float labelX = rightStart + iconSize + 2f;
                float labelWidth = buttonStart - labelX - 4f;
                if (labelWidth >= 20f)
                {
                    Rect labelRect = new Rect(labelX, rect.y, labelWidth, rect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(labelRect, religionLabel.Truncate(labelRect.width));
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                // Religion tooltip
                Rect tooltipRect = new Rect(rightStart, rect.y, buttonStart - rightStart, rect.height);
                if (Mouse.IsOver(tooltipRect))
                {
                    TipSignal tip = new TipSignal(() => religionRole.LabelForPawn(pawn) + ": " + religionRole.GetTip(), pawn.thingIDNumber * 39);
                    TooltipHandler.TipRegion(tooltipRect, tip);
                }
            }
            else
            {
                // No religion role — placeholder
                Rect noRoleRect = new Rect(rightStart, rect.y, rightWidth, rect.height);
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(noRoleRect, "No Reli Role Assigned");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // ── Divider line at bottom of row ──
            if (drawLine)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                Widgets.DrawLineHorizontal(0f, rect.yMax, rect.width);
                GUI.color = Color.white;
            }

            return false; // Skip vanilla entirely
        }
    }

    // ── Religion certainty bar in the certainty row ───────────────────────
    // Vanilla draws in the top 40px of the (now 80px) row.
    // We draw religion certainty in the bottom 40px, mirroring vanilla's layout.
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(SocialCardUtility))]
    [HarmonyPatch("DrawPawnCertainty")]
    public static class Patch_SocialCardUtility_DrawPawnCertainty
    {
        private static readonly Texture2D ReligionBarTex = SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Green);

        static void Postfix(Pawn pawn, Rect rect)
        {
            if (pawn == null) return;

            var religionIdeo = pawn.GetReligionIdeo();
            if (religionIdeo == null) return;

            // Religion certainty bar in the bottom 40px — uses FULL row width, same layout as vanilla
            // Vanilla layout: [17px pad] [32px icon] [10px gap] [name...] [10px gap] [bar...] [26px pad]
            float yOffset = 44f;
            float rowY = rect.y + yOffset;
            float rowHeight = 36f;
            float centerY = rowY + rowHeight / 2f;

            // Religion icon (32x32) — same position as vanilla's ideology icon
            float iconX = rect.x + 17f;
            Rect iconRect = new Rect(iconX, centerY - 16f, 32f, 32f);
            religionIdeo.DrawIcon(iconRect);

            // Religion name — same layout as vanilla's ideology name
            float nameX = iconX + 42f;
            float certainty = pawn.GetReligionCertainty();
            float nameWidth = rect.width / 2f - nameX;
            if (nameWidth < 40f) nameWidth = 40f;
            Rect nameRect = new Rect(nameX, rowY, nameWidth, rowHeight);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, religionIdeo.name.Truncate(nameRect.width));
            Text.Anchor = TextAnchor.UpperLeft;

            // Religion certainty bar — same layout as vanilla's ideology bar
            float barX = nameX + nameWidth + 10f;
            float rightEdge = rect.width - 26f;
            Rect barRect = new Rect(barX, centerY - 16f, rightEdge - barX, 32f);
            Widgets.FillableBar(barRect.ContractedBy(4f), certainty, ReligionBarTex);

            // Interactive area covering the full religion section
            Rect interactiveRect = new Rect(iconX, rowY, rightEdge - iconX, rowHeight);

            if (Mouse.IsOver(interactiveRect))
            {
                Widgets.DrawHighlight(interactiveRect);

                TaggedString tip = "Religion"
                    + "\n" + religionIdeo.name.Colorize(ColoredText.TipSectionTitleColor)
                    + "\n" + "Certainty".Translate().CapitalizeFirst() + ": " + certainty.ToStringPercent();
                TooltipHandler.TipRegion(interactiveRect, tip.Resolve());
            }

            if (Widgets.ButtonInvisible(interactiveRect))
            {
                Find.WindowStack.Add(new Dialog_ReligionList());
            }
        }
    }

    // ── Prefix: Replace DrawRoleSelection for religion rituals ─────────────
    // Vanilla only checks pawn.Ideo.GetRole (ideology) for currentRole.
    // For Dialog_BeginReligionRitual we use IdeoRoleManager.GetRole(pawn, true)
    // so the "None" (remove) option appears for religion roles.
    [HarmonyPatch(typeof(Dialog_BeginRitual))]
    [HarmonyPatch("DrawRoleSelection")]
    public static class Patch_Dialog_BeginRitual_DrawRoleSelection
    {
        static bool Prefix(Dialog_BeginRitual __instance, Pawn pawn, Rect rect)
        {
            if (!(__instance is Dialog_BeginReligionRitual))
                return true;

            var assignments = AccessTools.Field(typeof(Dialog_BeginRitual), "assignments")
                .GetValue(__instance) as RitualRoleAssignments;
            var cachedRoles = AccessTools.Field(typeof(Dialog_BeginRitual), "cachedRoles")
                .GetValue(null) as List<Precept_Role>;
            var confirmTextField = AccessTools.Field(typeof(Dialog_BeginRitual), "confirmText");

            Precept_Role roleChangeSelection = assignments.RoleChangeSelection;
            Precept_Role currentRole = IdeoRoleManager.GetRole(pawn, isReligion: true);

            if (roleChangeSelection == null && currentRole == null)
            {
                Precept_Role ideologyRole = pawn?.Ideo?.GetRole(pawn);
                if (ideologyRole == null)
                {
                    var firstAvailable = RitualUtility.AllRolesForPawn(pawn)
                        .FirstOrDefault(r => r.Active && r.RequirementsMet(pawn));
                    __instance.SetRoleToChangeTo(firstAvailable);
                    roleChangeSelection = firstAvailable;
                }
            }

            if (roleChangeSelection != null || currentRole != null)
            {
                SocialCardUtility.DrawPawnRole(pawn, roleChangeSelection,
                    (roleChangeSelection != null)
                        ? roleChangeSelection.LabelCap
                        : "RemoveRole".Translate(currentRole.Label).Resolve(),
                    rect, drawLine: false);
            }

            Rect buttonRect = new Rect(rect.x + 220f, rect.y + 2f, 140f, 32f);
            bool flag = pawn?.Ideo != null;
            if (!flag) GUI.color = Color.gray;

            if (cachedRoles != null && cachedRoles.Count > 1
                && Widgets.ButtonText(buttonRect, "ChooseNewRole".Translate() + "...",
                    drawBackground: true, doMouseoverSound: true, flag, null))
            {
                var list = new List<FloatMenuOption>();

                if (currentRole != null)
                {
                    list.Add(new FloatMenuOption("None".Translate(), delegate
                    {
                        confirmTextField.SetValue(__instance,
                            ("ChooseRoleConfirmUnassign".Translate(currentRole.Named("ROLE"), pawn.Named("PAWN"))
                            + "\n\n" + "ChooseRoleConfirmAssignPostfix".Translate()).Resolve());
                        __instance.SetRoleToChangeTo(null);
                    }, Widgets.PlaceholderIconTex, Color.white));
                }

                foreach (Precept_Role cachedRole in cachedRoles)
                {
                    Precept_Role newRole = cachedRole;
                    if (newRole == roleChangeSelection || newRole == currentRole
                        || !newRole.Active || !newRole.RequirementsMet(pawn))
                        continue;

                    string text = newRole.LabelForPawn(pawn) + " (" + newRole.def.label + ")";
                    TaggedString confirmTextLocal = "ChooseRoleConfirmAssign".Translate(
                        newRole.Named("ROLE"), pawn.Named("PAWN"));
                    string extraConfirmText = RitualUtility.RoleChangeConfirmation(pawn, currentRole, newRole);
                    Pawn pawn2 = newRole.ChosenPawns().FirstOrDefault();
                    if (pawn2 != null && newRole is Precept_RoleSingle)
                        text = text + ": " + pawn2.LabelShort;

                    if (!extraConfirmText.NullOrEmpty())
                    {
                        list.Add(new FloatMenuOption(text, delegate
                        {
                            confirmTextField.SetValue(__instance,
                                (confirmTextLocal + "\n\n" + extraConfirmText + "\n\n"
                                + "ChooseRoleConfirmAssignPostfix".Translate()).Resolve());
                            __instance.SetRoleToChangeTo(newRole);
                        }, newRole.Icon, newRole.ideo.Color, MenuOptionPriority.Default, r =>
                        {
                            TipSignal tip = new TipSignal(() => newRole.GetTip(), pawn.thingIDNumber * 39);
                            TooltipHandler.TipRegion(r, tip);
                        })
                        { orderInPriority = newRole.def.displayOrderInImpact });
                    }
                    else
                    {
                        list.Add(new FloatMenuOption(text, delegate
                        {
                            bool isReligionRole = PresetReligions.CreatedReligionIdeos.Contains(newRole.ideo);
                            if (isReligionRole)
                                IdeoRoleManager.AssignRole(pawn, newRole, isReligion: true);
                            newRole.Assign(pawn, addThoughts: true);
                        }, newRole.Icon, newRole.ideo.Color, MenuOptionPriority.Default, r =>
                        {
                            TipSignal tip = new TipSignal(() => newRole.GetTip(), pawn.thingIDNumber * 39);
                            TooltipHandler.TipRegion(r, tip);
                        })
                        { orderInPriority = newRole.def.displayOrderInImpact });
                    }
                }

                foreach (Precept_Role cachedRole in cachedRoles)
                {
                    if ((cachedRole != roleChangeSelection && !cachedRole.RequirementsMet(pawn))
                        || !cachedRole.Active)
                    {
                        string text2 = cachedRole.LabelForPawn(pawn) + " (" + cachedRole.def.label + ")";
                        if (cachedRole.ChosenPawnSingle() != null)
                            text2 = text2 + ": " + cachedRole.ChosenPawnSingle().LabelShort;
                        else if (!cachedRole.RequirementsMet(pawn))
                            text2 = text2 + ": " + cachedRole.GetFirstUnmetRequirement(pawn)
                                .GetLabel(cachedRole).CapitalizeFirst();
                        else if (!cachedRole.Active)
                        {
                            int believers = PresetReligions.CreatedReligionIdeos.Contains(cachedRole.ideo)
                                ? ReligionBelieverTracker.GetBelieverCount(cachedRole.ideo)
                                : cachedRole.ideo.ColonistBelieverCountCached;
                            if (cachedRole.def.activationBelieverCount > believers)
                                text2 += ": " + "InactiveRoleRequiresMoreBelievers".Translate(
                                    cachedRole.def.activationBelieverCount,
                                    cachedRole.ideo.memberName, believers).CapitalizeFirst();
                        }

                        list.Add(new FloatMenuOption(text2, null, cachedRole.Icon, cachedRole.ideo.Color)
                        { orderInPriority = cachedRole.def.displayOrderInImpact });
                    }
                }

                if (list.Any())
                    Find.WindowStack.Add(new FloatMenu(list));
            }

            GUI.color = Color.white;
            return false;
        }
    }
}
