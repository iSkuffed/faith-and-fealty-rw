using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace IdeoRework
{
    public class JobDriver_ConvertReligionPrisoner : JobDriver
    {
        private const int NumTalks = 3;

        private static PrisonerInteractionModeDef ConvertReligionMode =>
            DefDatabase<PrisonerInteractionModeDef>.GetNamed("ConvertReligion");

        protected Pawn Prisoner => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnMentalState(TargetIndex.A);
            this.FailOnNotAwake(TargetIndex.A);
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.PrisonerIsSecure);

            for (int i = 0; i < NumTalks; i++)
            {
                yield return Toils_Interpersonal.GotoPrisoner(pawn, Prisoner, ConvertReligionMode);
                yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
                yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
                yield return Toils_Interpersonal.ConvinceRecruitee(pawn, Prisoner, InteractionDefOf.Chitchat);
            }

            yield return Toils_Interpersonal.GotoPrisoner(pawn, Prisoner, ConvertReligionMode);
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A)
                .FailOn(() => !Prisoner.guest.ScheduledForInteraction);
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
            yield return TryConvertReligion(TargetIndex.A);
        }

        private static Toil TryConvertReligion(TargetIndex prisonerInd)
        {
            Toil toil = ToilMaker.MakeToil("TryConvertReligion");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Pawn prisoner = (Pawn)actor.jobs.curJob.GetTarget(prisonerInd).Thing;

                var wardenReligion = actor.GetReligionIdeo();
                if (wardenReligion == null) return;

                var prisonerReligion = prisoner.GetReligionIdeo();

                if (prisonerReligion == null)
                {
                    prisoner.SetReligionIdeo(wardenReligion);
                    prisoner.SetReligionCertainty(0.75f);

                    Messages.Message(
                        prisoner.LabelShortCap + " has been converted to " + wardenReligion.name + " by " + actor.LabelShortCap + ".",
                        new LookTargets(new Pawn[] { actor, prisoner }),
                        MessageTypeDefOf.PositiveEvent);

                    prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.MaintainOnly);
                    return;
                }

                if (prisonerReligion == wardenReligion)
                {
                    prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.MaintainOnly);
                    return;
                }

                float reduction = InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(actor, prisoner);
                float preCertainty = prisoner.GetReligionCertainty();
                float newCertainty = Mathf.Clamp01(preCertainty - reduction);
                prisoner.SetReligionCertainty(newCertainty);

                ReligionConversionTracker.CheckForConversion(prisoner, newCertainty, wardenReligion);

                Messages.Message(
                    prisoner.LabelShortCap + "'s religious certainty has been reduced to " + newCertainty.ToStringPercent() + " from " + preCertainty.ToStringPercent() + ".",
                    new LookTargets(new Pawn[] { actor, prisoner }),
                    MessageTypeDefOf.NegativeEvent);

                if (prisoner.GetReligionIdeo() == wardenReligion)
                {
                    prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.MaintainOnly);
                }
            };
            toil.socialMode = RandomSocialMode.Off;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 350;
            toil.activeSkill = () => SkillDefOf.Social;
            return toil;
        }
    }
}
