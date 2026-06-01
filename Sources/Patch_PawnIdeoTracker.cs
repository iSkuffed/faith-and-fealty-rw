using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    // ── Pawn-level religion ideo storage ──────────────────────────────────

    public static class ReligionIdeoTracker
    {
        private static readonly Dictionary<Pawn, Ideo> ReligionIdeos = new Dictionary<Pawn, Ideo>();
        private static readonly Dictionary<Pawn, float> ReligionCertainties = new Dictionary<Pawn, float>();

        public static Ideo GetReligionIdeo(this Pawn pawn)
        {
            if (pawn == null) return null;
            ReligionIdeos.TryGetValue(pawn, out var ideo);
            return ideo;
        }

        public static void SetReligionIdeo(this Pawn pawn, Ideo ideo)
        {
            if (pawn == null) return;
            if (ideo == null)
            {
                ReligionIdeos.Remove(pawn);
                ReligionCertainties.Remove(pawn);
            }
            else
            {
                ReligionIdeos[pawn] = ideo;
                if (!ReligionCertainties.ContainsKey(pawn))
                    ReligionCertainties[pawn] = 1.0f;
            }
        }

        public static float GetReligionCertainty(this Pawn pawn)
        {
            if (pawn == null) return 0f;
            ReligionCertainties.TryGetValue(pawn, out var c);
            return c;
        }

        public static void SetReligionCertainty(this Pawn pawn, float certainty)
        {
            if (pawn == null) return;
            ReligionCertainties[pawn] = Mathf.Clamp01(certainty);
        }

        public static void ClearAll()
        {
            ReligionIdeos.Clear();
            ReligionCertainties.Clear();
        }

        public static IEnumerable<Ideo> AllReligionIdeos()
        {
            var seen = new HashSet<Ideo>();
            foreach (var ideo in ReligionIdeos.Values)
            {
                if (ideo != null && seen.Add(ideo))
                    yield return ideo;
            }
        }
    }

    // ── Save/Load religion ideo ───────────────────────────────────────────

    [HarmonyPatch(typeof(Pawn_IdeoTracker))]
    [HarmonyPatch("ExposeData")]
    public static class Patch_PawnIdeoTracker_ExposeData
    {
        static void Postfix(Pawn_IdeoTracker __instance)
        {
            try
            {
                var pawnField = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) return;

                Ideo religionIdeo = null;
                float religionCertainty = 0f;
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    religionIdeo = pawn.GetReligionIdeo();
                    religionCertainty = pawn.GetReligionCertainty();
                }

                Scribe_Deep.Look(ref religionIdeo, "religionIdeoReligion");
                Scribe_Values.Look(ref religionCertainty, "religionCertainty", 0f);

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    pawn.SetReligionIdeo(religionIdeo);
                    pawn.SetReligionCertainty(religionCertainty);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] ExposeData religion ideo: " + ex.Message);
            }
        }
    }

    // ── Auto-assign religion ideo to pawns when they join a faction ────────

    [HarmonyPatch(typeof(Pawn_IdeoTracker))]
    [HarmonyPatch("SetIdeo")]
    public static class Patch_PawnIdeoTracker_SetIdeo
    {
        static void Postfix(Pawn_IdeoTracker __instance, Ideo ideo)
        {
            try
            {
                // Skip during world gen
                if (Find.World == null) return;

                var pawnField = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null || pawn.Faction == null || !pawn.Faction.IsPlayer) return;

                // Only assign if pawn doesn't already have a religion ideo
                var religionIdeo = pawn.GetReligionIdeo();
                if (religionIdeo != null) return;

                // For player pawns, prefer the player's custom religion
                if (PresetReligions.PlayerReligionIdeo != null)
                {
                    pawn.SetReligionIdeo(PresetReligions.PlayerReligionIdeo);
                    pawn.SetReligionCertainty(1.0f);
                    return;
                }

                // For NPC pawns, search all created religion ideos
                foreach (var candidate in PresetReligions.CreatedReligionIdeos)
                {
                    pawn.SetReligionIdeo(candidate);
                    pawn.SetReligionCertainty(1.0f);
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] SetIdeo religion assignment: " + ex.Message);
            }
        }
    }

    // ── Tick religion ideo precepts ────────────────────────────────────────

    [HarmonyPatch(typeof(Pawn_IdeoTracker))]
    [HarmonyPatch("IdeoTrackerTickInterval")]
    public static class Patch_PawnIdeoTracker_Tick
    {
        static void Postfix(Pawn_IdeoTracker __instance, int delta)
        {
            try
            {
                var pawnField = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) return;

                var religionIdeo = pawn.GetReligionIdeo();
                if (religionIdeo == null) return;

                foreach (var precept in religionIdeo.PreceptsListForReading)
                {
                    try
                    {
                        precept.Tick();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[IdeoRework] Precept.Tick error for {precept.def?.defName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] IdeoTrackerTickInterval religion: " + ex.Message);
            }
        }
    }
}
