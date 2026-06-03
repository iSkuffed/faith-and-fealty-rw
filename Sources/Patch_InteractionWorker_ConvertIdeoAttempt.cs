using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(InteractionWorker_ConvertIdeoAttempt))]
    [HarmonyPatch("Interacted")]
    public static class Patch_InteractionWorker_ConvertIdeoAttempt
    {
        static void Postfix(Pawn initiator, Pawn recipient)
        {
            try
            {
                if (initiator == null || recipient == null) return;

                var initiatorReligion = initiator.GetReligionIdeo();
                var recipientReligion = recipient.GetReligionIdeo();
                if (initiatorReligion == null || recipientReligion == null) return;
                if (initiatorReligion == recipientReligion) return;

                float reduction = InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(initiator, recipient);
                float newCertainty = UnityEngine.Mathf.Clamp01(recipient.GetReligionCertainty() - reduction);
                recipient.SetReligionCertainty(newCertainty);
                ReligionConversionTracker.CheckForConversion(recipient, newCertainty);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[IdeoRework] InteractionWorker_Convert religion: " + ex.Message);
            }
        }
    }
}
