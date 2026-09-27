using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    // World pawn GC discards pawns for good; drop them from the religion maps so they don't pile up.
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("Discard")]
    public static class Patch_Pawn_Discard
    {
        static void Postfix(Pawn __instance) => ReligionIdeoTracker.Forget(__instance);
    }

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
