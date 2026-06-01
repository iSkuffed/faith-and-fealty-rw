using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class SessionRegistry
    {
        public static Ideo CurrentIdeology { get; set; }
        public static Ideo CurrentReligion { get; set; }
        public static bool IsSessionActive { get; set; }

        public static void Clear()
        {
            CurrentIdeology = null;
            CurrentReligion = null;
            IsSessionActive = false;
        }

        /// <summary>
        /// Purge all wizard-created ideology/religion from the game.
        /// Called when the wizard is opened to ensure a clean slate.
        /// </summary>
        public static void Purge()
        {
            // 1. Remove old ideology from IdeoManager
            if (CurrentIdeology != null)
            {
                try
                {
                    if (Find.IdeoManager.IdeosListForReading.Contains(CurrentIdeology))
                        Find.IdeoManager.Remove(CurrentIdeology);
                }
                catch { } // Suppress "Faction contains ideo which was removed" — expected behavior
            }

            // 2. Remove old religion from IdeoManager and faction trackers
            if (CurrentReligion != null)
            {
                try
                {
                    if (Find.IdeoManager.IdeosListForReading.Contains(CurrentReligion))
                        Find.IdeoManager.Remove(CurrentReligion);
                }
                catch { } // Suppress faction reference error — expected behavior

                foreach (var faction in Find.FactionManager.AllFactions)
                {
                    if (faction.ideos == null) continue;
                    var minorField = AccessTools.Field(typeof(FactionIdeosTracker), "ideosMinor");
                    var minorList = minorField?.GetValue(faction.ideos) as List<Ideo>;
                    if (minorList != null)
                        minorList.Remove(CurrentReligion);
                }
            }

            // 3. Clear all caches
            ReligionIdeoTracker.ClearAll();
            PresetReligions.ClearCaches();

            // 4. Clear registry
            Clear();

            Log.Message("[IdeoRework] Session purged — old ideology/religion removed");
        }
    }
}
