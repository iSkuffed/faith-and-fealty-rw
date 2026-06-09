using System.Collections.Generic;
using Verse;

namespace IdeoRework
{
    public enum PresetType
    {
        Religion,
        Ideology
    }

    public class ReligionPresetDef : Def
    {
        // ── Core preset definition ──────────────────────────────────────────
        public PresetType presetType = PresetType.Religion;
        public string baseName;
        public string structureMeme;
        public List<string> normalMemes = new List<string>();
        public string iconPath;

        // ── NPC faction assignment ──────────────────────────────────────────
        public string leaderTitleMale;
        public string leaderTitleFemale;
        public List<string> factionWhitelist;
        public List<string> factionBlacklist;

        // ── Deity name generation ───────────────────────────────────────────
        // Overrides the structure meme's default deity name grammar pack.
        // Only effective if the structure meme supports deities (deityCount > 0).
        // If not set, the structure meme's default deityNameMaker is used.
        // Example: "NamerDeityChristian" for Christian-style deity names.
        public string deityNameMakerOverride;
    }
}
