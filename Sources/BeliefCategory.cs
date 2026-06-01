using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public enum BeliefCategory
    {
        Religion,
        Ideology
    }

    public static class BeliefCategoryLookup
    {
        private static Dictionary<string, BeliefCategory> MemeCategories;

        /// <summary>
        /// Build the mapping from XML defs. Called at startup.
        /// </summary>
        public static void Initialize()
        {
            MemeCategories = new Dictionary<string, BeliefCategory>();
            foreach (var mapping in DefDatabase<BeliefCategoryMappingDef>.AllDefsListForReading)
            {
                if (mapping.entries == null) continue;
                foreach (var entry in mapping.entries)
                {
                    if (!string.IsNullOrEmpty(entry.memeDefName))
                        MemeCategories[entry.memeDefName] = entry.category;
                }
            }
            Log.Message($"[IdeoRework] Loaded {MemeCategories.Count} meme → belief category mappings from XML");
        }

        public static BeliefCategory GetCategory(MemeDef meme)
        {
            if (MemeCategories != null && MemeCategories.TryGetValue(meme.defName, out var cat))
                return cat;
            return BeliefCategory.Religion;
        }

        public static List<MemeDef> ReligionMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => MemeCategories != null && MemeCategories.TryGetValue(m.defName, out var cat) && cat == BeliefCategory.Religion)
                    .ToList();
            }
        }

        public static List<MemeDef> IdeologyMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => MemeCategories != null && MemeCategories.TryGetValue(m.defName, out var cat) && cat == BeliefCategory.Ideology)
                    .ToList();
            }
        }

        public static bool IsReligionIdeo(Ideo ideo)
        {
            if (ideo == null || ideo.memes == null) return false;
            // An ideo is a "religion" if its structure meme is in the Religion category
            foreach (var meme in ideo.memes)
            {
                if (meme.category == MemeCategory.Structure)
                    return GetCategory(meme) == BeliefCategory.Religion;
            }
            // No structure meme found — check if majority are religion memes
            int religionCount = 0;
            foreach (var meme in ideo.memes)
            {
                if (GetCategory(meme) == BeliefCategory.Religion)
                    religionCount++;
            }
            return religionCount > ideo.memes.Count / 2;
        }

        public static bool IsIdeologyIdeo(Ideo ideo)
        {
            if (ideo == null || ideo.memes == null) return false;
            foreach (var meme in ideo.memes)
            {
                if (meme.category == MemeCategory.Structure)
                    return GetCategory(meme) == BeliefCategory.Ideology;
            }
            int ideologyCount = 0;
            foreach (var meme in ideo.memes)
            {
                if (GetCategory(meme) == BeliefCategory.Ideology)
                    ideologyCount++;
            }
            return ideologyCount > ideo.memes.Count / 2;
        }
    }
}
