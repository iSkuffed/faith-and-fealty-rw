using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(CharacterCardUtility))]
    [HarmonyPatch("DoTopStack")]
    public static class Patch_CharacterCardUtility_DoTopStack
    {
        static void Postfix(Pawn pawn, Rect rect, bool creationMode, float curY, ref float __result)
        {
            try
            {
                if (pawn == null || Find.IdeoManager == null || Find.IdeoManager.classicMode) return;

                var religionIdeo = pawn.GetReligionIdeo();
                if (religionIdeo == null) return;

                float width = rect.width - 10f;
                if (creationMode)
                    width -= 20f + Page_ConfigureStartingPawns.PawnPortraitSize.x;

                float plateWidth = Text.CalcSize(religionIdeo.name).x + 22f + 15f;

                // Draw religion plate at the current Y position
                Rect plateRect = new Rect(0f, __result, plateWidth, 22f);

                GUI.color = new Color(0.35f, 0.35f, 0.35f);
                GUI.DrawTexture(plateRect, BaseContent.WhiteTex);
                GUI.color = Color.white;

                // Draw icon (20x20)
                Rect iconRect = new Rect(plateRect.x + 1f, plateRect.y + 1f, 20f, 20f);
                religionIdeo.DrawIcon(iconRect);

                // Draw name
                Widgets.Label(new Rect(plateRect.x + 22f + 5f, plateRect.y, plateWidth - 22f - 5f, 22f), religionIdeo.name);

                // Click to open ideo info
                if (Widgets.ButtonInvisible(plateRect))
                {
                    IdeoUIUtility.OpenIdeoInfo(religionIdeo);
                }

                // Single tooltip — fixed uniqueId prevents duplicate registration
                if (Mouse.IsOver(plateRect))
                {
                    TaggedString tip = "Religion"
                        + "\n" + religionIdeo.name.Colorize(ColoredText.TipSectionTitleColor);
                    tip += "\n" + "Certainty".Translate().CapitalizeFirst() + ": " + pawn.GetReligionCertainty().ToStringPercent();
                    tip += "\n\n" + "ClickForMoreInfo".Translate().Colorize(ColoredText.SubtleGrayColor);
                    TooltipHandler.TipRegion(plateRect, tip.Resolve());
                }

                __result += 26f;
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] DoTopStack religion display: " + ex.Message);
            }
        }
    }
}
