using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    // ── Helper for adding minor ideos via reflection ──────────────────────

    public static class FactionIdeoHelper
    {
        public static void AddMinorIdeo(FactionIdeosTracker tracker, Ideo ideo)
        {
            if (tracker == null || ideo == null) return;
            var minorField = AccessTools.Field(typeof(FactionIdeosTracker), "ideosMinor");
            var minorList = minorField?.GetValue(tracker) as List<Ideo>;
            if (minorList != null && !minorList.Contains(ideo))
                minorList.Add(ideo);
        }

        public static Ideo FindReligionIdeo(FactionIdeosTracker tracker)
        {
            if (tracker == null) return null;
            // Check against the tracking set — this matches only ideos we explicitly created as religions
            return tracker.AllIdeos.FirstOrDefault(i => PresetReligions.CreatedReligionIdeos.Contains(i));
        }
    }

    // ── Generate religion ideo for NPC factions ───────────────────────────

    [HarmonyPatch(typeof(FactionIdeosTracker))]
    [HarmonyPatch("ChooseOrGenerateIdeo")]
    public static class Patch_FactionIdeosTracker_ChooseOrGenerateIdeo
    {
        static void Postfix(FactionIdeosTracker __instance, IdeoGenerationParms parms)
        {
            // Religion creation for NPC factions is handled exclusively by FinalizeInit.
            // This postfix is intentionally a no-op to prevent duplicate religion creation
            // during world generation (before the wizard and FinalizeInit run).
        }
    }

    // ── Preserve religion ideo during recalculation ────────────────────────

    [HarmonyPatch(typeof(FactionIdeosTracker))]
    [HarmonyPatch("RecalculateIdeosBasedOnPlayerPawns")]
    public static class Patch_FactionIdeosTracker_Recalculate
    {
        private static Ideo savedReligion;

        static void Prefix(FactionIdeosTracker __instance)
        {
            try
            {
                savedReligion = FactionIdeoHelper.FindReligionIdeo(__instance);
            }
            catch { }
        }

        static void Finalizer(FactionIdeosTracker __instance)
        {
            try
            {
                if (savedReligion == null) return;

                // Re-add religion ideo if it was lost during recalculation
                if (!__instance.AllIdeos.Contains(savedReligion))
                {
                    FactionIdeoHelper.AddMinorIdeo(__instance, savedReligion);
                }

                savedReligion = null;
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] Recalculate religion preservation: " + ex.Message);
            }
        }
    }

    // ── Filter religion ideos from pawn generation pool ─────────────────────
    // Vanilla's GetRandomIdeoForNewPawn picks from AllIdeos with weighted random
    // (primary gets weight 4, each minor gets weight 1). Since the religion is in
    // the faction's minor ideos, it can be picked as a pawn's primary ideo ~20%
    // of the time. This prefix filters it out so no pawn ever gets the religion
    // as their primary — the religion is always assigned separately via
    // SetReligionIdeo() in HardOverride.

    [HarmonyPatch(typeof(FactionIdeosTracker))]
    [HarmonyPatch("GetRandomIdeoForNewPawn")]
    public static class Patch_FactionIdeosTracker_GetRandomIdeoForNewPawn
    {
        static bool Prefix(FactionIdeosTracker __instance, ref Ideo __result)
        {
            try
            {
                // Build a list of non-religion ideos from this faction's AllIdeos
                var validIdeos = new List<Ideo>();
                foreach (var ideo in __instance.AllIdeos)
                {
                    if (!PresetReligions.CreatedReligionIdeos.Contains(ideo))
                        validIdeos.Add(ideo);
                }

                // If all ideos are religions (shouldn't happen), let original run
                if (validIdeos.Count == 0)
                    return true;

                // Pick with same vanilla weighting: primary gets 4, minor gets 1
                __result = validIdeos.RandomElementByWeightWithFallback(
                    (Ideo x) => (__instance.PrimaryIdeo == x) ? 4f : 1f);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    // ── Register religion ideos with IdeoManager ──────────────────────────

    [HarmonyPatch(typeof(FactionIdeosTracker))]
    [HarmonyPatch("GetPrecept")]
    public static class Patch_FactionIdeosTracker_GetPrecept
    {
        static void Postfix(PreceptDef precept, ref Precept __result)
        {
            try
            {
                if (__result != null) return;

                foreach (var religionIdeo in IdeoRework.ReligionIdeoTracker.AllReligionIdeos())
                {
                    var found = religionIdeo.PreceptsListForReading.FirstOrDefault(p => p.def == precept);
                    if (found != null)
                    {
                        __result = found;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] FactionIdeosTracker.GetPrecept religion: " + ex.Message);
            }
        }
    }
}
