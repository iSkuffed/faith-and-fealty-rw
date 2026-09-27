using System.Collections.Generic;
using RimWorld;
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

            Text.Font = GameFont.Medium;
            listing.Label("Cognitive Dissonance System");
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            var currentMode = Settings.cognitiveDissonanceMode;

            var modes = new List<CognitiveDissonanceMode>
            {
                CognitiveDissonanceMode.Legacy,
                CognitiveDissonanceMode.Experimental,
                CognitiveDissonanceMode.Disabled
            };

            foreach (var mode in modes)
            {
                bool selected = currentMode == mode;

                listing.Gap(4f);
                var rowRect = listing.GetRect(30f);

                var radioRect = new Rect(rowRect.x, rowRect.y, 24f, 24f);
                var labelRect = new Rect(rowRect.x + 28f, rowRect.y, rowRect.width - 28f, rowRect.height);

                string label = GetModeLabel(mode);
                string tooltip = GetModeTooltip(mode);

                if (Widgets.RadioButton(radioRect.position, selected))
                {
                    if (!selected)
                    {
                        OnModeChanged(currentMode, mode);
                        currentMode = mode;
                        Settings.cognitiveDissonanceMode = mode;
                    }
                }

                Widgets.Label(labelRect, label);
                TooltipHandler.TipRegion(rowRect, tooltip);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (!selected)
                    {
                        OnModeChanged(currentMode, mode);
                        currentMode = mode;
                        Settings.cognitiveDissonanceMode = mode;
                    }
                }
            }

            listing.Gap(12f);
            listing.Label("Changes take effect immediately. Active modifiers from the previous mode will be removed.");

            listing.End();
        }

        private string GetModeLabel(CognitiveDissonanceMode mode) => mode switch
        {
            CognitiveDissonanceMode.Legacy => "Legacy Cognitive Dissonance",
            CognitiveDissonanceMode.Experimental => "Experimental Cognitive Dissonance",
            CognitiveDissonanceMode.Disabled => "Disabled",
            _ => "Unknown"
        };

        private string GetModeTooltip(CognitiveDissonanceMode mode) => mode switch
        {
            CognitiveDissonanceMode.Legacy => "Original system: persistent 3-stage mood thought based on cumulative precept conflicts between religion and ideology.",
            CognitiveDissonanceMode.Experimental => "New system: event-triggered individual mood thoughts with certainty erosion per category. Tracks slavery, cannibalism, organ use, diversity of thought, physical love, and execution.",
            CognitiveDissonanceMode.Disabled => "Cognitive dissonance mechanics are turned off entirely.",
            _ => ""
        };

        private void OnModeChanged(CognitiveDissonanceMode oldMode, CognitiveDissonanceMode newMode)
        {
            if (oldMode == CognitiveDissonanceMode.Legacy)
            {
                CognitiveDissonanceTracker.RemoveAllModifiers();
            }
            else if (oldMode == CognitiveDissonanceMode.Experimental)
            {
                CognitiveDissonanceExperimental.RemoveAllModifiers();
            }

            Log.Message($"[IdeoRework] Cognitive Dissonance mode changed: {oldMode} \u2192 {newMode}");
        }

        public override string SettingsCategory() => "Faith & Fealty";
    }
}
