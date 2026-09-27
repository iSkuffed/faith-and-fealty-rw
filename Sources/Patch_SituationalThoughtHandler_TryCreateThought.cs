using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(SituationalThoughtHandler))]
    [HarmonyPatch("TryCreateThought")]
    public static class Patch_SituationalThoughtHandler_TryCreateThought
    {
        static void Postfix(SituationalThoughtHandler __instance, ThoughtDef def,
            ref Thought_Situational __result)
        {
            if (__result == null) return;
            if (__result.sourcePrecept != null) return;

            var religion = __instance.pawn.GetReligionIdeo();
            if (religion == null) return;

            __result.sourcePrecept = religion.GetFirstPreceptAllowingSituationalThought(def);
        }
    }
}
