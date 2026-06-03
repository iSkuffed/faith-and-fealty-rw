using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("Kill")]
    public static class Patch_Pawn_Kill
    {
        static void Postfix(Pawn __instance)
        {
            ReligionBelieverTracker.OnPawnDied(__instance);
            if (__instance == ReligionLeaderTracker.ReligionLeader)
            {
                Log.Message($"[IdeoRework] Religion leader {__instance.LabelShort} died, clearing");
                ReligionLeaderTracker.Clear();
            }
        }
    }
}
