using System;
using HarmonyLib;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(GameComponentUtility))]
    [HarmonyPatch("GameComponentTick")]
    public static class Patch_GameComponentUtility_GameComponentTick
    {
        private static int tickCounter = 0;

        static void Postfix()
        {
            try
            {
                if (IdeoReworkModController.Settings != null && !IdeoReworkModController.Settings.enableCognitiveDissonance)
                    return;

                tickCounter++;
                if (tickCounter < 250) return;  // Every 250 ticks (~4 seconds)
                tickCounter = 0;

                // Skip during world gen
                if (Find.World == null || Current.ProgramState != ProgramState.Playing) return;

                CognitiveDissonanceTracker.Tick(250);
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] CognitiveDissonance tick: " + ex.Message);
            }
        }
    }
}
