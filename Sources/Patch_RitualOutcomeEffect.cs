using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_FromQuality))]
    [HarmonyPatch("Apply")]
    public static class Patch_RitualOutcomeEffectWorker_Apply
    {
        static void Postfix(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            try
            {
                var ritualIdeo = jobRitual.Ritual?.ideo;
                if (ritualIdeo == null) return;

                foreach (var entry in totalPresence)
                {
                    var pawn = entry.Key;
                    if (pawn == null) continue;

                    var religionIdeo = pawn.GetReligionIdeo();
                    if (religionIdeo == null || religionIdeo != ritualIdeo) continue;

                    // Gain certainty: +3% for attending a religion ritual
                    float gain = 0.03f;

                    float newCertainty = UnityEngine.Mathf.Clamp01(pawn.GetReligionCertainty() + gain);
                    pawn.SetReligionCertainty(newCertainty);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[IdeoRework] RitualOutcome religion certainty: " + ex.Message);
            }
        }
    }
}
