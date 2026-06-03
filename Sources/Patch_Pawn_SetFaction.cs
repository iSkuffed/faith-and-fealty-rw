using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("SetFaction")]
    public static class Patch_Pawn_SetFaction
    {
        static void Postfix(Pawn __instance, Faction newFaction)
        {
            if (newFaction != null && newFaction.IsPlayer)
                ReligionBelieverTracker.OnPawnJoinedColony(__instance);
        }
    }
}
