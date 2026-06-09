using System.Linq;
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
            if (newFaction == null || !newFaction.IsPlayer) return;

            if (__instance.GetReligionIdeo() == null)
            {
                var playerReligion = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists
                    .Where(p => p != __instance && p.GetReligionIdeo() != null)
                    .Select(p => p.GetReligionIdeo())
                    .FirstOrDefault();

                if (playerReligion != null)
                {
                    __instance.SetReligionIdeo(playerReligion);
                    __instance.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
                }
            }

            ReligionBelieverTracker.OnPawnJoinedColony(__instance);
        }
    }
}
