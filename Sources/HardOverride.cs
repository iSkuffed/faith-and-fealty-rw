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
                        pawn.SetReligionCertainty(1.0f);
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
                            pawn.SetReligionCertainty(1.0f);
                        }
                    }
                }

                Log.Message($"[IdeoRework] HardOverride complete: ideology='{ideology.name}', religion='{religion.name}'");
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] HardOverride failed: {ex}");
            }
        }
    }
}
