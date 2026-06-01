using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public class PresetReligion
    {
        public string id;
        public string baseName;
        public string structureMeme;
        public List<string> normalMemes;
    }

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

        // ── The 4 preset religions ──────────────────────────────────────────

        public static readonly PresetReligion ChurchOfTheHolyLight = new PresetReligion
        {
            id = "holy_light",
            baseName = "Church of the Holy Light",
            structureMeme = "Structure_OriginChristian",
            normalMemes = new List<string> { "Proselytizer", "FleshPurity" }
        };

        public static readonly PresetReligion FaithOfTheCrescent = new PresetReligion
        {
            id = "crescent",
            baseName = "Faith of the Crescent",
            structureMeme = "Structure_OriginIslamic",
            normalMemes = new List<string> { "Proselytizer", "Collectivist" }
        };

        public static readonly PresetReligion TheOldWays = new PresetReligion
        {
            id = "old_ways",
            baseName = "The Old Ways",
            structureMeme = "Structure_Animist",
            normalMemes = new List<string> { "AnimalPersonhood", "TreeConnection" }
        };

        public static readonly PresetReligion ImperialCult = new PresetReligion
        {
            id = "imperial_cult",
            baseName = "Imperial Cult",
            structureMeme = "Structure_Archist",
            normalMemes = new List<string> { "Collectivist", "Loyalist", "HumanPrimacy" }
        };

        // ── Faction → Religion mapping ─────────────────────────────────────

        public static PresetReligion GetReligionForFaction(FactionDef def)
        {
            if (def == null) return ChurchOfTheHolyLight;

            // Empire gets Imperial Cult
            if (def.defName == "Empire")
                return ImperialCult;

            // Pirates and savage tribes get Old Ways
            if (def.defName.Contains("Pirate") || def.defName.Contains("Savage"))
                return TheOldWays;

            // HoraxCult already has forced memes — skip
            if (def.defName == "HoraxCult")
                return null;

            // Ancients already have forced memes — skip
            if (def.defName.Contains("Ancients"))
                return null;

            // TradersGuild/Salvagers already have forced Shipborn — skip
            if (def.defName == "TradersGuild" || def.defName == "Salvagers")
                return null;

            // Sanguophages — skip (forced Bloodfeeding)
            if (def.defName == "Sanguophages")
                return null;

            // Tribes get Crescent
            if (def.defName.Contains("Tribe"))
                return FaithOfTheCrescent;

            // Everything else (outlanders, player) gets Holy Light
            return ChurchOfTheHolyLight;
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

        public static string GenerateReligionName(PresetReligion preset)
        {
            try
            {
                Rand.PushState(Find.TickManager.TicksGame ^ preset.id.GetHashCode());

                string prefix, suffix;
                if (preset.id == "old_ways")
                {
                    prefix = PaganNamePrefixes.RandomElement();
                    suffix = PaganNameSuffixes.RandomElement();
                }
                else if (preset.id == "imperial_cult")
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

        public static List<MemeDef> GetValidReligionMemes(PresetReligion preset, FactionDef factionDef)
        {
            var result = new List<MemeDef>();

            // Add structure meme
            var structureDef = DefDatabase<MemeDef>.GetNamedSilentFail(preset.structureMeme);
            if (structureDef != null)
                result.Add(structureDef);

            // Add normal memes, filtering out any that conflict with faction
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

            return result;
        }

        // ── Create an Ideo from a preset ───────────────────────────────────

        public static Ideo CreateReligionIdeo(PresetReligion preset, FactionDef forFaction)
        {
            // Return cached ideo if one exists for this preset
            if (ReligionIdeoByPreset.TryGetValue(preset.id, out var cachedIdeo))
            {
                Log.Message($"[IdeoRework] Reusing cached religion '{cachedIdeo.name}' for faction '{forFaction?.defName}'");
                return cachedIdeo;
            }

            Log.Message($"[IdeoRework] CreateReligionIdeo: preset={preset.id}, faction={forFaction?.defName}");

            var memes = GetValidReligionMemes(preset, forFaction);
            Log.Message($"[IdeoRework] Valid memes: {string.Join(", ", memes.Select(m => m.defName))}");

            if (!memes.Any())
            {
                Log.Warning("[IdeoRework] No valid memes for religion " + preset.id + " faction " + forFaction?.defName);
                return null;
            }

            try
            {
                // Create ideo with foundation
                var parms = new IdeoGenerationParms(forFaction ?? FactionDefOf.PlayerColony);
                var ideo = IdeoGenerator.GenerateIdeo(parms);
                Log.Message($"[IdeoRework] Generated ideo: {ideo.name}, memes: {string.Join(", ", ideo.memes.Select(m => m.defName))}");

                // Override memes with our preset memes
                ideo.memes = new List<MemeDef>(memes);

                // Generate a randomized name
                ideo.name = GenerateReligionName(preset);
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
                ReligionIdeoByPreset[preset.id] = ideo;

                Log.Message($"[IdeoRework] Religion ideo created: '{ideo.name}' for faction '{forFaction?.defName}'");
                return ideo;
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] CreateReligionIdeo FAILED for {forFaction?.defName}: {ex}");
                return null;
            }
        }
    }
}
