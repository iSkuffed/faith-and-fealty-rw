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
                var settings = IdeoReworkModController.Settings;
                if (settings == null) return;

                switch (settings.cognitiveDissonanceMode)
                {
                    case CognitiveDissonanceMode.Legacy:
                        CognitiveDissonanceTracker.LogEngagement(historyEvent.def);
                        break;
                    case CognitiveDissonanceMode.Experimental:
                        CognitiveDissonanceExperimental.OnHistoryEvent(historyEvent);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] RecordEvent: " + ex.Message);
            }
        }
    }
}
