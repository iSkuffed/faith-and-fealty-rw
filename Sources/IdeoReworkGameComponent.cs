using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public class IdeoReworkGameComponent : GameComponent
    {
        private List<int> savedReligionIdeoIds = new List<int>();

        public IdeoReworkGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref savedReligionIdeoIds, "savedReligionIdeoIds", LookMode.Value);
            if (savedReligionIdeoIds == null)
                savedReligionIdeoIds = new List<int>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            PresetReligions.CreatedReligionIdeos.Clear();
            foreach (var id in savedReligionIdeoIds)
            {
                var ideo = IdeoTrackerHelper.FindIdeoById(id);
                if (ideo != null)
                    PresetReligions.CreatedReligionIdeos.Add(ideo);
            }
            Log.Message($"[IdeoRework] FinalizeLoading: restored {PresetReligions.CreatedReligionIdeos.Count} religion ideos from {savedReligionIdeoIds.Count} saved IDs");
        }

        public static void SaveReligionIdeoIds()
        {
            var comp = Current.Game?.GetComponent<IdeoReworkGameComponent>();
            if (comp == null) return;
            comp.savedReligionIdeoIds.Clear();
            foreach (var ideo in PresetReligions.CreatedReligionIdeos)
                comp.savedReligionIdeoIds.Add(ideo.id);
        }
    }

    public static class IdeoTrackerHelper
    {
        public static Ideo FindIdeoById(int id)
        {
            foreach (var ideo in Find.IdeoManager.IdeosListForReading)
            {
                if (ideo.id == id)
                    return ideo;
            }
            return null;
        }
    }
}
