using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public class Dialog_TwoStepIdeoWizard : Window
    {
        private enum Step
        {
            ReligionMemes,
            ReligionCustomize,
            IdeologyMemes,
            IdeologyCustomize
        }

        private Step currentStep = Step.ReligionMemes;
        private readonly Page_ChooseIdeoPreset parentPage;
        private readonly List<MemeDef> selectedReligionMemes = new List<MemeDef>();
        private readonly List<MemeDef> selectedIdeologyMemes = new List<MemeDef>();
        private Ideo religionIdeo;
        private Ideo ideologyIdeo;
        private Vector2 scrollPosition;
        private float scrollViewHeight;
        private bool religionPresetsExpanded;
        private bool ideologyPresetsExpanded;
        private ReligionPresetDef selectedReligionPreset;
        private ReligionPresetDef selectedIdeologyPreset;

        private const float TitleHeight = 50f;
        private const float SubtitleHeight = 25f;
        private const float BottomBarHeight = 55f;
        private const float MemeBoxWidth = 100f;
        private const float MemeBoxHeight = 130f;
        private const float MemeChipWidth = 80f;
        private const float MemeChipHeight = 105f;
        private const float GapX = 10f;
        private const float GapY = 10f;
        private const float SectionHeaderHeight = 28f;
        private const float SectionDescHeight = 20f;

        public override Vector2 InitialSize => new Vector2(960f, 720f);

        public Dialog_TwoStepIdeoWizard(Page_ChooseIdeoPreset parent)
        {
            // Purge old wizard-created ideology/religion from previous session
            SessionRegistry.Purge();

            parentPage = parent;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            doCloseButton = false;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            string title = currentStep switch
            {
                Step.ReligionMemes => "Design Your Religion  —  Step 1 of 4",
                Step.ReligionCustomize => "Customize Your Religion  —  Step 2 of 4",
                Step.IdeologyMemes => "Design Your Ideology  —  Step 3 of 4",
                Step.IdeologyCustomize => "Customize Your Ideology  —  Step 4 of 4",
                _ => ""
            };
            Widgets.Label(new Rect(0f, 0f, inRect.width, TitleHeight), title);
            float curY = TitleHeight;

            if (currentStep == Step.ReligionMemes || currentStep == Step.IdeologyMemes)
                DrawMemeSelectionStep(inRect, ref curY);
            else
                DrawCustomizationStep(inRect, ref curY);

            DrawBottomBar(inRect);
        }

        private void DrawMemeSelectionStep(Rect inRect, ref float curY)
        {
            bool isReligion = currentStep == Step.ReligionMemes;

            Text.Font = GameFont.Small;
            string subtitle = isReligion
                ? "Choose your supernatural beliefs, spiritual foundation, and religious practices."
                : "Choose your social, political, and moral values.";
            Widgets.Label(new Rect(0f, curY, inRect.width, SubtitleHeight), subtitle);
            curY += SubtitleHeight + 5f;

            var available = isReligion
                ? BeliefCategoryLookup.ReligionMemes
                : BeliefCategoryLookup.IdeologyMemes;
            var selected = isReligion ? selectedReligionMemes : selectedIdeologyMemes;

            Rect outRect = new Rect(0f, curY, inRect.width, inRect.height - curY - BottomBarHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float innerY = 0f;

            // Draw collapsible preset section
            DrawPresetSection(viewRect.width, ref innerY, isReligion, selected);

            DrawMemeGrid(viewRect.width, ref innerY, available, selected);
            scrollViewHeight = innerY;
            Widgets.EndScrollView();
        }

        private void DrawPresetSection(float width, ref float curY, bool isReligion, List<MemeDef> selected)
        {
            var presets = PresetReligions.GetPlayerPresets(isReligion ? PresetType.Religion : PresetType.Ideology);
            if (presets == null || presets.Count == 0)
                return;

            ref bool expanded = ref (isReligion ? ref religionPresetsExpanded : ref ideologyPresetsExpanded);
            var selectedPreset = isReligion ? selectedReligionPreset : selectedIdeologyPreset;

            // Collapsible header
            Rect headerRect = new Rect(0f, curY, width, 30f);
            string label = $"Presets ({presets.Count} available)";
            if (selectedPreset != null)
                label += $"  —  Selected: {selectedPreset.label}";

            bool didExpand = expanded;
            Widgets.DrawBoxSolid(headerRect, new Color(0.15f, 0.15f, 0.15f));
            Rect arrowRect = new Rect(headerRect.x + 4f, headerRect.y + 4f, 22f, 22f);
            string arrow = expanded ? "▼" : "▶";
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(arrowRect, arrow);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(headerRect.x + 28f, headerRect.y + 4f, width - 28f, 22f), label);

            if (Widgets.ButtonInvisible(headerRect))
                expanded = !expanded;

            curY += 34f;

            // Expanded content: grid of preset cards
            if (expanded)
            {
                float cardW = 140f;
                float cardH = 90f;
                float cardGap = 8f;
                float cx = 0f;

                foreach (var preset in presets)
                {
                    Rect cardRect = new Rect(cx, curY, cardW, cardH);
                    bool isThisSelected = selectedPreset == preset;

                    // Card background
                    Widgets.DrawBoxSolid(cardRect, isThisSelected
                        ? new Color(0.2f, 0.35f, 0.2f)
                        : new Color(0.12f, 0.12f, 0.12f));
                    GUI.color = isThisSelected
                        ? new Color(0.4f, 0.8f, 0.4f)
                        : new Color(0.4f, 0.4f, 0.4f);
                    Widgets.DrawBox(cardRect, 1, null);
                    GUI.color = Color.white;

                    // Structure meme icon (top-left)
                    var structureMeme = DefDatabase<MemeDef>.GetNamedSilentFail(preset.structureMeme);
                    if (structureMeme != null)
                    {
                        Rect iconRect = new Rect(cardRect.x + 6f, cardRect.y + 6f, 28f, 28f);
                        GUI.DrawTexture(iconRect, structureMeme.Icon);
                    }

                    // Label
                    Text.Font = GameFont.Small;
                    Widgets.Label(new Rect(cardRect.x + 38f, cardRect.y + 6f, cardW - 44f, 20f), preset.label);

                    // Description (truncated)
                    if (!preset.description.NullOrEmpty())
                    {
                        Text.Font = GameFont.Tiny;
                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                        string desc = preset.description.Length > 80
                            ? preset.description.Substring(0, 77) + "..."
                            : preset.description;
                        Widgets.Label(new Rect(cardRect.x + 8f, cardRect.y + 36f, cardW - 16f, 48f), desc);
                        GUI.color = Color.white;
                    }

                    Text.Font = GameFont.Small;

                    if (Widgets.ButtonInvisible(cardRect))
                    {
                        if (isThisSelected)
                        {
                            // Deselect: clear the preset and meme list
                            selected.Clear();
                            if (isReligion) selectedReligionPreset = null;
                            else selectedIdeologyPreset = null;
                        }
                        else
                        {
                            // Select: populate meme list from preset
                            selected.Clear();
                            if (!preset.structureMeme.NullOrEmpty())
                            {
                                var structureDef = DefDatabase<MemeDef>.GetNamedSilentFail(preset.structureMeme);
                                if (structureDef != null) selected.Add(structureDef);
                            }
                            if (preset.normalMemes != null)
                            {
                                foreach (var memeName in preset.normalMemes)
                                {
                                    var memeDef = DefDatabase<MemeDef>.GetNamedSilentFail(memeName);
                                    if (memeDef != null) selected.Add(memeDef);
                                }
                            }
                            if (isReligion) selectedReligionPreset = preset;
                            else selectedIdeologyPreset = preset;
                        }
                    }

                    cx += cardW + cardGap;
                    if (cx + cardW > width)
                    {
                        cx = 0f;
                        curY += cardH + cardGap;
                    }
                }

                if (cx + cardW > width)
                    curY += cardH + cardGap;

                curY += cardH + cardGap + 4f;
            }
        }

        private void DrawMemeGrid(float width, ref float curY, List<MemeDef> available, List<MemeDef> selected)
        {
            var structureMemes = available.Where(m => m.category == MemeCategory.Structure).ToList();
            var normalMemes = available.Where(m => m.category == MemeCategory.Normal)
                .OrderBy(m => m.renderOrder).ToList();

            if (structureMemes.Any())
            {
                DrawSectionHeader("Worldview Foundation", "Select one spiritual or philosophical foundation.", width, ref curY);

                var structureSelected = selected.Where(m => m.category == MemeCategory.Structure).FirstOrDefault();
                float sx = 0f;
                float rowH = 0f;
                foreach (var meme in structureMemes)
                {
                    Rect box = new Rect(sx, curY, MemeBoxWidth, MemeBoxHeight);
                    bool isSelected = structureSelected == meme;

                    IdeoUIUtility.DoMeme(box, meme);
                    if (!isSelected)
                        Widgets.DrawBox(box, 1, null);
                    if (isSelected)
                        Widgets.DrawHighlightSelected(box);

                    if (Widgets.ButtonInvisible(box))
                    {
                        if (structureSelected == meme)
                            selected.Remove(meme);
                        else
                        {
                            selected.RemoveAll(m => m.category == MemeCategory.Structure);
                            selected.Add(meme);
                        }
                    }

                    sx += MemeBoxWidth + GapX;
                    rowH = MemeBoxHeight;
                    if (sx + MemeBoxWidth > width)
                    {
                        sx = 0f;
                        curY += rowH + GapY;
                        rowH = 0f;
                    }
                }
                if (rowH > 0f)
                    curY += rowH + GapY;
                curY += 6f;
            }

            if (normalMemes.Any())
            {
                DrawSectionHeader("Additional Beliefs", "Select all that apply to your belief system.", width, ref curY);

                float nx = 0f;
                float rowH = 0f;
                foreach (var meme in normalMemes)
                {
                    Rect box = new Rect(nx, curY, MemeBoxWidth, MemeBoxHeight);
                    bool isSelected = selected.Contains(meme);

                    IdeoUIUtility.DoMeme(box, meme);
                    if (!isSelected)
                        Widgets.DrawBox(box, 1, null);
                    if (isSelected)
                        Widgets.DrawHighlightSelected(box);

                    if (Widgets.ButtonInvisible(box))
                    {
                        if (isSelected)
                            selected.Remove(meme);
                        else
                        {
                            if (ConflictsWithSelected(meme, selected))
                            {
                                Messages.Message(
                                    "This meme conflicts with one you've already selected.",
                                    MessageTypeDefOf.RejectInput, false);
                                return;
                            }
                            selected.Add(meme);
                        }
                    }

                    nx += MemeBoxWidth + GapX;
                    rowH = MemeBoxHeight;

                    if (nx + MemeBoxWidth > width)
                    {
                        nx = 0f;
                        curY += rowH + GapY;
                        rowH = 0f;
                    }
                }
                if (rowH > 0f)
                    curY += rowH + GapY;
            }
        }

        private static readonly List<HashSet<string>> ExclusionGroups = new()
        {
            new() {"MaleSupremacy", "FemaleSupremacy"},
            new() {"HumanPrimacy", "NaturePrimacy"},
            new() {"FleshPurity", "Transhumanist"},
            new() {"FleshPurity", "HighLife"},
            new() {"AnimalPersonhood", "Rancher"},
        };

        private static bool ConflictsWithSelected(MemeDef meme, List<MemeDef> selected)
        {
            foreach (var group in ExclusionGroups)
            {
                if (!group.Contains(meme.defName))
                    continue;
                foreach (var sel in selected)
                {
                    if (group.Contains(sel.defName))
                        return true;
                }
            }
            return false;
        }

        private void DrawCustomizationStep(Rect inRect, ref float curY)
        {
            bool isReligion = currentStep == Step.ReligionCustomize;
            Ideo currentIdeo = isReligion ? religionIdeo : ideologyIdeo;
            List<MemeDef> selectedMemes = isReligion ? selectedReligionMemes : selectedIdeologyMemes;

            if (currentIdeo == null)
                return;

            Text.Font = GameFont.Small;
            string subtitle = isReligion
                ? "Customize your religion's precepts and give it a name."
                : "Customize your ideology's precepts and give it a name.";
            Widgets.Label(new Rect(0f, curY, inRect.width, SubtitleHeight), subtitle);
            curY += SubtitleHeight + 5f;

            Rect outRect = new Rect(0f, curY, inRect.width, inRect.height - curY - BottomBarHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float innerY = 0f;
            float width = viewRect.width;

            DrawSectionHeader("Selected Beliefs",
                "Click a meme to return to the previous step and change it.",
                width, ref innerY);

            float cx = 0f;
            float chipRowH = 0f;
            foreach (var meme in selectedMemes)
            {
                Rect chipBox = new Rect(cx, innerY, MemeChipWidth, MemeChipHeight);
                IdeoUIUtility.DoMeme(chipBox, meme);
                Widgets.DrawBox(chipBox, 1, null);

                if (Widgets.ButtonInvisible(chipBox))
                {
                    currentStep = isReligion ? Step.ReligionMemes : Step.IdeologyMemes;
                    scrollPosition = Vector2.zero;
                    Widgets.EndScrollView();
                    return;
                }

                cx += MemeChipWidth + GapX;
                chipRowH = MemeChipHeight;
                if (cx + MemeChipWidth > width)
                {
                    cx = 0f;
                    innerY += chipRowH + GapY;
                    chipRowH = 0f;
                }
            }
            if (chipRowH > 0f)
                innerY += chipRowH + GapY;
            innerY += 10f;

            DrawSectionHeader("Symbol",
                "Choose your " + (isReligion ? "religion" : "ideology") + "'s name, icon, and colors. Click the icon to customize.",
                width, ref innerY);

            Rect iconRect = new Rect(0f, innerY, 60f, 60f);
            GUI.color = currentIdeo.Color;
            GUI.DrawTexture(iconRect, currentIdeo.Icon);
            GUI.color = Color.white;
            Widgets.DrawBox(iconRect, 1, null);

            if (Widgets.ButtonInvisible(iconRect))
            {
                Find.WindowStack.Add(new Dialog_ChooseIdeoSymbols(currentIdeo));
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(70f, innerY, width - 70f, 28f), currentIdeo.name ?? "");
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(new Rect(70f, innerY + 28f, width - 70f, 18f),
                (currentIdeo.adjective ?? "") + " — " + (currentIdeo.memberName ?? ""));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            innerY += 70f;

            // Culture picker (Ideology only — religion doesn't have culture)
            if (!isReligion)
            {
                DrawSectionHeader("Culture",
                    "Choose the cultural origin of your ideology.",
                    width, ref innerY);

                Rect cultureRect = new Rect(0f, innerY, 35f, 35f);
                if (currentIdeo.culture != null)
                {
                    GUI.color = currentIdeo.culture.iconColor;
                    GUI.DrawTexture(cultureRect, currentIdeo.culture.Icon);
                    GUI.color = Color.white;
                }

                if (Widgets.ButtonInvisible(cultureRect))
                {
                    List<FloatMenuOption> cultureOptions = new List<FloatMenuOption>();
                    foreach (CultureDef culture in DefDatabase<CultureDef>.AllDefs.OrderBy(c => c.label))
                    {
                        CultureDef localCulture = culture;
                        cultureOptions.Add(new FloatMenuOption(localCulture.LabelCap, delegate
                        {
                            if (currentIdeo.culture != localCulture)
                            {
                                currentIdeo.culture = localCulture;
                                currentIdeo.foundation.RandomizeStyles();
                                currentIdeo.style.RecalculateAvailableStyleItems();
                                if (currentIdeo.foundation is IdeoFoundation_Deity deityFoundation)
                                {
                                    deityFoundation.GenerateDeities();
                                }
                                currentIdeo.RegenerateDescription(force: true);
                            }
                        }, localCulture.Icon, localCulture.iconColor));
                    }
                    Find.WindowStack.Add(new FloatMenu(cultureOptions, "ChooseCulture".Translate()));
                }

                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(45f, innerY, width - 45f, 35f),
                    currentIdeo.culture?.LabelCap ?? "No culture selected");
                innerY += 45f;
            }

            // Style selectors
            DrawSectionHeader("Styles",
                "Choose style categories for your " + (isReligion ? "religion" : "ideology") + ".",
                width, ref innerY);

            float styleX = 0f;
            for (int i = 0; i < 3; i++)
            {
                Rect styleRect = new Rect(styleX, innerY, 28f, 28f);
                int index = i;

                if (i < currentIdeo.thingStyleCategories.Count)
                {
                    GUI.DrawTexture(styleRect.ContractedBy(4f), currentIdeo.thingStyleCategories[i].category.Icon);
                    if (Mouse.IsOver(styleRect))
                    {
                        Widgets.DrawHighlight(styleRect);
                        TooltipHandler.TipRegion(styleRect, currentIdeo.thingStyleCategories[i].category.LabelCap);
                    }
                    if (Widgets.ButtonInvisible(styleRect))
                    {
                        List<FloatMenuOption> styleOptions = new List<FloatMenuOption>();
                        styleOptions.Add(new FloatMenuOption("Remove".Translate(), delegate
                        {
                            currentIdeo.thingStyleCategories.RemoveAt(index);
                            currentIdeo.SortStyleCategories();
                            currentIdeo.style.ResetStylesForThingDef();
                        }));
                        foreach (StyleCategoryDef s in DefDatabase<StyleCategoryDef>.AllDefs
                            .Where(x => !x.fixedIdeoOnly && !currentIdeo.thingStyleCategories.Any(y => y.category == x)))
                        {
                            StyleCategoryDef localStyle = s;
                            styleOptions.Add(new FloatMenuOption(localStyle.LabelCap, delegate
                            {
                                currentIdeo.thingStyleCategories.RemoveAt(index);
                                currentIdeo.thingStyleCategories.Insert(index, new ThingStyleCategoryWithPriority(localStyle, 3 - index));
                                currentIdeo.SortStyleCategories();
                                currentIdeo.style.ResetStylesForThingDef();
                            }, localStyle.Icon, Color.white));
                        }
                        Find.WindowStack.Add(new FloatMenu(styleOptions));
                    }
                }
                else
                {
                    GUI.DrawTexture(styleRect.ContractedBy(4f), ContentFinder<Texture2D>.Get("UI/Buttons/Plus"));
                    if (Mouse.IsOver(styleRect))
                    {
                        Widgets.DrawHighlight(styleRect);
                        TooltipHandler.TipRegion(styleRect, "AddStyleCategory".Translate());
                    }
                    if (Widgets.ButtonInvisible(styleRect))
                    {
                        List<FloatMenuOption> styleOptions = new List<FloatMenuOption>();
                        foreach (StyleCategoryDef s in DefDatabase<StyleCategoryDef>.AllDefs
                            .Where(x => !x.fixedIdeoOnly && !currentIdeo.thingStyleCategories.Any(y => y.category == x)))
                        {
                            StyleCategoryDef localStyle = s;
                            styleOptions.Add(new FloatMenuOption(localStyle.LabelCap, delegate
                            {
                                currentIdeo.thingStyleCategories.Add(new ThingStyleCategoryWithPriority(localStyle, 3 - index));
                                currentIdeo.SortStyleCategories();
                                currentIdeo.style.ResetStylesForThingDef();
                            }, localStyle.Icon, Color.white));
                        }
                        if (styleOptions.Any())
                            Find.WindowStack.Add(new FloatMenu(styleOptions));
                    }
                }
                styleX += 28f;
            }
            innerY += 38f;

            // Deities section — drawn by IdeoFoundation_Deity.DoInfo
            // Only visible if structure meme has deityCount > 0
            try
            {
                if (currentIdeo.foundation != null)
                    currentIdeo.foundation.DoInfo(ref innerY, width, IdeoEditMode.GameStart);
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] DoInfo (deities) error: " + ex);
            }

            // Narrative section
            DrawSectionHeader("Narrative",
                "Write the core narrative of your " + (isReligion ? "religion" : "ideology") + ".",
                width, ref innerY);

            float narrativeWidth = width - 80f;
            string desc = currentIdeo.description ?? "";
            int narrativeHeight = (int)Mathf.Max(70f, Text.CalcHeight(desc, narrativeWidth));
            Rect narrativeRect = new Rect(40f, innerY, narrativeWidth, narrativeHeight);
            Widgets.Label(narrativeRect, desc);

            if (Widgets.ButtonInvisible(narrativeRect))
            {
                Find.WindowStack.Add(new Dialog_EditIdeoDescription(currentIdeo));
            }

            innerY += narrativeHeight + 17f;

            DrawSectionHeader("Precepts",
                "Add or remove precepts for your " + (isReligion ? "religion" : "ideology") + ".",
                width, ref innerY);

            try
            {
                IdeoUIUtility.DoPrecepts(ref innerY, width, currentIdeo, IdeoEditMode.GameStart);
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] DoPrecepts error: " + ex);
            }

            scrollViewHeight = innerY;
            Widgets.EndScrollView();
        }

        private void DrawSectionHeader(string title, string desc, float width, ref float curY)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, curY, width, SectionHeaderHeight), title);
            curY += SectionHeaderHeight;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(new Rect(0f, curY, width, SectionDescHeight), desc);
            GUI.color = Color.white;
            curY += SectionDescHeight + 4f;

            Text.Font = GameFont.Small;
        }

        private void DrawBottomBar(Rect inRect)
        {
            float y = inRect.height - BottomBarHeight;
            float btnH = 40f;
            float backBtnW = 130f;

            if (currentStep > Step.ReligionMemes)
            {
                string backLabel = currentStep switch
                {
                    Step.ReligionCustomize => "< Back to Memes",
                    Step.IdeologyMemes => "< Back to Religion",
                    Step.IdeologyCustomize => "< Back to Memes",
                    _ => "< Back"
                };
                if (Widgets.ButtonText(new Rect(10f, y + 7f, backBtnW, btnH), backLabel))
                {
                    switch (currentStep)
                    {
                        case Step.ReligionCustomize:
                            currentStep = Step.ReligionMemes;
                            break;
                        case Step.IdeologyMemes:
                            currentStep = Step.ReligionCustomize;
                            scrollPosition = Vector2.zero;
                            break;
                        case Step.IdeologyCustomize:
                            currentStep = Step.IdeologyMemes;
                            break;
                    }
                    scrollPosition = Vector2.zero;
                }
            }

            string nextLabel = currentStep switch
            {
                Step.ReligionMemes => "Next: Customize >",
                Step.ReligionCustomize => "Next: Ideology >",
                Step.IdeologyMemes => "Next: Customize >",
                Step.IdeologyCustomize => "Finish",
                _ => ""
            };
            float btnW = nextLabel == "Finish" ? 130f : 150f;
            float nextX = inRect.width - btnW - 10f;
            if (Widgets.ButtonText(new Rect(nextX, y + 7f, btnW, btnH), nextLabel))
            {
                switch (currentStep)
                {
                    case Step.ReligionMemes:
                        if (!selectedReligionMemes.Any(m => m.category == MemeCategory.Structure))
                        {
                            Messages.Message("You must select a worldview foundation before proceeding.",
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        EnterReligionCustomize();
                        break;
                    case Step.ReligionCustomize:
                        if (string.IsNullOrWhiteSpace(religionIdeo?.name))
                        {
                            Messages.Message("You must name your religion before proceeding.",
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        currentStep = Step.IdeologyMemes;
                        scrollPosition = Vector2.zero;
                        break;
                    case Step.IdeologyMemes:
                        if (!selectedIdeologyMemes.Any(m => m.category == MemeCategory.Structure))
                        {
                            Messages.Message("You must select a worldview foundation before proceeding.",
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        EnterIdeologyCustomize();
                        break;
                    case Step.IdeologyCustomize:
                        if (string.IsNullOrWhiteSpace(ideologyIdeo?.name))
                        {
                            Messages.Message("You must name your ideology before proceeding.",
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        OnWizardComplete();
                        break;
                }
            }

            float cancelX = nextX - 140f;
            if (Widgets.ButtonText(new Rect(cancelX, y + 7f, 130f, btnH), "Cancel"))
                Close();
        }

        private void EnterReligionCustomize()
        {
            religionIdeo = new Ideo();
            SetupIdeo(religionIdeo, selectedReligionMemes);
            currentStep = Step.ReligionCustomize;
            scrollPosition = Vector2.zero;
        }

        private void EnterIdeologyCustomize()
        {
            ideologyIdeo = new Ideo();
            SetupIdeo(ideologyIdeo, selectedIdeologyMemes);
            currentStep = Step.IdeologyCustomize;
            scrollPosition = Vector2.zero;
        }

        private static void SetupIdeo(Ideo ideo, List<MemeDef> selectedMemes)
        {
            // Assign a unique ID (new Ideo() doesn't do this)
            ideo.id = Find.UniqueIDsManager.GetNextIdeoID();

            var preceptsField = AccessTools.Field(typeof(Ideo), "precepts");
            if (preceptsField != null && preceptsField.GetValue(ideo) == null)
                preceptsField.SetValue(ideo, new List<Precept>());

            var factionField = AccessTools.Field(typeof(Ideo), "factionIdeoWeaponPairs");
            if (factionField != null && factionField.GetValue(ideo) == null)
            {
                var elemType = factionField.FieldType.GetGenericArguments()[0];
                factionField.SetValue(ideo, Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType)));
            }

            var thingStylesField = AccessTools.Field(typeof(Ideo), "thingStyleCategories");
            if (thingStylesField != null && thingStylesField.GetValue(ideo) == null)
                thingStylesField.SetValue(ideo, new List<ThingStyleCategoryWithPriority>());

            var usedSymbolPacksField = AccessTools.Field(typeof(Ideo), "usedSymbolPacks");
            if (usedSymbolPacksField != null && usedSymbolPacksField.GetValue(ideo) == null)
                usedSymbolPacksField.SetValue(ideo, new List<string>());

            var usedSymbolsField = AccessTools.Field(typeof(Ideo), "usedSymbols");
            if (usedSymbolsField != null && usedSymbolsField.GetValue(ideo) == null)
                usedSymbolsField.SetValue(ideo, new List<string>());

            ideo.memes = new List<MemeDef>(selectedMemes);

            if (ideo.foundation == null)
            {
                IdeoFoundationDef foundationDef = DefDatabase<IdeoFoundationDef>.GetNamedSilentFail("Deity")
                    ?? DefDatabase<IdeoFoundationDef>.AllDefsListForReading.FirstOrDefault();

                if (foundationDef != null)
                {
                    ideo.foundation = IdeoGenerator.MakeFoundation(foundationDef);
                    ideo.foundation.ideo = ideo;
                    ideo.foundation.def = foundationDef;

                    var parms = new IdeoGenerationParms(IdeoUIUtility.FactionForRandomization(ideo));

                    try { ideo.foundation.RandomizeCulture(parms); } catch (Exception ex) { Log.Warning("[IdeoRework] RandomizeCulture: " + ex.Message); }
                    try { ideo.foundation.RandomizePlace(); } catch (Exception ex) { Log.Warning("[IdeoRework] RandomizePlace: " + ex.Message); }
                    try { ideo.foundation.GenerateTextSymbols(); } catch { }
                    try { ideo.foundation.GenerateLeaderTitle(); } catch { }
                    try { ideo.foundation.RandomizePrecepts(init: true, parms); } catch (Exception ex) { Log.Warning("[IdeoRework] RandomizePrecepts FAILED: " + ex); }
                    try { ideo.foundation.RandomizeIcon(); } catch { }
                    if (ideo.foundation is IdeoFoundation_Deity deityFoundation)
                    {
                        try { deityFoundation.GenerateDeities(); } catch { }
                    }
                    try { ideo.RegenerateDescription(true); } catch { }
                    try { ideo.foundation.RandomizeStyles(); } catch { }
                }
            }

            var styleField = AccessTools.Field(typeof(Ideo), "style");
            if (styleField?.GetValue(ideo) == null)
            {
                var tracker = new IdeoStyleTracker(ideo);
                styleField.SetValue(ideo, tracker);
            }

            try { ideo.SortMemesInDisplayOrder(); } catch { }
            try { ideo.RecachePossibleRoles(); } catch { }
            try { ideo.RecacheNeeds(); } catch { }
            try { ideo.RegenerateAllPreceptNames(); } catch { }

            try
            {
                var tracker = styleField?.GetValue(ideo) as IdeoStyleTracker;
                tracker?.RecalculateAvailableStyleItems();
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] Style recalculation: " + ex.Message);
            }
        }

        private void OnWizardComplete()
        {
            try
            {
                // Validate both ideos exist
                if (religionIdeo == null)
                {
                    Messages.Message("Error: Religion ideo is missing. Please restart the wizard.",
                        MessageTypeDefOf.RejectInput, false);
                    return;
                }
                if (ideologyIdeo == null)
                {
                    Messages.Message("Error: Ideology ideo is missing. Please restart the wizard.",
                        MessageTypeDefOf.RejectInput, false);
                    return;
                }

                // Store in session registry
                SessionRegistry.CurrentIdeology = ideologyIdeo;
                SessionRegistry.CurrentReligion = religionIdeo;
                SessionRegistry.IsSessionActive = true;

                // Store player's custom religion for pawn assignment
                PresetReligions.PlayerReligionIdeo = religionIdeo;
                PresetReligions.CreatedReligionIdeos.Add(religionIdeo);
                IdeoReworkGameComponent.SaveReligionIdeoIds();

                // Set preset fields for vanilla UI
                var presetField = AccessTools.Field(typeof(Page_ChooseIdeoPreset), "presetSelection");
                if (presetField == null)
                {
                    Log.Error("[IdeoRework] Could not find presetSelection field");
                    Messages.Message("Error completing wizard. See log.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                presetField.SetValue(parentPage, Enum.Parse(presetField.FieldType, "CustomFixed"));

                var classicField = AccessTools.Field(typeof(Page_ChooseIdeoPreset), "classicIdeo");
                if (classicField == null)
                {
                    Log.Error("[IdeoRework] Could not find classicIdeo field");
                    Messages.Message("Error completing wizard. See log.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                classicField.SetValue(parentPage, ideologyIdeo);

                // Set primary ideo BEFORE generating pawns (so pawns get correct ideology)
                Faction.OfPlayer.ideos.SetPrimary(ideologyIdeo);
                ideologyIdeo.initialPlayerIdeo = true;

                // Regenerate starting pawns (use PostIdeoChosen which sets startingPawnCount + generates pawns + adds optional pawns)
                try
                {
                    var scenario = Find.Scenario;
                    if (scenario != null)
                    {
                        var partsList = Traverse.Create(scenario).Field("parts").GetValue() as System.Collections.IList;
                        if (partsList != null)
                        {
                            foreach (var part in partsList)
                            {
                                var partType = part.GetType();
                                if (partType.Name == "ScenPart_ConfigPage_ConfigureStartingPawns")
                                {
                                    var postIdeoMethod = partType.GetMethod("PostIdeoChosen",
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    postIdeoMethod?.Invoke(part, null);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[IdeoRework] Could not regenerate starting pawns: " + ex.Message);
                }

                // Hard override: assign ideology + religion to ALL player pawns
                HardOverride.AssignToAllPlayerPawns(ideologyIdeo, religionIdeo);

                if (parentPage.next == null)
                {
                    Log.Error("[IdeoRework] parentPage.next is null");
                    Messages.Message("Error: Cannot navigate. See log.", MessageTypeDefOf.RejectInput, false);
                    return;
                }

                parentPage.next.prev = parentPage;
                Close();
                Find.WindowStack.Add(parentPage.next);
            }
            catch (Exception ex)
            {
                Log.Error($"[IdeoRework] OnWizardComplete failed: {ex}");
                Messages.Message("Error completing wizard. Check the log for details.",
                    MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
