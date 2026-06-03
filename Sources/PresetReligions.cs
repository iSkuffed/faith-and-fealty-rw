using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class PresetReligions
    {
        // ── Tracking set for religion ideos we've created ──────────────────
        public static readonly HashSet<Ideo> CreatedReligionIdeos = new HashSet<Ideo>();

        // ── Cache for shared religion ideos by preset ─────────────────────
        private static readonly Dictionary<string, Ideo> ReligionIdeoByPreset = new Dictionary<string, Ideo>();

        // ── Player's custom religion ideo (from wizard) ───────────────────
        public static Ideo PlayerReligionIdeo { get; set; }

        // ── Clear all caches (called when player goes back to remake) ────
        public static void ClearCaches()
        {
            CreatedReligionIdeos.Clear();
            ReligionIdeoByPreset.Clear();
            PlayerReligionIdeo = null;
        }

        // ── Faction → Religion mapping (from XML) ─────────────────────────

        public static ReligionPresetDef GetReligionForFaction(FactionDef def)
        {
            if (def == null) return null;

            // Get all presets from XML
            var presets = DefDatabase<ReligionPresetDef>.AllDefsListForReading;
            if (presets == null || presets.Count == 0) return null;

            // Find matching preset based on faction whitelist/blacklist
            foreach (var preset in presets)
            {
                // Check whitelist (if set, faction must be in list)
                if (preset.factionWhitelist != null && preset.factionWhitelist.Count > 0)
                {
                    if (!preset.factionWhitelist.Contains(def.defName))
                        continue;
                }

                // Check blacklist (if set, faction must not be in list)
                if (preset.factionBlacklist != null && preset.factionBlacklist.Count > 0)
                {
                    if (preset.factionBlacklist.Contains(def.defName))
                        continue;
                }

                return preset;
            }

            // No matching preset — return first preset without whitelist
            return presets.FirstOrDefault(p => p.factionWhitelist == null || p.factionWhitelist.Count == 0);
        }

        // ── Name randomization ─────────────────────────────────────────────

        private static readonly string[] NamePrefixes = {
            "The Holy", "The Sacred", "The Blessed", "The Divine",
            "The Sacred Order of", "The Blessed Fellowship of",
            "The Holy Communion of", "The Divine Light of"
        };

        private static readonly string[] PaganNamePrefixes = {
            "The Old", "The Ancient", "The Primal", "The Sacred Grove of",
            "The Circle of", "The Keepers of", "The Elders of"
        };

        private static readonly string[] ImperialNamePrefixes = {
            "The Imperial", "The Eternal", "The Sovereign", "The Divine Emperor's",
            "The Holy Order of", "The Celestial"
        };

        private static readonly string[] NameSuffixes = {
            "Light", "Dawn", "Grace", "Faith", "Truth", "Spirit",
            "Flame", "Star", "Crown", "Shield", "Blade", "Cross"
        };

        private static readonly string[] PaganNameSuffixes = {
            "Woods", "Stone", "River", "Moon", "Stars", "Roots",
            "Flame", "Wind", "Earth", "Sky", "Hunt", "Ancestors"
        };

        private static readonly string[] ImperialNameSuffixes = {
            "Throne", "Crown", "Empire", "Blade", "Star", "Dominion",
            "Scepter", "Phoenix", "Eternity", "Ascension"
        };

        public static string GenerateReligionName(ReligionPresetDef preset)
        {
            try
            {
                Rand.PushState(Find.TickManager.TicksGame ^ preset.defName.GetHashCode());

                string prefix, suffix;
                if (preset.defName == "Preset_OldWays")
                {
                    prefix = PaganNamePrefixes.RandomElement();
                    suffix = PaganNameSuffixes.RandomElement();
                }
                else if (preset.defName == "Preset_ImperialCult")
                {
                    prefix = ImperialNamePrefixes.RandomElement();
                    suffix = ImperialNameSuffixes.RandomElement();
                }
                else
                {
                    prefix = NamePrefixes.RandomElement();
                    suffix = NameSuffixes.RandomElement();
                }

                Rand.PopState();
                return prefix + " " + suffix;
            }
            catch
            {
                return preset.baseName;
            }
        }

        // ── Meme conflict validation ────────────────────────────────────────

        public static List<MemeDef> GetValidReligionMemes(ReligionPresetDef preset, FactionDef factionDef)
        {
            var result = new List<MemeDef>();

            // Add structure meme
            if (!preset.structureMeme.NullOrEmpty())
            {
                var structureDef = DefDatabase<MemeDef>.GetNamedSilentFail(preset.structureMeme);
                if (structureDef != null)
                    result.Add(structureDef);
            }

            // Add normal memes, filtering out any that conflict with faction
            if (preset.normalMemes != null)
            {
                foreach (var memeName in preset.normalMemes)
                {
                    var memeDef = DefDatabase<MemeDef>.GetNamedSilentFail(memeName);
                    if (memeDef == null) continue;

                    // Check faction disallowed memes
                    if (factionDef != null && factionDef.disallowedMemes != null &&
                        factionDef.disallowedMemes.Contains(memeDef))
                    {
                        Log.Message($"[IdeoRework] Skipping meme {memeName} for faction {factionDef.defName} (disallowed)");
                        continue;
                    }

                    result.Add(memeDef);
                }
            }

            return result;
        }

        // ── Create an Ideo from a preset ───────────────────────────────────

        public static Ideo CreateReligionIdeo(ReligionPresetDef preset, FactionDef forFaction)
        {
            // Return cached ideo if one exists for this preset
            if (ReligionIdeoByPreset.TryGetValue(preset.defName, out var cachedIdeo))
            {
                Log.Message($"[IdeoRework] Reusing cached religion '{cachedIdeo.name}' for faction '{forFaction?.defName}'");
                return cachedIdeo;
            }

            Log.Message($"[IdeoRework] CreateReligionIdeo: preset={preset.defName}, faction={forFaction?.defName}");

            var memes = GetValidReligionMemes(preset, forFaction);
            Log.Message($"[IdeoRework] Valid memes: {string.Join(", ", memes.Select(m => m.defName))}");

            try
            {
                // Create ideo with foundation
                var parms = new IdeoGenerationParms(forFaction ?? FactionDefOf.PlayerColony);
                var ideo = IdeoGenerator.GenerateIdeo(parms);
                Log.Message($"[IdeoRework] Generated ideo: {ideo.name}, memes: {string.Join(", ", ideo.memes.Select(m => m.defName))}");

                // Override memes with our preset memes
                ideo.memes = new List<MemeDef>(memes);

                // Apply deity name maker override to structure meme (if preset specifies one)
                // This controls the grammar pack used for deity name generation
                if (!preset.deityNameMakerOverride.NullOrEmpty())
                {
                    var structureMeme = ideo.StructureMeme;
                    if (structureMeme != null)
                    {
                        var rulePack = DefDatabase<RulePackDef>.GetNamedSilentFail(preset.deityNameMakerOverride);
                        if (rulePack != null)
                            structureMeme.deityNameMakerOverride = rulePack;
                    }
                }

                // Generate deities (uses structure meme's deityCount + deityNameMakerOverride)
                if (ideo.foundation is IdeoFoundation_Deity deityFoundation)
                {
                    try { deityFoundation.GenerateDeities(); } catch { }
                }

                // Generate a randomized name
                ideo.name = GenerateReligionName(preset);

                // Set leader titles if specified
                if (!preset.leaderTitleMale.NullOrEmpty())
                    ideo.leaderTitleMale = preset.leaderTitleMale;
                if (!preset.leaderTitleFemale.NullOrEmpty())
                    ideo.leaderTitleFemale = preset.leaderTitleFemale;

                // Set description if specified
                if (!preset.description.NullOrEmpty())
                    ideo.description = preset.description;

                Log.Message($"[IdeoRework] Override memes: {string.Join(", ", ideo.memes.Select(m => m.defName))}");

                // Recache precepts for the new memes
                try { ideo.RecachePrecepts(); } catch (Exception ex) { Log.Warning("[IdeoRework] RecachePrecepts religion: " + ex.Message); }

                // Register with IdeoManager
                if (!Find.IdeoManager.IdeosListForReading.Contains(ideo))
                    Find.IdeoManager.Add(ideo);

                // Track as a religion ideo
                CreatedReligionIdeos.Add(ideo);

                // Persist religion ideo IDs for save/load
                IdeoReworkGameComponent.SaveReligionIdeoIds();

                // Cache by preset for sharing across factions
                ReligionIdeoByPreset[preset.defName] = ideo;

                Log.Message($"[IdeoRework] Religion ideo created: '{ideo.name}' for faction '{forFaction?.defName}'");
                return ideo;
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] CreateReligionIdeo FAILED for {forFaction?.defName}: {ex}");
                return null;
            }
        }

        // ── Create Agnostic religion (no memes, no precepts) ─────────────

        public static Ideo CreateAgnosticReligion(FactionDef forFaction)
        {
            // Check if already created
            var existing = CreatedReligionIdeos.FirstOrDefault(i => i.name == "Agnostic");
            if (existing != null) return existing;

            try
            {
                var parms = new IdeoGenerationParms(forFaction ?? FactionDefOf.PlayerColony);
                var ideo = IdeoGenerator.GenerateIdeo(parms);

                // Blank placeholder — no memes, no precepts
                ideo.memes = new List<MemeDef>();
                ideo.ClearPrecepts();
                ideo.name = "Agnostic";
                ideo.description = "Those who hold no strong beliefs about the nature of existence. They remain open to many possibilities but commit to none.";
                ideo.memberName = "Agnostic";
                ideo.adjective = "agnostic";
                ideo.leaderTitleMale = "Speaker";
                ideo.leaderTitleFemale = "Speaker";

                // Register with IdeoManager
                if (!Find.IdeoManager.IdeosListForReading.Contains(ideo))
                    Find.IdeoManager.Add(ideo);

                // Track as a religion ideo
                CreatedReligionIdeos.Add(ideo);

                // Persist religion ideo IDs for save/load
                IdeoReworkGameComponent.SaveReligionIdeoIds();

                Log.Message($"[IdeoRework] Agnostic religion created");
                return ideo;
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] CreateAgnosticReligion FAILED: {ex}");
                return null;
            }
        }

        // ── Get or create Agnostic religion ─────────────────────────────────

        public static Ideo GetOrCreateAgnosticReligion()
        {
            var existing = CreatedReligionIdeos.FirstOrDefault(i => i.name == "Agnostic");
            if (existing != null) return existing;

            return CreateAgnosticReligion(FactionDefOf.PlayerColony);
        }

    }
}
