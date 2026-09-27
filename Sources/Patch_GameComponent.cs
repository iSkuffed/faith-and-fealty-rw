using System;
using HarmonyLib;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(GameComponentUtility))]
    [HarmonyPatch("GameComponentTick")]
    public static class Patch_GameComponentUtility_GameComponentTick
    {
        private static int lastTick = -1;
        private const int TickInterval = 250;

        static void Postfix()
        {
            try
            {
                if (Find.World == null || Current.ProgramState != ProgramState.Playing)
                    return;

                var settings = IdeoReworkModController.Settings;
                if (settings == null) return;

                int currentTick = Find.TickManager.TicksGame;

                if (lastTick < 0)
                {
                    lastTick = currentTick;
                    return;
                }

                int elapsed = currentTick - lastTick;
                if (elapsed < TickInterval) return;

                lastTick = currentTick;

                switch (settings.cognitiveDissonanceMode)
                {
                    case CognitiveDissonanceMode.Legacy:
                        CognitiveDissonanceTracker.Tick(elapsed);
                        break;
                    case CognitiveDissonanceMode.Experimental:
                        CognitiveDissonanceExperimental.Tick(elapsed);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] CognitiveDissonance tick: " + ex.Message);
            }
        }
    }
}
