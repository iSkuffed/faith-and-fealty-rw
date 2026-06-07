using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class HardOverride
    {
        // Tracks which pawns have already been processed by FixVisitorPawn to
        // ensure we never re-process the same pawn (e.g. on re-generation or
        // redress). This prevents overwriting a pawn that was intentionally
        // converted away from their faction's religion.
        private static readonly HashSet<int> _processedPawns = new HashSet<int>();

        public static void ClearProcessedPawns() => _processedPawns.Clear();
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

        // Post-generation check for visitor/NPC pawns. Runs once per new pawn
        // generation (e.g. when caravans arrive). Detects two issues:
        //   1. pawn.Ideo is the religion instead of the faction's primary ideo
        //   2. pawn is missing religion tracking (GetReligionIdeo returns null)
        // Uses _processedPawns to never run on the same pawn twice.
        public static void FixVisitorPawn(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.ideo == null) return;

                // Skip player pawns — those are handled by VerifyAndFixAllPlayerPawns
                if (pawn.IsPlayerControlled) return;
                if (pawn.Faction != null && pawn.Faction.IsPlayer) return;

                // Skip hidden factions (Ancients, mechanoids, insects, etc.)
                var faction = pawn.Faction;
                if (faction == null || faction.ideos == null) return;
                if (faction.def.hidden) return;

                // Only act if this faction has a religion assigned
                var religion = FactionIdeoHelper.FindReligionIdeo(faction.ideos);
                if (religion == null) return;

                // Skip if we already processed this pawn (never re-process)
                if (!_processedPawns.Add(pawn.thingIDNumber)) return;

                // Fix mirrored entry: pawn.Ideo should be the faction's primary,
                // not the religion. The religion is tracked separately via SetReligionIdeo.
                if (pawn.Ideo == religion)
                {
                    var correctIdeo = faction.ideos.PrimaryIdeo;
                    if (correctIdeo != null && correctIdeo != religion)
                        pawn.ideo.SetIdeo(correctIdeo);
                }

                // Fix missing entry: pawn should have religion tracking assigned
                if (pawn.GetReligionIdeo() == null)
                {
                    pawn.SetReligionIdeo(religion);
                    if (pawn.GetReligionCertainty() <= 0f)
                        pawn.SetReligionCertainty(Rand.Range(0.75f, 1.0f));
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[IdeoRework] FixVisitorPawn failed: {ex}");
            }
        }
    }
}
