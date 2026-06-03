using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(FactionUIUtility))]
    [HarmonyPatch("DrawFactionRow")]
    public static class Patch_FactionUIUtility
    {
        static void Postfix(Faction faction, float rowY, Rect fillRect)
        {
            if (!faction.IsPlayer) return;

            var leader = ReligionLeaderTracker.ReligionLeader;
            if (leader == null) return;

            // Add religion leader to tooltip
            var rect = new Rect(90f, rowY, 300f, 80f);
            if (Mouse.IsOver(rect))
            {
                var title = ReligionLeaderTracker.GetLeaderTitle();
                if (title != null)
                {
                    var tip = "Religion".Colorize(ColoredText.TipSectionTitleColor)
                        + "\n" + title.CapitalizeFirst() + ": " + leader.Name.ToStringFull;
                    TooltipHandler.TipRegion(rect, tip);
                }
            }
        }
    }
}
