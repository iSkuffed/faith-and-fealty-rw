using RimWorld;
using Verse;
using Verse.AI;

namespace IdeoRework
{
    public class WorkGiver_Warden_ConvertReligion : WorkGiver_Warden
    {
        private static PrisonerInteractionModeDef ConvertReligionMode =>
            DefDatabase<PrisonerInteractionModeDef>.GetNamed("ConvertReligion");

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ShouldTakeCareOfPrisoner(pawn, t))
                return false;

            Pawn prisoner = (Pawn)t;

            if (prisoner.InMentalState)
            {
                JobFailReason.Is("PawnIsInMentalState".Translate(prisoner));
                return false;
            }

            if (prisoner.guest.IsInteractionEnabled(ConvertReligionMode) && !prisoner.guest.ScheduledForInteraction)
            {
                JobFailReason.Is("PrisonerInteractedTooRecently".Translate());
                return false;
            }

            var wardenReligion = pawn.GetReligionIdeo();
            var prisonerReligion = prisoner.GetReligionIdeo();

            if (wardenReligion != null && wardenReligion == prisonerReligion)
                return false;

            return base.HasJobOnThing(pawn, t, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ShouldTakeCareOfPrisoner(pawn, t))
                return null;

            Pawn prisoner = (Pawn)t;

            if (!prisoner.guest.IsInteractionEnabled(ConvertReligionMode))
                return null;

            if (!prisoner.guest.ScheduledForInteraction)
                return null;

            if (!prisoner.guest.IsPrisoner)
                return null;

            if (prisoner.Downed && !prisoner.InBed())
                return null;

            if (!prisoner.Awake())
                return null;

            var wardenReligion = pawn.GetReligionIdeo();
            if (wardenReligion == null)
                return null;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                return null;

            if (!pawn.CanReserve(t))
                return null;

            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PrisonerConvertReligion"), t);
        }
    }
}
