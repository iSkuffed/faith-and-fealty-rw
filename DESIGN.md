# Ideology & Religion Rework — Design Philosophy

## Core Principle

**This is a complete overhaul of RimWorld's Ideology system.** We own both Ideology and Religion entirely. We do not patch vanilla systems to work with us — we implement our own systems from scratch.

---

## Design Philosophy

### 1. Our System is THE System

Ideology and Religion are both ours. We don't add Religion alongside vanilla's Ideology — we replace the entire role/ability/icon system for both. Vanilla's base structures (memes, precepts, cultures, styles) still exist, but all assignment, tracking, and display logic is ours.

### 2. Never Patch When You Can Implement

If you're debating whether to patch vanilla's code or implement your own system, **always implement your own**. Patches are fragile, break on updates, and create cascading issues. Custom code works once and works forever.

**Bad:** "Let me patch `Precept_RoleSingle.Assign()` to prevent cross-ideo unassignment"
**Good:** "Let me implement `IdeoRoleManager` that directly sets `chosenPawn.pawn` without calling `Assign()`"

### 3. Complex But Robust ONCE

Implement a complicated but robust solution once rather than spending time troubleshooting patches that repeatedly fail. Complexity in our own code is manageable. Complexity in patches is fragile and unpredictable.

**Pattern:** When a patch fails, don't try to fix it. Replace it with custom code.

### 4. No Compromises

Don't compromise with vanilla's systems. If vanilla's code doesn't work with our dual-system approach, we bypass it entirely. We don't try to make vanilla understand our system — we make our system work independently.

### 5. Simple Hooks, Complex Logic

Use simple Harmony hooks only to observe when things happen (ritual completion, pawn death, gizmo display). Implement all complex logic in our own code. Hooks should be observation points, not behavior modifiers.

**Good hook:** Postfix on `RitualOutcomeEffectWorker_RoleChange.Apply()` to detect ritual completion
**Bad hook:** Prefix/postfix on `Precept_RoleSingle.Assign()` with recursion guards and state restoration

---

## Architecture

### Three-Layer System

```
┌─────────────────────────────────────────────────────────────┐
│                    OUR SYSTEM (Everything)                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IdeoRoleManager — Role assignment for both systems  │   │
│  │  IdeoAbilityManager — Ability tracking for both      │   │
│  │  UnifiedIconManager — Icon drawing for both          │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    HOOKS (Simple, Robust)                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Patch_RitualOutcomeEffectWorker_RoleChange           │   │
│  │  Patch_Pawn_AbilityTracker_GetGizmos                  │   │
│  │  Patch_ColonistBarColonistDrawer                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    VANILLA (Base Only)                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Memes, Precepts, Cultures, Styles (structural data) │   │
│  │  Role requirements (validation only)                  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|---|---|
| `IdeoRoleManager` | Manages role assignment for both ideology and religion. Directly sets `chosenPawn.pawn` without calling vanilla's `Assign()`. |
| `IdeoAbilityManager` | Creates and tracks abilities for both systems. Uses `AbilityUtility.MakeAbility()` to create abilities. |
| `UnifiedIconManager` | Draws icons for both systems on the colonist bar using `GUI.DrawTexture`. |
| `ReligionLeaderTracker` | Stores the religion leader pawn reference. Used by `IdeoRoleManager`. |

---

## Technical Notes

### Role Assignment

- `Precept_RoleSingle.chosenPawn` is a **public field** of type `IdeoRoleInstance` — no reflection needed
- `IdeoRoleInstance.pawn` is the assigned pawn — directly settable
- `Precept_RoleSingle.Notify_PawnAssigned(pawn)` must be called after setting `chosenPawn.pawn`
- Never call `Precept_RoleSingle.Assign()` for religion roles — it triggers cross-ideo unassignment

### Ability Creation

- `AbilityUtility.MakeAbility(def, pawn, precept)` is the correct way to create abilities
- `Ability.Initialize()` creates comps, assigns unique ID, wires verb, sets charges
- `Ability.GetGizmos()` lazily creates `Command_Ability` gizmo via `Activator.CreateInstance`
- Cooldowns are persisted via absolute ticks — they survive save/load

### Gizmo Display

- `Pawn_AbilityTracker.GetGizmos()` is an iterator method — use `IEnumerable` wrapping for hooks
- Never try to mutate `__result` directly on iterator methods — always replace with a new `IEnumerable`
- `Ability.GetGizmos()` returns `Command` objects — yield them directly in the wrapper

### Icon Drawing

- `ColonistBarColonistDrawer.DrawColonist(Rect rect, ...)` receives the full entry rect
- Icons are drawn at bottom-left: `new Rect(rect.x + 1, rect.yMax - iconSize - 1, iconSize, iconSize)`
- Use `Find.ColonistBar.Scale` for proper scaling
- Vanilla's icon size is 16x16 at scale 1.0

### XML Framework

- **Sorting Defs** categorize memes as Religion/Ideology/Both
- **Content Defs** define preset religions for AI factions
- Default category for uncategorized memes: "Ideology"
- Default category for uncategorized precepts/styles: "Both"
- Cultures don't need sorting — they're universal
- Structures are categorized through memes (they ARE memes)

### Cooldown System

- Non-certainty abilities share cooldowns via `Leader` group (10 days per pawn)
- Certainty abilities use per-pawn independent cooldowns via `Moralist` group (3 days)
- Cooldowns are per-caster, not per-target — different pawns can use abilities independently
- Cooldowns are persisted via absolute ticks — they survive save/load

---

## Anti-Patterns to Avoid

### ❌ Patching Vanilla's Role System

```csharp
// DON'T: Patch Precept_RoleSingle.Assign with prefix/postfix
[HarmonyPatch(typeof(Precept_RoleSingle))]
[HarmonyPatch("Assign")]
public static class Patch_Precept_RoleSingle_Assign
{
    static bool isRestoring;
    static void Postfix(...)
    {
        if (isRestoring) return;
        isRestoring = true;
        // Complex restoration logic...
        isRestoring = false;
    }
}
```

**Why it fails:** Vanilla's `Assign()` unassigns other leader roles across all ideos. Patching it requires recursion guards, state restoration, and fragile timing.

**Better:** Implement `IdeoRoleManager` that directly sets `chosenPawn.pawn` without calling `Assign()`.

### ❌ Patching Vanilla's Ability System

```csharp
// DON'T: Patch AllAbilitiesForReading with complex logic
[HarmonyPatch(typeof(Pawn_AbilityTracker))]
[HarmonyPatch("AllAbilitiesForReading", MethodType.Getter)]
public static class Patch_Pawn_AbilityTracker
{
    static void Postfix(ref List<Ability> __result, ...)
    {
        // Complex ability merging logic...
    }
}
```

**Why it fails:** `AllAbilitiesForReading` is a getter that returns a cached list. Modifying it requires understanding caching, dirty flags, and ordering.

**Better:** Implement `IdeoAbilityManager` that tracks abilities independently. Use `Pawn_AbilityTracker.GetGizmos()` with `IEnumerable` wrapping to display them.

### ❌ Patching Vanilla's Icon System

```csharp
// DON'T: Try to access private fields via reflection
static void Postfix(Pawn colonist, ref List<object> ___tmpIconsToDraw)
{
    // Type mismatch between List<IconDrawCall> and List<object>...
}
```

**Why it fails:** `IconDrawCall` is a private struct. `List<object>` can't hold it. `IList` works but is fragile.

**Better:** Implement `UnifiedIconManager` that draws icons directly via `GUI.DrawTexture` in a `DrawColonist` postfix.

### ❌ Blind OffsetCertainty Mirror

```csharp
// DON'T: Mirror all ideology certainty changes to religion
static void Postfix(Pawn_IdeoTracker __instance, float offset)
{
    // Blindly apply same offset to religion...
}
```

**Why it fails:** Not all certainty changes are relevant to religion. A pawn doing something their ideology disapproves of shouldn't affect their religion certainty.

**Better:** Implement context-aware certainty changes that check if the event is relevant to the specific system.

---

## Testing Philosophy

When something doesn't work:
1. Don't try to fix the patch
2. Don't add more patches to work around the issue
3. Implement your own system instead
4. Test thoroughly
5. Move on

The goal is to ship a working product, not to prove that vanilla's code can be made compatible with our design.

---

## File Organization

```
Sources/
├── Defs/
│   ├── ReligionCategoryMemeDef.cs      # Sorting Def for memes
│   └── ReligionPresetDef.cs            # Content Def for preset religions
├── IdeoRoleManager.cs                  # Role assignment for both systems
├── IdeoAbilityManager.cs               # Ability tracking for both systems
├── UnifiedIconManager.cs               # Icon drawing for both systems
├── ReligionLeaderTracker.cs            # Stores religion leader pawn
├── ReligionConversionTracker.cs        # Handles religion conversion
├── CognitiveDissonanceTracker.cs       # Handles dissonance between systems
├── PresetReligions.cs                  # Preset religion definitions
├── BeliefCategory.cs                   # Meme categorization
├── ReligionDefLoader.cs                # Loads sorting defs
├── Dialog_BeginReligionRitual.cs       # Custom ritual dialog
├── IdeoReworkGameComponent.cs          # Save/load persistence
├── IdeoReworkMod.cs                    # Mod initialization
├── SessionRegistry.cs                  # Session management
├── HardOverride.cs                     # Hard override for pawn assignment
├── Patch_*.cs                          # Simple hooks (observation only)
└── CompAbilityEffect_*.cs              # Custom ability effects
```

---

## Remember

- **This is a complete overhaul**, not a compatibility patch
- **Own both systems** — Ideology AND Religion are ours
- **Never patch when you can implement** — custom code works once and forever
- **Complex but robust ONCE** — implement the right solution, not the easy one
- **No compromises** — if vanilla doesn't work with us, we bypass it entirely
