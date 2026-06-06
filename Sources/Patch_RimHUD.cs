using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch]
    public static class Patch_RimHUD_InspectPaneButtons
    {
        static bool Prepare()
        {
            return ModLister.GetActiveModWithIdentifier("Jaxe.RimHUD") != null;
        }

        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
            if (type == null) return null;
            return AccessTools.Method(type, "Draw");
        }

        static void Postfix(Rect bounds, IInspectPane pane, ref float offset)
        {
            try
            {
                if (!ModsConfig.IdeologyActive) return;

                var pawn = Find.Selector.SingleSelectedObject as Pawn;
                if (pawn?.ideo == null) return;

                var religion = pawn.GetReligionIdeo();
                if (religion == null) return;

                float iconSize = GenUI.SmallIconSize;
                float padding = 4f;

                offset += iconSize;
                var rect = new Rect(
                    bounds.xMax - offset,
                    bounds.y + (bounds.height - iconSize) / 2f,
                    iconSize,
                    iconSize
                );
                offset += padding;

                IdeoUIUtility.DoIdeoIcon(rect, religion, false,
                    () => IdeoUIUtility.OpenIdeoInfo(religion));

                if (Mouse.IsOver(rect))
                {
                    string label = religion.name.Colorize(ColoredText.TipSectionTitleColor);
                    string certainty = "Certainty".TranslateSimple().CapitalizeFirst() + ": "
                        + pawn.GetReligionCertainty().ToStringPercent();
                    TooltipHandler.TipRegion(rect, label + "\n" + certainty);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] RimHUD religion icon: " + ex.Message);
            }
        }
    }
}
