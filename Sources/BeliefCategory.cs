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
        private static readonly Dictionary<string, BeliefCategory> MemeCategories = new Dictionary<string, BeliefCategory>
        {
            // --- RELIGION (supernatural/spiritual worldview) ---
            {"Structure_Animist", BeliefCategory.Religion},
            {"Structure_TheistEmbodied", BeliefCategory.Religion},
            {"Structure_TheistAbstract", BeliefCategory.Religion},
            {"Structure_Archist", BeliefCategory.Religion},
            {"Structure_OriginChristian", BeliefCategory.Religion},
            {"Structure_OriginIslamic", BeliefCategory.Religion},
            {"Structure_OriginHindu", BeliefCategory.Religion},
            {"Structure_OriginBuddhist", BeliefCategory.Religion},
            {"Proselytizer", BeliefCategory.Religion},
            {"Blindsight", BeliefCategory.Religion},
            {"HighLife", BeliefCategory.Religion},
            {"PainIsVirtue", BeliefCategory.Religion},
            {"Cannibal", BeliefCategory.Religion},
            {"Nudism", BeliefCategory.Religion},
            {"TreeConnection", BeliefCategory.Religion},
            {"Darkness", BeliefCategory.Religion},
            {"AnimalPersonhood", BeliefCategory.Religion},
            {"Tunneler", BeliefCategory.Religion},

            // --- RELIGION (added after user feedback) ---
            {"MaleSupremacy", BeliefCategory.Religion},
            {"FemaleSupremacy", BeliefCategory.Religion},
            {"HumanPrimacy", BeliefCategory.Religion},
            {"NaturePrimacy", BeliefCategory.Religion},
            {"FleshPurity", BeliefCategory.Religion},

            // --- IDEOLOGY (social/political/economic) ---
            {"Structure_Ideological", BeliefCategory.Ideology},
            {"Transhumanist", BeliefCategory.Ideology},
            {"Supremacist", BeliefCategory.Ideology},
            {"Loyalist", BeliefCategory.Ideology},
            {"Guilty", BeliefCategory.Ideology},
            {"Individualist", BeliefCategory.Ideology},
            {"Collectivist", BeliefCategory.Ideology},
            {"Rancher", BeliefCategory.Ideology},
            {"Raider", BeliefCategory.Ideology},
        };

        public static BeliefCategory GetCategory(MemeDef meme)
        {
            if (MemeCategories.TryGetValue(meme.defName, out var cat))
                return cat;
            return BeliefCategory.Religion;
        }

        public static List<MemeDef> ReligionMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => MemeCategories.TryGetValue(m.defName, out var cat) && cat == BeliefCategory.Religion)
                    .ToList();
            }
        }

        public static List<MemeDef> IdeologyMemes
        {
            get
            {
                return DefDatabase<MemeDef>.AllDefsListForReading
                    .Where(m => MemeCategories.TryGetValue(m.defName, out var cat) && cat == BeliefCategory.Ideology)
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
