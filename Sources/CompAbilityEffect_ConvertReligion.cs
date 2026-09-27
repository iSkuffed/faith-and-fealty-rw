using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public class CompProperties_ConvertReligion : CompProperties_AbilityEffect
    {
        public float convertPowerFactor = 1f;

        public CompProperties_ConvertReligion()
        {
            compClass = typeof(CompAbilityEffect_ConvertReligion);
        }
    }

    public class CompAbilityEffect_ConvertReligion : CompAbilityEffect
    {
        public new CompProperties_ConvertReligion Props => (CompProperties_ConvertReligion)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) return false;
            if (!AbilityUtility.ValidateMustBeHuman(pawn, throwMessages, parent)) return false;
            if (!AbilityUtility.ValidateMustNotBeBaby(pawn, throwMessages, parent)) return false;
            if (!AbilityUtility.ValidateNoMentalState(pawn, throwMessages, parent)) return false;
            if (!AbilityUtility.ValidateIsConscious(pawn, throwMessages, parent)) return false;
            return true;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var recipient = target.Pawn;
            if (recipient == null) return false;

            var initiatorReligion = parent.pawn.GetReligionIdeo();
            var recipientReligion = recipient.GetReligionIdeo();

            // Deny if target shares religion with initiator. Matthew 6:9-15
            if (initiatorReligion != null && initiatorReligion == recipientReligion)
                return false;

            return base.CanApplyOn(target, dest);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var initiator = parent.pawn;
            var recipient = target.Pawn;
            if (recipient == null) return;

            var initiatorReligion = initiator.GetReligionIdeo();
            var recipientReligion = recipient.GetReligionIdeo();
            if (initiatorReligion == null) return;
            if (initiatorReligion == recipientReligion) return;

            // Atheist target: assign initiator's religion directly
            if (recipientReligion == null)
            {
                recipient.SetReligionIdeo(initiatorReligion);
                recipient.SetReligionCertainty(0.75f);

                Messages.Message(
                    recipient.LabelShortCap + " has been converted to " + initiatorReligion.name
                    + " by " + initiator.LabelShortCap + ".",
                    new LookTargets(new Pawn[] { initiator, recipient }),
                    MessageTypeDefOf.PositiveEvent);
                return;
            }

            float reduction = InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(initiator, recipient)
                              * Props.convertPowerFactor;
            float preCertainty = recipient.GetReligionCertainty();
            float newCertainty = Mathf.Clamp01(preCertainty - reduction);
            recipient.SetReligionCertainty(newCertainty);
            ReligionConversionTracker.CheckForConversion(recipient, newCertainty);

            Messages.Message(
                recipient.LabelShortCap + "'s Religious Certainty has been reduced to "
                + newCertainty.ToStringPercent() + " from " + preCertainty.ToStringPercent(),
                new LookTargets(new Pawn[] { initiator, recipient }),
                MessageTypeDefOf.NegativeEvent);
        }
    }
}
