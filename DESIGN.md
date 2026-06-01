# Ideology & Religion Rework — Design Document

## Overview

> **Version:** 1.3.0 | **Created:** 2026-05-29 | **Last updated:** 2026-05-30 05:15 UTC
> **Last updated by:** opencode/deepseek-v4-flash-free

RimWorld mod that replaces the classic ideology creation flow (`Page_ConfigureIdeo`) with a 4-step wizard that separates belief systems into two conceptual segments: **Religion** (supernatural/spiritual beliefs) and **Ideology** (social/political/moral values). 

### Phase 1 (COMPLETED!) — UI & Debugging

WOOP WOOP!

### PHASE 2 (CURRENT) - SEPERATE RELIGION AND IDEOLOGY INTO SEPERATE BELIEFS TO BE GENERATED FOR EACH PAWN! ###

Religion and Ideology will now become 2 distinct belief systems held by every pawn in the game, each with their own memes, precepts, and gameplay effects. 

Target: RimWorld 1.6.4633 rev1273, netstandard2.1  
Dependency: `brrainz.harmony` (Steam 2009463077)  
Package ID: `skuffed.ideorework`

## Architecture

```
IdeoReworkMod (static ctor, Harmony init)
  └─ Patch_PageChooseIdeoPreset_DoCustomize (Harmony prefix)
       └─ intercepts Page_ChooseIdeoPreset.DoCustomize(fluid=false)
       └─ opens Dialog_TwoStepIdeoWizard instead of Page_ConfigureIdeo
       └─ returns false (= skip original method)
  └─ Dialog_TwoStepIdeoWizard (Window)
       └─ 4-step wizard with navigation
  └─ BeliefCategoryLookup (static dict)
       └─ maps MemeDef.defName → Religion/Ideology
  └─ (no other patches exist)
```

## Project Structure

```
ideorework/
├── Sources/
│   ├── IdeoReworkMod.cs          # Entry point, static ctor Harmony patching
│   ├── Patch_PageChooseIdeoPreset.cs  # Harmony prefix on DoCustomize
│   ├── Dialog_TwoStepIdeoWizard.cs    # Core 4-step wizard dialog
│   ├── BeliefCategory.cs              # Meme→Religion/Ideology classification
│   ├── IdeoRework.csproj              # .NET project file
│   ├── build.sh                       # Build script
│   └── obj/                           # Build artifacts
├── About/
│   └── About.xml                      # Mod metadata
├── LoadFolders.xml                     # Folder routing for 1.6
├── 1.6/
│   └── Assemblies/
│       └── IdeoRework.dll             # Built assembly
└── DESIGN.md                          # This file
```

## 4-Step Wizard Flow

```
[ReligionMemes] → [ReligionCustomize] → [IdeologyMemes] → [IdeologyCustomize] → [Finish]
   Step 1              Step 2               Step 3              Step 4
```

### Step 1: ReligionMemes
- Player selects memes from the Religion category (supernatural/spiritual)
- Structure memes (worldview foundations) are single-select (radio button behavior)
- Normal memes are multi-select, checked against exclusion groups
- Exclusion groups are hardcoded in `Dialog_TwoStepIdeoWizard.ExclusionGroups`
- Uses `IdeoUIUtility.DoMeme()` for meme card rendering with `Widgets.DrawBox` for card frames

### Step 2: ReligionCustomize
- Creates a temporary `Ideo` from selected Religion memes
- Calls `IdeoUIUtility.DoPrecepts()` for precept editing with `(IdeoEditMode)2`
- Text field for naming the religion
- Clicking a meme chip navigates back to Step 1

### Step 3: IdeologyMemes
- Same layout as Step 1 but filters to Ideology category memes
- Same multi-select + exclusion group behavior

### Step 4: IdeologyCustomize
- Same layout as Step 2 but for ideology memes
- Separate name field for ideology name (string currently stored but may not be used)

### Finish (OnWizardComplete)
1. Merges `selectedReligionMemes + selectedIdeologyMemes` into `allMemes`
2. Validates at least one Structure meme exists across both sets
3. Creates final `Ideo` with merged meme list
4. Sets `Page_ChooseIdeoPreset.presetSelection = CustomFixed` via reflection
5. Sets `Page_ChooseIdeoPreset.classicIdeo = finalIdeo` via reflection
6. Calls `Page_ChooseIdeoPreset.AssignIdeoToPlayer(finalIdeo)` via reflection
7. Regenerates starting pawns via `ScenPart_ConfigPage_ConfigureStartingPawns.GenerateStartingPawns()` reflection
8. Navigates to `parentPage.next` (= `Page_ConfigureStartingPawns`)

### Step 4.5 After Merger (ideology assigned)
- Initializes `Ideo.style` field (IdeoStyleTracker) if null — prevents NRE in `RecalculateAvailableStyleItems` during pawn generation
- Calls `IdeoStyleTracker.RecalculateAvailableStyleItems()` directly

## Classification System (BeliefCategory.cs)

### Religion category (supernatural/spiritual)
| defName | Type |
|---|---|
| Structure_Animist, Structure_TheistEmbodied, Structure_TheistAbstract, Structure_Archist, Structure_OriginChristian, Structure_OriginIslamic, Structure_OriginHindu, Structure_OriginBuddhist | Structure |
| Proselytizer, Blindsight, HighLife, PainIsVirtue, Cannibal, Nudism, TreeConnection, Darkness, AnimalPersonhood, Tunneler | Normal |
| MaleSupremacy, FemaleSupremacy, HumanPrimacy, NaturePrimacy, FleshPurity | Normal (moved from Ideology) |

### Ideology category (social/political)
| defName | Type |
|---|---|
| Structure_Ideological | Structure |
| Transhumanist, Supremacist, Loyalist, Guilty, Individualist, Collectivist, Rancher, Raider | Normal |

### Exclusion Groups
```
MaleSupremacy ↔ FemaleSupremacy
HumanPrimacy  ↔ NaturePrimacy
FleshPurity   ↔ Transhumanist
FleshPurity   ↔ HighLife
AnimalPersonhood ↔ Rancher
```

## Reflection Accessed Members

### Page_ChooseIdeoPreset
- **Field `presetSelection`**: Enum type, set to `"CustomFixed"` to indicate custom ideo
- **Field `classicIdeo`**: `Ideo` reference for the custom ideo
- **Method `AssignIdeoToPlayer(Ideo)`**: Assigns the ideo to the player faction

### ScenPart_ConfigPage_ConfigureStartingPawns
- **Method `ClearAllStartingPawns()`**: Clears pawn list
- **Method `GenerateStartingPawns()`**: (Re)generates starting pawns
- Found via `Find.Scenario.parts` list → type name match

### Ideo internals (in SetupIdeo)
- **Field `precepts`**: `List<Precept>` — must be initialized to empty list before `RecachePrecepts()` to avoid NRE
- **Field `factionIdeoWeaponPairs`**: Generic `List<FactionIdeoWeaponPair>` — must be initialized before `RecachePrecepts()` to avoid NRE in `CanAddPreceptAllFactions`

### Ideo style field (in OnWizardComplete)
- **Field `style`**: `IdeoStyleTracker` — `new Ideo()` leaves this null. Must initialize before `AssignIdeoToPlayer` to prevent NRE in `GetFrequency` / `RecalculateAvailableStyleItems` during pawn generation.
- `IdeoStyleTracker` is a standalone class in `RimWorld` namespace (NOT a nested class).
- Constructor takes one `Ideo` parameter.
- `RecalculateAvailableStyleItems()` must be called after construction.

## Known Issues & Edge Cases (PHASE 1) (ASSUME RESOLVED UNTIL FURTHER NOTICE)

### `Ideo` Constructor Leaves Internal Fields Null
`new Ideo()` does not initialize `precepts`, `factionIdeoWeaponPairs`, or `style` (IdeoStyleTracker). These must be manually initialized before any recache or pawn generation operations. This is the primary source of fragility when constructing ideos outside the vanilla preset system — consider replacing manual `new Ideo()` with `IdeoGenerator.GenerateIdeo()` if more null fields surface.

### Pawn List Emptied By Ideo Assignment
`AssignIdeoToPlayer` clears `Find.GameInitData.startingAndOptionalPawns`. Pawns must be regenerated by calling `GenerateStartingPawns()` on the matching `ScenPart_ConfigPage_ConfigureStartingPawns` instance found in `Find.Scenario.parts`.

### `ApplySelectedStylesToIdeo` Crashes
This method on `Page_ChooseIdeoPreset` NREs when called from the wizard context. It was removed from the flow entirely.

### Fluid Ideology Not Intercepted
The Harmony prefix on `DoCustomize` checks `fluid` parameter — returns `true` (pass through to vanilla) if fluid. Only classic (non-fluid) customization is intercepted.

### Names From Steps 2 and 4
Religion name and ideology name are stored in fields but are NOT applied to the final ideo. The `finalIdeo.name` is not set. This is a known gap.

### Building

**One-liner (from project root):**
```
dotnet build Sources/IdeoRework.csproj
```

**Full path (works from anywhere):**
```
dotnet build /home/skuffed/.local/share/Steam/steamapps/workshop/content/294100/ideorework/Sources/IdeoRework.csproj
```

- Output: `1.6/Assemblies/IdeoRework.dll`
- NuGet dependency: `Lib.Harmony.Ref 2.3.1`
- Game DLLs referenced from `RimWorldLinux_Data/Managed/Assembly-CSharp.dll` (and related Unity DLLs)
- After building, restart RimWorld (or reload) to pick up the new DLL. No additional deployment steps needed.

## Development Environment
- Game path: `/home/skuffed/.local/share/Steam/steamapps/common/RimWorld/`
- Mod path: same root + `/Mods/ideorework/` (symlinked from workshop content directory)
- Editor: VS Code with C#/OmniSharp
- Debugging: Game output log (`~/config/unity3d/Ludeon Studios/RimWorld/Player.log`)

## Changelog

### 1.3.0 — 2026-05-30
- Fixed `ideo.foundation.place` being null: picks a random `PlaceDef` before `RegenerateDescription()` — resolves all `place_foeSoldiers`/`place_foeLeader` grammar unresolvable errors and `foeSoldiers → ERR:` fallback
- Fixed `ideo.foundation.RandomizeStyles()` calling wrong class: now invokes private `Ideo.RandomizeStyles()` via reflection instead — resolves style recalculation NRE

### 1.2.0 — 2026-05-30
- `SetupIdeo` now runs the full vanilla `IdeoFoundation` Init chain minus `RandomizeMemes`: calls `GenerateTextSymbols()`, `GenerateLeaderTitle()`, `RandomizeIcon()`, `RegenerateDescription(true)`, and `RandomizeStyles()` after `InitPrecepts()`
- Added null-guard initialization for `usedSymbolPacks` and `usedSymbols` fields (prevents `TryGuessChosenSymbolPack` NRE during precept name generation)
- Changed `(IdeoEditMode)2` (Dev) to `IdeoEditMode.GameStart` in `DrawCustomizationStep` — prevents UI from triggering redundant `RandomizePrecepts` calls and fixes precept name display
- `RandomizeStyles()` call populates `thingStyleCategories` with meme-appropriate style categories, fixing `IdeoStyleTracker.GetFrequency` NRE during pawn generation

### 1.1.0 — 2026-05-30
- `SetupIdeo` now creates `IdeoFoundation` with `InitPrecepts()` to auto-generate precepts from selected memes (Bug 1 fix)
- `SetupIdeo` initializes `thingStyleCategories` to prevent NRE in `IdeoStyleTracker.RecalculateAvailableStyleItems()` (Bug 2 root cause fix)
- `IdeoStyleTracker` init moved into `SetupIdeo`; duplicate block removed from `OnWizardComplete`
- `SetupIdeo` now takes `List<MemeDef> selectedMemes` parameter instead of relying on pre-set `ideo.memes`

### 1.0.0 — 2026-05-30
- Initial release: 4-step wizard replacing `Page_ConfigureIdeo`
- Meme classification into Religion vs Ideology categories
- Hardcoded mutual exclusivity groups for conflicting memes
- `Ideo` internal list initialization fix for `RecachePrecepts()`
- Starting pawn regeneration after ideo assignment via reflection
- `IdeoStyleTracker` initialization fix for pawn generation NRE
