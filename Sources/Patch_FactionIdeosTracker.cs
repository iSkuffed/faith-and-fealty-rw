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
