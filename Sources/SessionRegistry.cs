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
            // 1. Remove old ideology from faction tracker before removing from IdeoManager
            if (CurrentIdeology != null)
            {
                try
                {
                    // Clear primary ideo from player faction if it references the old ideology
                    var playerIdeos = Faction.OfPlayer?.ideos;
                    if (playerIdeos != null)
                    {
                        var primaryField = AccessTools.Field(typeof(FactionIdeosTracker), "primaryIdeo");
                        var currentPrimary = primaryField?.GetValue(playerIdeos) as Ideo;
                        if (currentPrimary == CurrentIdeology)
                        {
                            primaryField.SetValue(playerIdeos, null);
                        }
                    }

                    // Now safe to remove from IdeoManager
                    if (Find.IdeoManager.IdeosListForReading.Contains(CurrentIdeology))
                        Find.IdeoManager.Remove(CurrentIdeology);
                }
                catch { }
            }

            // 2. Remove old religion from faction trackers, then IdeoManager
            if (CurrentReligion != null)
            {
                // Remove from faction trackers FIRST
                foreach (var faction in Find.FactionManager.AllFactions)
                {
                    if (faction.ideos == null) continue;
                    var minorField = AccessTools.Field(typeof(FactionIdeosTracker), "ideosMinor");
                    var minorList = minorField?.GetValue(faction.ideos) as List<Ideo>;
                    if (minorList != null)
                        minorList.Remove(CurrentReligion);
                }

                // Now safe to remove from IdeoManager
                try
                {
                    if (Find.IdeoManager.IdeosListForReading.Contains(CurrentReligion))
                        Find.IdeoManager.Remove(CurrentReligion);
                }
                catch { }
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
