using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class ReligionConversionTracker
    {
        public static void CheckForConversion(Pawn pawn, float newCertainty)
        {
            if (newCertainty > 0f) return;
            if (pawn == null || pawn.Dead) return;

            var oldReligion = pawn.GetReligionIdeo();
            Ideo targetReligion = FindAlternativeReligion(pawn);

            if (targetReligion == null)
            {
                targetReligion = PresetReligions.GetOrCreateAgnosticReligion();
            }

            if (targetReligion == null || targetReligion == oldReligion) return;

            pawn.SetReligionIdeo(targetReligion);
            pawn.SetReligionCertainty(0.5f);

            Messages.Message(
                $"{pawn.LabelShort} has lost faith in {oldReligion?.name ?? "their religion"} and converted to {targetReligion.name}.",
                pawn,
                MessageTypeDefOf.NegativeEvent
            );
        }

        private static Ideo FindAlternativeReligion(Pawn pawn)
        {
            var otherReligions = new System.Collections.Generic.HashSet<Ideo>();
            foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
            {
                if (p == pawn) continue;
                var r = p.GetReligionIdeo();
                if (r != null && r != pawn.GetReligionIdeo())
                    otherReligions.Add(r);
            }

            if (otherReligions.Count == 0) return null;
            return otherReligions.RandomElement();
        }
    }
}
