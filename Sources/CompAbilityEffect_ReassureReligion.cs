using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
// Psalms 23:4
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

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) return false;
            if (!AbilityUtility.ValidateMustBeHuman(pawn, throwMessages, parent)) return false;
            if (!AbilityUtility.ValidateNoMentalState(pawn, throwMessages, parent)) return false;
            // Same check as vanilla's ValidateSameIdeo, but for religion; otherwise Apply no-ops after the cooldown starts.
            var religion = parent.pawn.GetReligionIdeo();
            if (religion == null || pawn.GetReligionIdeo() != religion)
            {
                if (throwMessages)
                    Messages.Message(pawn.LabelShortCap + " does not share " + parent.pawn.LabelShortCap + "'s religion.",
                        pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

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
