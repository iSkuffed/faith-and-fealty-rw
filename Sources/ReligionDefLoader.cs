using System.Collections.Generic;
using Verse;

namespace IdeoRework
{
    public static class ReligionDefLoader
    {
        public static Dictionary<string, string> MemeCategoryLookup { get; private set; } = new Dictionary<string, string>();

        public static void LoadAll()
        {
            MemeCategoryLookup.Clear();
            foreach (var def in DefDatabase<ReligionCategoryMemeDef>.AllDefsListForReading)
            {
                if (!def.targetDefName.NullOrEmpty())
                    MemeCategoryLookup[def.targetDefName] = def.category ?? "Ideology";
            }

            Log.Message($"[IdeoRework] Loaded {MemeCategoryLookup.Count} meme category mappings from Sorting Defs");
        }

        public static string GetMemeCategory(string defName)
        {
            if (MemeCategoryLookup.TryGetValue(defName, out var category))
                return category;
            return "Ideology";
        }
    }
}
