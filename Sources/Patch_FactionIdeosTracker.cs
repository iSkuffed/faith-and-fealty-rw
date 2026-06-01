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
            try
            {
                // Skip classic mode
                if (Find.IdeoManager != null && Find.IdeoManager.classicMode) return;

                var factionDef = parms.forFaction;
                Log.Message($"[IdeoRework] ChooseOrGenerateIdeo postfix: faction={factionDef?.defName}");

                if (factionDef == null) { Log.Warning("[IdeoRework] parms.forFaction is null"); return; }

                // Skip if faction already has a religion ideo
                if (FactionIdeoHelper.FindReligionIdeo(__instance) != null)
                {
                    Log.Message($"[IdeoRework] Faction {factionDef.defName} already has religion ideo, skipping");
                    return;
                }

                var preset = PresetReligions.GetReligionForFaction(factionDef);
                Log.Message($"[IdeoRework] Preset for {factionDef.defName}: {preset?.id ?? "null"}");

                if (preset == null) return;

                var religionIdeo = PresetReligions.CreateReligionIdeo(preset, factionDef);
                if (religionIdeo != null)
                {
                    FactionIdeoHelper.AddMinorIdeo(__instance, religionIdeo);
                    Log.Message($"[IdeoRework] Generated religion '{religionIdeo.name}' for faction '{factionDef.label}'");
                }
                else
                {
                    Log.Warning($"[IdeoRework] CreateReligionIdeo returned null for {factionDef.defName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] ChooseOrGenerateIdeo religion: " + ex.Message);
            }
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
