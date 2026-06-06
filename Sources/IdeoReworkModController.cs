using UnityEngine;
using Verse;

namespace IdeoRework
{
    public class IdeoReworkModController : Mod
    {
        public static IdeoReworkSettings Settings { get; private set; }

        public IdeoReworkModController(ModContentPack content) : base(content)
        {
            Settings = GetSettings<IdeoReworkSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "Enable Cognitive Dissonance",
                ref Settings.enableCognitiveDissonance,
                "When enabled, conflicts between religion and ideology precepts cause mood penalties and certainty erosion."
            );
            listing.End();
            Settings.Write();
        }

        public override string SettingsCategory() => "Faith & Fealty";
    }
}
