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

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var recipient = target.Pawn;
            if (recipient == null) return false;

            var initiatorReligion = parent.pawn.GetReligionIdeo();
            var recipientReligion = recipient.GetReligionIdeo();

            // Deny if target shares religion with initiator
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
            if (initiatorReligion == null || recipientReligion == null) return;
            if (initiatorReligion == recipientReligion) return;

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
