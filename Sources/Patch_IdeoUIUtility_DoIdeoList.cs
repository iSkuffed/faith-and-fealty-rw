using System.Collections.Generic;
using System.Linq;
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

        static bool Prefix(Rect fillRect, ref Vector2 scrollPosition, ref float scrollViewHeight, out Ideo mouseoverIdeo, bool showCreateNewButton)
        {
            mouseoverIdeo = null;

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
