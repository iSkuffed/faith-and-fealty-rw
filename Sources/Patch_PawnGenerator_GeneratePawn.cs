using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch("GeneratePawn")]
    [HarmonyPatch(new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            try
            {
                var pawn = __result;
                if (pawn == null || pawn.ideo == null) return;

                // Only handle NPC pawns (not player-controlled or player faction)
                if (pawn.IsPlayerControlled) return;
                if (pawn.Faction != null && pawn.Faction.IsPlayer) return;

                // Skip hidden/non-humanlike factions (ancients, mechanoids, insects)
                var faction = pawn.Faction;
                if (faction == null || faction.ideos == null) return;
                if (faction.def.hidden) return;

                // Skip if already has religion
                if (pawn.GetReligionIdeo() != null) return;

                // Only assign if faction already has a religion — do NOT create here
                var religionIdeo = FactionIdeoHelper.FindReligionIdeo(faction.ideos);
                if (religionIdeo == null) return;

                // Assign religion to the pawn
                pawn.SetReligionIdeo(religionIdeo);
                if (pawn.GetReligionCertainty() <= 0f)
                    pawn.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
            }
            catch (System.Exception ex)
            {
                Log.Warning("[IdeoRework] GeneratePawn religion assignment: " + ex.Message);
            }
        }
    }
}
