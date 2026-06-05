# Faith & Fealty

**Faith & Fealty** is a RimWorld 1.6 mod that completely overhauls the Ideology DLC by splitting its monolithic belief system into two independent, parallel systems — **Religion** (spiritual/supernatural beliefs) and **Ideology** (social/political/economic values). Both run simultaneously on every pawn, with their own roles, abilities, certainty trackers, and dedicated UI.

---

## Features

- **Dual Belief System** — Every pawn has both a Religion and an Ideology, tracked independently. A pawn can be a devout follower of the Holy Light while holding a progressive, egalitarian ideology. Both systems interact through cognitive dissonance, shared cooldowns, and unified leadership mechanics.

- **4-Step Wizard** — Replaces the vanilla ideoligion creation screen with a guided 4-step process: Religion Memes → Religion Customize → Ideology Memes → Ideology Customize. Each step provides full control over memes, precepts, styles, symbols, names, and narratives. Preset templates are available collapsed by default and expandable on click.

- **Preset System & Customizability** — NPC factions are assigned curated religion presets (Imperial Cult for the Empire, Paganism for tribals, Christian and Islamic variants for outlanders). Player presets are defined in simple XML files and serve as quick-start meme templates — all memes remain fully editable after selection. The entire preset system is data-driven: add, remove, or modify presets without touching any code.

- **Religion Certainty** — A certainty system with randomized erosion, passive recovery, and conversion/reassurance mechanics. Certainty dropping to 0% triggers evaluation of other faiths present in the colony or defaults to Agnostic.

- **Cognitive Dissonance** — When certainty reaches 0%, the pawn evaluates other faiths present in the colony. If a different faith exists, they may convert to it. If no other faith is found, they default to Agnostic — a blank placeholder — until converted back or to a different faith.

- **Dual Leadership** — Religion leaders handle spiritual affairs (conversion, reassurance); ideology leaders handle trade, diplomacy, and warfare. Leader abilities share a unified cooldown system and are deduplicated between the two roles.

- **Custom Role Assignment UI** — Replaces vanilla role dropdowns with a dedicated assignment interface supporting both religion and ideology roles simultaneously.

- **Deity System** — Structure memes with deity support (Christian, Islamic, Pantheism, Eldritch, etc.) generate named deities with custom grammar-based name generation and deity name maker overrides.

- **Religion UI** — A dedicated religion browser window matching vanilla's Ideoligion tab layout, with faction icons and full detail views.

- **Child Inheritance** — Children inherit religion from parents or the colony majority.

- **Full Save/Load Support** — Religion and role data persist via custom `IExposable` structs through `IdeoReworkGameComponent`.

---

## How It Works

The mod replaces `Page_ChooseIdeoPreset` with a custom `Dialog_TwoStepIdeoWizard`. When a new game starts, players design their religion first (spiritual foundation), then their ideology (social framework). Both are stored as independent `Ideo` objects with their own meme sets, precepts, styles, and narratives.

At runtime, a static `ReligionIdeoTracker` maps every pawn to their religion ideo alongside the vanilla `Ideo` tracker for ideology. All player pawns are assigned both systems on spawn; NPC pawns inherit their faction's assigned religion via XML-defined presets.

Roles and abilities are managed through custom `IdeoRoleManager` and `IdeoAbilityManager` classes, with save/load support via `IExposable` data structs for full persistence.

---

## Requirements

- RimWorld 1.6
- Harmony (`brrainz.harmony`)
- Ideology DLC

---

## Mod Compatibility

Faith & Fealty is designed as a framework. If your mod adds memes, rituals, or belief-adjacent content, there are two integration paths:

**Submods** — List `skuffed.ideorework` in your mod's `<modDependencies>` and ship `ReligionCategoryMemeDef` entries in your own `Defs/` folder. Your entries are automatically picked up by Faith & Fealty's def database. No patches, no C# changes.

**Conditional patching** — Use your own `LoadFolders.xml` with `IfModActive="skuffed.ideorework"` to load compat content only when Faith & Fealty is present. This is the same pattern Faith & Fealty uses for its own conditional compat (see below).

### Currently Supported

- **Vanilla Ideology Expanded - Memes and Structures** (`VanillaExpanded.vmemese`) — 45 meme categorizations automatically applied when the mod is present. All VME structure and normal memes are sorted into the correct Religion or Ideology wizard steps.

### Expanding Compatibility

This system is designed to grow. Future support for additional mods follows the same `LoadFolders.xml` + `IfModActive` pattern already in place.

No C# patches required. No Harmony conflicts. No fragile hardcoded references.

---

## Technical Notes

- **Design philosophy:** Total system replacement. Faith & Fealty owns Religion and Ideology — Harmony hooks exist only to observe events. All logic resides in custom managers.
- **NPC religion defs:** `Defs/Content/ReligionPresetDefs.xml`
- **Player presets:** `Defs/Content/PlayerPresets.xml`
- **Meme categorization:** `Defs/Sorting/ReligionCategoryMemes.xml`
- **Conditional compat:** `LoadFolders.xml` + `1.6/Mods/` subfolders
