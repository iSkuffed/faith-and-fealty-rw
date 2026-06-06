using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class HardOverride
    {
        public static void AssignToAllPlayerPawns(Ideo ideology, Ideo religion)
        {
            try
            {
                // 1. Set ideology as primary
                Faction.OfPlayer.ideos.SetPrimary(ideology);
                ideology.initialPlayerIdeo = true;

                // 2. Register both in IdeoManager
                if (!Find.IdeoManager.IdeosListForReading.Contains(ideology))
                    Find.IdeoManager.Add(ideology);
                if (!Find.IdeoManager.IdeosListForReading.Contains(religion))
                    Find.IdeoManager.Add(religion);

                // 3. Add religion to faction's minor ideos
                FactionIdeoHelper.AddMinorIdeo(Faction.OfPlayer.ideos, religion);

                // 4. Assign religion to ALL player pawns
                foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
                {
                    if (pawn?.ideo != null)
                    {
                        pawn.SetReligionIdeo(religion);
                        pawn.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
                    }
                }

                // 5. Also assign to starting pawns from GameInitData (they may not be in AllMaps yet)
                var gameInit = Find.GameInitData;
                if (gameInit != null)
                {
                    foreach (var pawn in gameInit.startingAndOptionalPawns)
                    {
                        if (pawn?.ideo != null)
                        {
                            pawn.SetReligionIdeo(religion);
                            pawn.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
                        }
                    }
                }

                // 7. Fix any pawn whose primary ideo is incorrectly set to the religion
                foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
                {
                    if (pawn?.ideo != null && pawn.Ideo == religion)
                    {
                        pawn.ideo.SetIdeo(ideology);
                    }
                }
                if (gameInit != null)
                {
                    foreach (var pawn in gameInit.startingAndOptionalPawns)
                    {
                        if (pawn?.ideo != null && pawn.Ideo == religion)
                        {
                            pawn.ideo.SetIdeo(ideology);
                        }
                    }
                }

                // 8. Cache religion believer counts
                ReligionBelieverTracker.RecacheAll();

                Log.Message($"[IdeoRework] HardOverride complete: ideology='{ideology.name}', religion='{religion.name}'");
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] HardOverride failed: {ex}");
            }
        }

        public static void VerifyAndFixAllPlayerPawns()
        {
            try
            {
                var tracker = Faction.OfPlayer?.ideos;
                if (tracker == null) return;
                var ideology = tracker.PrimaryIdeo;
                var religion = FactionIdeoHelper.FindReligionIdeo(tracker);
                if (ideology == null || religion == null) return;

                foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
                {
                    if (pawn?.ideo == null) continue;

                    if (pawn.Ideo == religion)
                    {
                        pawn.ideo.SetIdeo(ideology);
                    }

                    if (pawn.GetReligionIdeo() == null)
                    {
                        pawn.SetReligionIdeo(religion);
                        if (pawn.GetReligionCertainty() <= 0f)
                            pawn.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[IdeoRework] VerifyAndFixAllPlayerPawns failed: {ex}");
            }
        }
    }
}
