using System.Collections.Generic;
using Verse;

namespace IdeoRework
{
    public class BeliefCategoryEntry
    {
        public string memeDefName;
        public BeliefCategory category;
    }

    public class BeliefCategoryMappingDef : Def
    {
        public List<BeliefCategoryEntry> entries = new List<BeliefCategoryEntry>();
    }
}
