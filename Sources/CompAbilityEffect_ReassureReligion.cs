using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public class CompProperties_ReassureReligion : CompProperties_AbilityEffect
    {
        public float baseCertaintyGain = 0.2f;

        public CompProperties_ReassureReligion()
        {
            compClass = typeof(CompAbilityEffect_ReassureReligion);
        }
    }

    public class CompAbilityEffect_ReassureReligion : CompAbilityEffect
    {
        public new CompProperties_ReassureReligion Props => (CompProperties_ReassureReligion)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var initiator = parent.pawn;
            var recipient = target.Pawn;
            if (recipient == null) return;

            var initiatorReligion = initiator.GetReligionIdeo();
            var recipientReligion = recipient.GetReligionIdeo();
            if (initiatorReligion == null || recipientReligion == null) return;
            if (initiatorReligion != recipientReligion) return;

            float preCertainty = recipient.GetReligionCertainty();
            float newCertainty = Mathf.Clamp01(preCertainty + Props.baseCertaintyGain);
            recipient.SetReligionCertainty(newCertainty);

            Messages.Message(
                recipient.LabelShortCap + "'s Religious Certainty has been increased from "
                + preCertainty.ToStringPercent() + " to " + newCertainty.ToStringPercent(),
                new LookTargets(new Pawn[] { initiator, recipient }),
                MessageTypeDefOf.PositiveEvent);
        }
    }
}
