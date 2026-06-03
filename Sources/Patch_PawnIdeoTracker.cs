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
            var oldReligion = pawn.GetReligionIdeo();
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
            // Update believer count tracker
            ReligionBelieverTracker.OnReligionChanged(pawn, oldReligion, ideo);
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
        // Persist religion data across separate ExposeData calls (LoadingVars → PostLoadInit)
        private static readonly Dictionary<int, (int religionIdeoId, float certainty)> PendingReligionData
            = new Dictionary<int, (int, float)>();

        static void Postfix(Pawn_IdeoTracker __instance)
        {
            try
            {
                var pawnField = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) return;

                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    var religionIdeo = pawn.GetReligionIdeo();
                    var religionCertainty = pawn.GetReligionCertainty();
                    int religionIdeoId = religionIdeo?.id ?? -1;
                    Scribe_Values.Look(ref religionIdeoId, "religionIdeoId", -1);
                    Scribe_Values.Look(ref religionCertainty, "religionCertainty", 0f);
                }
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    int religionIdeoId = -1;
                    float religionCertainty = 0f;
                    Scribe_Values.Look(ref religionIdeoId, "religionIdeoId", -1);
                    Scribe_Values.Look(ref religionCertainty, "religionCertainty", 0f);
                    PendingReligionData[pawn.thingIDNumber] = (religionIdeoId, religionCertainty);
                }
                else if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    Ideo religionIdeo = null;
                    float religionCertainty = 0f;

                    if (PendingReligionData.TryGetValue(pawn.thingIDNumber, out var data))
                    {
                        religionCertainty = data.certainty;
                        if (data.religionIdeoId >= 0)
                        {
                            foreach (var candidate in Find.IdeoManager.IdeosListForReading)
                            {
                                if (candidate.id == data.religionIdeoId)
                                {
                                    religionIdeo = candidate;
                                    break;
                                }
                            }
                        }
                        PendingReligionData.Remove(pawn.thingIDNumber);
                    }

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
