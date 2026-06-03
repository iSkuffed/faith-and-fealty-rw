using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class ReligionBelieverTracker
    {
        private static Dictionary<int, int> believerCounts = new Dictionary<int, int>();

        public static int GetBelieverCount(Ideo religionIdeo)
        {
            if (religionIdeo == null) return 0;
            return believerCounts.GetValueOrDefault(religionIdeo.id, 0);
        }

        public static void RecacheAll()
        {
            believerCounts.Clear();
            foreach (var religionIdeo in PresetReligions.CreatedReligionIdeos)
            {
                var countedPawns = new HashSet<int>();
                int count = 0;

                // Count spawned pawns
                foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep)
                {
                    if (pawn.GetReligionIdeo() == religionIdeo && !pawn.IsSlave && !pawn.IsQuestLodger())
                    {
                        countedPawns.Add(pawn.thingIDNumber);
                        count++;
                    }
                }

                // Also count starting pawns that may not be spawned yet
                var gameInit = Find.GameInitData;
                if (gameInit != null)
                {
                    foreach (var pawn in gameInit.startingAndOptionalPawns)
                    {
                        if (pawn != null && !countedPawns.Contains(pawn.thingIDNumber)
                            && pawn.GetReligionIdeo() == religionIdeo
                            && !pawn.IsSlave && !pawn.IsQuestLodger())
                        {
                            count++;
                        }
                    }
                }

                believerCounts[religionIdeo.id] = count;
            }
        }

        public static void OnReligionChanged(Pawn pawn, Ideo oldReligion, Ideo newReligion)
        {
            if (oldReligion != null && believerCounts.ContainsKey(oldReligion.id))
                believerCounts[oldReligion.id] = Math.Max(0, believerCounts[oldReligion.id] - 1);

            if (newReligion != null)
            {
                if (!believerCounts.ContainsKey(newReligion.id))
                    believerCounts[newReligion.id] = 0;
                believerCounts[newReligion.id]++;
            }
        }

        public static void OnPawnJoinedColony(Pawn pawn)
        {
            var religion = pawn.GetReligionIdeo();
            if (religion != null)
            {
                if (!believerCounts.ContainsKey(religion.id))
                    believerCounts[religion.id] = 0;
                believerCounts[religion.id]++;
            }
        }

        public static void OnPawnDied(Pawn pawn)
        {
            var religion = pawn.GetReligionIdeo();
            if (religion != null && believerCounts.ContainsKey(religion.id))
                believerCounts[religion.id] = Math.Max(0, believerCounts[religion.id] - 1);
        }
    }
}
