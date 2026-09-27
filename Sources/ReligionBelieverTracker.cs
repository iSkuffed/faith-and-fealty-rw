using RimWorld;
using Verse;

namespace IdeoRework
{
    // Religion believer counts live in vanilla's Ideo.colonistBelieverCountCached, which
    // Patch_Ideo_RecacheColonistBelieverCount fills with free colonists of that religion.
    // Vanilla only recounts an ideo when pawn.Ideo changes, so these hooks trigger the
    // recount when a pawn's *religion* membership in the colony changes.
    public static class ReligionBelieverTracker
    {
        public static int GetBelieverCount(Ideo religionIdeo)
        {
            return religionIdeo?.ColonistBelieverCountCached ?? 0;
        }

        public static void RecacheAll()
        {
            foreach (var religionIdeo in PresetReligions.CreatedReligionIdeos)
                religionIdeo.RecacheColonistBelieverCount();
        }

        public static void OnReligionChanged(Pawn pawn, Ideo oldReligion, Ideo newReligion)
        {
            // Only colonists count; skip the recount for every generated NPC.
            if (oldReligion == newReligion || pawn?.Faction == null || !pawn.Faction.IsPlayer) return;
            oldReligion?.RecacheColonistBelieverCount();
            newReligion?.RecacheColonistBelieverCount();
        }

        public static void OnPawnJoinedColony(Pawn pawn)
        {
            pawn?.GetReligionIdeo()?.RecacheColonistBelieverCount();
        }

        public static void OnPawnDied(Pawn pawn)
        {
            pawn?.GetReligionIdeo()?.RecacheColonistBelieverCount();
        }
    }
}
