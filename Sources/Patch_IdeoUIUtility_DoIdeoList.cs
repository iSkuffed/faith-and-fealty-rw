using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace IdeoRework
{
    // Filter religions from the Ideoligion list and add "Religions..." button
    [HarmonyPatch(typeof(IdeoUIUtility))]
    [HarmonyPatch("DoIdeoList")]
    public static class Patch_IdeoUIUtility_DoIdeoList
    {
        private static float religionButtonHeight = 36f;

        // When the Prefix defers to vanilla, vanilla still lists every ideo via IdeosInViewOrder.
        // Swap those calls for a filtered copy so religions can't be picked as a primary ideo there.
        // (Filtering the getter itself would also hide religions from ritual seats.)
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(IdeoManager), nameof(IdeoManager.IdeosInViewOrder));
            var filtered = AccessTools.Method(typeof(Patch_IdeoUIUtility_DoIdeoList), nameof(IdeosInViewOrderNoReligions));
            foreach (var ins in instructions)
            {
                if (ins.Calls(getter))
                    yield return new CodeInstruction(OpCodes.Call, filtered).WithLabels(ins.labels).WithBlocks(ins.blocks);
                else
                    yield return ins;
            }
        }

        private static IEnumerable<Ideo> IdeosInViewOrderNoReligions(IdeoManager manager)
        {
            return manager.IdeosInViewOrder.Where(i => !PresetReligions.CreatedReligionIdeos.Contains(i));
        }

        static bool Prefix(Rect fillRect, ref Vector2 scrollPosition, ref float scrollViewHeight, out Ideo mouseoverIdeo,
            bool showCreateNewButton, List<Pawn> pawns = null, Action createCustomBtnActOverride = null,
            bool forArchonexusRestart = false, Func<Pawn, Ideo> pawnIdeoGetter = null,
            bool showLoadExistingIdeoBtn = false, Action createFluidBtnAct = null)
        {
            mouseoverIdeo = null;

            // Our filtered list only knows how to draw a plain ideo browser (used by the main
            // Ideos tab / landing page). Any of these mean a caller needs vanilla's pawn-assignment
            // rows, create/load/fluid buttons, or the archonexus restart flow (e.g. the new-colony
            // quest's Dialog_ConfigureIdeo, or Dialog_ConfigureIdeo's CreateFluid button) — let
            // vanilla run untouched rather than silently dropping that functionality.
            if (pawns != null || createCustomBtnActOverride != null || forArchonexusRestart
                || pawnIdeoGetter != null || showLoadExistingIdeoBtn || createFluidBtnAct != null
                || showCreateNewButton)
            {
                return true;
            }

            // Open our own group (vanilla's DoIdeoList opens one too, but we skip vanilla)
            Widgets.BeginGroup(fillRect);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = fillRect.AtZero();
            outRect.yMin += 17f;
            Rect viewRect = new Rect(0f, 0f, fillRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float curY = 0f;
            int row = 0;

            // "Religions..." button at the top
            Rect religionsBtnRect = new Rect(0f, curY + 4f, viewRect.width, 28f);
            if (Widgets.ButtonText(religionsBtnRect, "Religions...", drawBackground: true, doMouseoverSound: true, active: true, null))
            {
                Find.WindowStack.Add(new Dialog_ReligionList());
            }
            curY += religionButtonHeight;
            row++;

            // Draw ideo rows, skipping religions
            foreach (var ideo in Find.IdeoManager.IdeosInViewOrder)
            {
                if (PresetReligions.CreatedReligionIdeos.Contains(ideo))
                    continue;

                DrawIdeoRowFiltered(ideo, ref curY, viewRect, ref mouseoverIdeo, row);
                row++;
            }

            if (Event.current.type == EventType.Layout)
                scrollViewHeight = curY;

            Widgets.EndScrollView();
            Widgets.EndGroup();

            return false; // Skip vanilla DoIdeoList
        }

        private static void DrawIdeoRowFiltered(Ideo ideo, ref float curY, Rect fillRect, ref Ideo mouseover, int row)
        {
            Rect iconRect = new Rect(7f, curY + 7f, 30f, 30f);
            Rect labelRect = new Rect(44f, curY + 3f, fillRect.width - 44f, 22f);
            Rect rowRect = new Rect(0f, curY, fillRect.width, 46f);

            if (row % 2 == 1)
                Widgets.DrawLightHighlight(rowRect);
            if (IdeoUIUtility.selected == ideo)
                Widgets.DrawHighlightSelected(rowRect);
            else
                Widgets.DrawHighlightIfMouseover(rowRect);

            ideo.DrawIcon(iconRect);
            Widgets.Label(labelRect, ideo.name.Truncate(labelRect.width));

            // Draw faction icons below the name
            float factionY = curY + 22f;
            float factionX = 44f;
            foreach (var faction in Find.FactionManager.AllFactionsInViewOrder)
            {
                if (faction.Hidden || faction.ideos == null) continue;
                if (!faction.ideos.IsPrimary(ideo) && !faction.ideos.IsMinor(ideo)) continue;

                float sz = faction.ideos.IsPrimary(ideo) ? 18f : 14f;
                if (factionX + sz > fillRect.width - 10f)
                    break;

                FactionUIUtility.DrawFactionIconWithTooltip(new Rect(factionX, factionY, sz, sz), faction);
                factionX += sz + 2f;
            }

            curY += 46f;

            if (Mouse.IsOver(rowRect))
                mouseover = ideo;

            if (IdeoUIUtility.selected != ideo && Widgets.ButtonInvisible(rowRect))
            {
                IdeoUIUtility.SetSelected(ideo);
                SoundDefOf.DialogBoxAppear.PlayOneShotOnCamera();
            }
        }
    }
}
