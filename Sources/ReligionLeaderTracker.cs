using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class ReligionLeaderTracker
    {
        private static Pawn religionLeader;
        private static string leaderTitleMale;
        private static string leaderTitleFemale;

        public static Pawn ReligionLeader => religionLeader;

        public static void SetReligionLeader(Pawn pawn, Ideo religionIdeo)
        {
            religionLeader = pawn;
            leaderTitleMale = religionIdeo?.leaderTitleMale ?? "Religious Leader";
            leaderTitleFemale = religionIdeo?.leaderTitleFemale ?? leaderTitleMale;
            IdeoReworkGameComponent.SaveReligionLeaderId();
        }

        public static string GetLeaderTitle()
        {
            if (religionLeader == null) return null;
            return religionLeader.gender == Gender.Female && !leaderTitleFemale.NullOrEmpty()
                ? leaderTitleFemale : leaderTitleMale;
        }

        public static void Clear()
        {
            religionLeader = null;
            leaderTitleMale = null;
            leaderTitleFemale = null;
            IdeoReworkGameComponent.SaveReligionLeaderId();
        }

        public static void RestoreFromId(int pawnId, Ideo religionIdeo)
        {
            if (pawnId < 0) return;
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
            {
                if (pawn.thingIDNumber == pawnId)
                {
                    religionLeader = pawn;
                    leaderTitleMale = religionIdeo?.leaderTitleMale ?? "Religious Leader";
                    leaderTitleFemale = religionIdeo?.leaderTitleFemale ?? leaderTitleMale;
                    Log.Message($"[IdeoRework] Restored religion leader: {pawn.LabelShort} as {GetLeaderTitle()}");
                    return;
                }
            }
        }
    }
}
