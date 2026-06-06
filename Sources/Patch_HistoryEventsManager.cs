using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(HistoryEventsManager))]
    [HarmonyPatch("RecordEvent")]
    public static class Patch_HistoryEventsManager_RecordEvent
    {
        static void Postfix(HistoryEvent historyEvent)
        {
            try
            {
                if (IdeoReworkModController.Settings != null && !IdeoReworkModController.Settings.enableCognitiveDissonance)
                    return;

                // Log engagement for cognitive dissonance tracking
                CognitiveDissonanceTracker.LogEngagement(historyEvent.def);
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] RecordEvent engagement logging: " + ex.Message);
            }
        }
    }
}
