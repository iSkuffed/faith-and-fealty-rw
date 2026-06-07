using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
// Hooks into PawnGenerator.GeneratePawn to run HardOverride.FixVisitorPawn on
// every newly generated pawn. This catches visitor/NPC pawns that got the
// religion ideo as their primary, or that are missing religion tracking.
// Each pawn is processed at most once via the _processedPawns HashSet.
[HarmonyPatch(typeof(PawnGenerator))]
[HarmonyPatch("GeneratePawn")]
[HarmonyPatch(new[] { typeof(PawnGenerationRequest) })]
public static class Patch_PawnGenerator_GeneratePawn
{
    static void Postfix(Pawn __result)
    {
        try
        {
            HardOverride.FixVisitorPawn(__result);
        }
        catch (System.Exception ex)
        {
            Log.Warning("[IdeoRework] GeneratePawn postfix: " + ex.Message);
        }
    }
}
}
