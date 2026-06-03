using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(LifeStageWorker_HumanlikeChild))]
    [HarmonyPatch("Notify_LifeStageStarted")]
    public static class Patch_LifeStageWorker_HumanlikeChild
    {
        static void Postfix(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.Dead) return;

                // Only handle pawns that just got an ideology (baby → child transition)
                if (pawn.Ideo == null) return;

                // Skip if already has religion
                if (pawn.GetReligionIdeo() != null) return;

                // Try to find a living parent in the colony
                var mother = pawn.GetMother();
                var father = pawn.GetFather();

                Ideo parentReligion = null;

                // Check if parents are alive and in the colony
                bool motherPresent = mother != null && !mother.Dead && mother.IsPlayerControlled && mother.GetReligionIdeo() != null;
                bool fatherPresent = father != null && !father.Dead && father.IsPlayerControlled && father.GetReligionIdeo() != null;

                if (motherPresent && fatherPresent)
                {
                    // Both present — 50/50 if different faiths, otherwise same
                    var motherReligion = mother.GetReligionIdeo();
                    var fatherReligion = father.GetReligionIdeo();
                    parentReligion = Rand.Bool ? motherReligion : fatherReligion;
                }
                else if (motherPresent)
                {
                    parentReligion = mother.GetReligionIdeo();
                }
                else if (fatherPresent)
                {
                    parentReligion = father.GetReligionIdeo();
                }

                // If no parent religion found, use colony majority
                if (parentReligion == null)
                {
                    parentReligion = GetColonyMajorityReligion();
                }

                if (parentReligion != null)
                {
                    pawn.SetReligionIdeo(parentReligion);
                    // Children are less certain than adults
                    pawn.SetReligionCertainty(Rand.Range(0.6f, 0.9f));
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[IdeoRework] Child religion inheritance: " + ex.Message);
            }
        }

        private static Ideo GetColonyMajorityReligion()
        {
            Ideo best = null;
            int bestCount = 0;

            foreach (var religionIdeo in PresetReligions.CreatedReligionIdeos)
            {
                int count = ReligionBelieverTracker.GetBelieverCount(religionIdeo);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = religionIdeo;
                }
            }

            return best;
        }
    }
}
