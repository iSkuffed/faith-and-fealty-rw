using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(ThoughtWorker_Precept))]
    [HarmonyPatch("CurrentStateInternal")]
    public static class Patch_ThoughtWorker_Precept_CurrentStateInternal
    {
        static void Postfix(ThoughtWorker_Precept __instance, Pawn p, ref ThoughtState __result)
        {
            if (__result.Active) return;

            var religion = p.GetReligionIdeo();
            if (religion == null) return;

            Precept precept = religion.GetFirstPreceptAllowingSituationalThought(__instance.def);
            if (precept == null) return;
            if (!precept.def.enabledForNPCFactions && !p.CountsAsNonNPCForPrecepts()) return;
            if (!religion.cachedPossibleSituationalThoughts.Contains(__instance.def)) return;

            var method = AccessTools.Method(typeof(ThoughtWorker_Precept), "ShouldHaveThought");
            if (method != null)
                __result = (ThoughtState)method.Invoke(__instance, new object[] { p });
        }
    }
}
