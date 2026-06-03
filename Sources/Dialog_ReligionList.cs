using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace IdeoRework
{
    public class Dialog_ReligionList : Window
    {
        private Vector2 scrollPosition_list;
        private float scrollViewHeight_list;
        private Vector2 scrollPosition_details;
        private float scrollViewHeight_details;
        private Ideo selectedReligion;

        public static Dialog_ReligionList OpenInstance;

        // Match vanilla Ideos tab size: 1010 x (screenHeight - 35)
        public override Vector2 InitialSize => new Vector2(1010f, UI.screenHeight - 35f);

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            // Anchor to left side, matching vanilla Ideos tab
            windowRect.x = 0f;
            windowRect.y = (float)(UI.screenHeight - 35) - windowRect.height;
        }

        public Dialog_ReligionList()
        {
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            OpenInstance = this;
        }

        public override void PostClose()
        {
            base.PostClose();
            OpenInstance = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            float backButtonWidth = 200f;
            Rect backButtonRect = new Rect(inRect.x, inRect.y, backButtonWidth, 32f);
            if (Widgets.ButtonText(backButtonRect, "Back to Ideoligions"))
            {
                Close();
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Ideos);
            }

            float topOffset = 36f;
            Rect contentRect = new Rect(inRect.x, inRect.y + topOffset, inRect.width, inRect.height - topOffset);

            Rect listRect = new Rect(contentRect.x, contentRect.y, Mathf.FloorToInt(contentRect.width * 0.25f), contentRect.height);
            Rect detailsRect = new Rect(listRect.xMax, contentRect.y, contentRect.width - listRect.width, contentRect.height);

            DrawReligionList(listRect);
            DrawReligionDetails(detailsRect);
        }

        private void DrawReligionList(Rect fillRect)
        {
            Widgets.BeginGroup(fillRect);
            Rect outRect = fillRect.AtZero();
            outRect.yMin += 17f;
            Rect viewRect = new Rect(0f, 0f, fillRect.width - 16f, scrollViewHeight_list);
            Widgets.BeginScrollView(outRect, ref scrollPosition_list, viewRect);

            float curY = 0f;
            int row = 0;

            // Use CreatedReligionIdeos directly — includes NPC religions and player religion
            foreach (var ideo in PresetReligions.CreatedReligionIdeos)
            {
                if (ideo == null) continue;
                DrawReligionRow(ideo, ref curY, viewRect, row);
                row++;
            }

            if (Event.current.type == EventType.Layout)
                scrollViewHeight_list = curY;

            Widgets.EndScrollView();
            Widgets.EndGroup();
        }

        private void DrawReligionRow(Ideo ideo, ref float curY, Rect fillRect, int row)
        {
            Rect iconRect = new Rect(7f, curY + 7f, 30f, 30f);
            Rect labelRect = new Rect(44f, curY + 3f, fillRect.width - 44f, 22f);
            Rect rowRect = new Rect(0f, curY, fillRect.width, 46f);

            if (row % 2 == 1)
                Widgets.DrawLightHighlight(rowRect);
            if (ideo == selectedReligion)
                Widgets.DrawHighlightSelected(rowRect);
            else
                Widgets.DrawHighlightIfMouseover(rowRect);

            ideo.DrawIcon(iconRect);
            Widgets.Label(labelRect, ideo.name.Truncate(labelRect.width));

            // Draw faction icons below the name
            DrawFactionIconsForIdeo(ideo, 44f, ref curY, fillRect.width - 44f, 18f);

            curY += 46f;

            if (Mouse.IsOver(rowRect) && Widgets.ButtonInvisible(rowRect))
            {
                selectedReligion = ideo;
                SoundDefOf.DialogBoxAppear.PlayOneShotOnCamera();
            }
        }

        private void DrawFactionIconsForIdeo(Ideo ideo, float startX, ref float curY, float width, float iconSize)
        {
            float y = curY + 22f; // Below the name
            float x = startX;

            foreach (var faction in Find.FactionManager.AllFactionsInViewOrder)
            {
                if (faction.Hidden || faction.ideos == null) continue;
                if (!faction.ideos.IsPrimary(ideo) && !faction.ideos.IsMinor(ideo)) continue;

                float sz = faction.ideos.IsPrimary(ideo) ? iconSize : iconSize * 0.75f;
                if (x + sz > startX + width)
                    break;

                FactionUIUtility.DrawFactionIconWithTooltip(new Rect(x, y, sz, sz), faction);
                x += sz + 2f;
            }
        }

        private void DrawReligionDetails(Rect fillRect)
        {
            if (selectedReligion == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.NoneLabelCenteredVertically(fillRect, "(" + "Select a religion" + ")");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect contracted = fillRect.ContractedBy(17f);
            contracted.yMax += 8f;
            IdeoUIUtility.DoIdeoDetails(contracted, selectedReligion, ref scrollPosition_details, ref scrollViewHeight_details, editMode: false, ideoLoadedFromFile: null, allowLoad: false, allowSave: false);
        }
    }
}
