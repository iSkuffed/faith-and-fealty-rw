using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Ideo))]
    [HarmonyPatch("RecacheColonistBelieverCount")]
    public static class Patch_Ideo_RecacheColonistBelieverCount
    {
        static void Postfix(Ideo __instance, ref int __result)
        {
            if (!PresetReligions.CreatedReligionIdeos.Contains(__instance)) return;

            int religionBelievers = ReligionBelieverTracker.GetBelieverCount(__instance);
            if (religionBelievers > 0)
            {
                var field = AccessTools.Field(typeof(Ideo), "colonistBelieverCountCached");
                field.SetValue(__instance, __result + religionBelievers);
                __result += religionBelievers;
            }
        }
    }
}
