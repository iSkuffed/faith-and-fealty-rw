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
        /// <summary>
        /// Build the mapping from ReligionDefLoader. Called at startup.
        /// </summary>
        public static void Initialize()
        {
            ReligionDefLoader.LoadAll();
            Log.Message($"[IdeoRework] Loaded {ReligionDefLoader.MemeCategoryLookup.Count} meme category mappings from Sorting Defs");
        }

        public static BeliefCategory GetCategory(MemeDef meme)
        {
            if (meme == null) return BeliefCategory.Ideology;
            var category = ReligionDefLoader.GetMemeCategory(meme.defName);
            return category == "Religion" ? BeliefCategory.Religion : BeliefCategory.Ideology;
        }

        public static List<MemeDef> ReligionMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => ReligionDefLoader.GetMemeCategory(m.defName) == "Religion")
                    .ToList();
            }
        }

        public static List<MemeDef> IdeologyMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => ReligionDefLoader.GetMemeCategory(m.defName) == "Ideology")
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
