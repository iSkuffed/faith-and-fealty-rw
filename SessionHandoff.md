# Session Handoff Context

## Testing Workflow
- User tests in-game, then provides `DEBUG.md` contents from the game log (`~/config/unity3d/Ludeon Studios/RimWorld/Player.log`)
- Agent analyzes errors in DEBUG.md, proposes fixes, user approves, agent implements, builds, user re-tests
- **DEBUG.md is user-owned** — agent never modifies it

## Debug Log Analysis Patterns
- **Grammar errors**: Look for `Grammar unresolvable. Root '...'` followed by `GRAMMAR RESOLUTION TRACE`. Unresolvable means the rule isn't in the grammar at all. `ERR: ...` fallback means the rule exists but resolves to an error placeholder.
- **NRE stack traces**: Always check the line number in the agent's own source files first (the mod's .cs files). The call stack shows exact file:line.
- **Style recalculation NREs**: Root cause is `ideo.culture` being null. `RandomizePlace()` and `RandomizeStyles()` both access `ideo.culture` fields.

## RiMCP_hybrid / RimWorld Code RAG State

### Current State
- **`RimWorldData/Source/`**: Contains full decompiled C# source from `Assembly-CSharp.dll` (9,262 files) + `Assembly-CSharp-firstpass.dll` (46 files) via ILSpy
- **Index**: Built and current:
  - Lucene: 77,009 chunks indexed
  - Embeddings: 77,009/77,009 generated (e5-base-v2 on CUDA)
  - Graph: 249,745 edges (C#→C#: 227,737, XML→C#: 10,269, XML→XML: 1,470, reverse: 10,269)
  - PageRank: calculated
- **Embedding server**: Running on `http://127.0.0.1:5000` (flask + torch)
- **Restart opencode to pick up the new index**

### Known Limits
- Syntactic analysis only (no Roslyn-based semantic analysis) — graph edges are approximate

## Key Learnings (from decompiled source, corrected from initial `strings`-only analysis)
- `RandomizeStyles()` is on `IdeoFoundation`, NOT on `Ideo` — no reflection needed
- `Ideo()` constructor creates `this.style = new IdeoStyleTracker(this)` — `ideo.style` is never null after construction
- **Root cause of ALL NREs and grammar errors**: `ideo.culture` was null
  - `RandomizePlace()` filters `PlaceDef` by `ideo.culture.allowedPlaceTags` — crashes if culture is null
  - `RandomizeStyles()` accesses `ideo.culture.thingStyleCategories` and `ideo.culture.styleItemTags` — crashes if culture is null
  - Grammar resolution needs `ideo.foundation.place_foeSoldiers` etc. — crashes if place is null
- Fix chain: `RandomizeCulture(parms)` → `RandomizePlace()` → then everything else works
- `IdeoFoundation.RandomizeMemes()` must NEVER be called (preserves player's meme selections)
- `IdeoGenerationParms` constructor requires a `FactionDef` — use `FactionDefOf.PlayerColony`
- Name validation uses `text.NullOrEmpty()` + `Messages.Message()` with `MessageTypeDefOf.RejectInput` — blocks wizard advancement
- Grammar resolution rules like `place_foeSoldiers` and `place_foeLeader` come from `IdeoFoundation.AddPlaceRules()` — populated by `RandomizePlace()`

## Done in Current Session
- Fixed all runtime errors in `Dialog_TwoStepIdeoWizard.cs`:
  - Added `ideo.foundation.RandomizeCulture(parms)` and `ideo.foundation.RandomizePlace()` before `RegenerateDescription()`
  - Reverted `RandomizeStyles()` back to `ideo.foundation.RandomizeStyles()` (not reflection)
  - Removed manual `place` assignment (unnecessary with RandomizePlace())
  - Removed reflection-based style tracker fallback (unnecessary)
  - Added non-empty name validation for both religion and ideology name fields
- **Decompiled** `Assembly-CSharp.dll` (9,226 types) + `Assembly-CSharp-firstpass.dll` (46 types) to `RimWorld/Source/`
- **Set up** Python embedding server (flask, torch, transformers, e5-base-v2 on CUDA)
- **Built** full RiMCP_hybrid index: 77,009 chunks, 249,745 graph edges, PageRank scored
- **RAG C# fallback** (`strings` on DLL) is no longer needed — full decompiled source is indexed

## AI Council Protocol
- Multiple AI instances may provide suggestions simultaneously
- User arbitrates via AI Council
- Suggestions are advisory only — agent evaluates merit independently
- No suggestion should be followed blindly; verify against DEBUG.md evidence

## Remaining Gaps
- Religion/Ideology names from Steps 2 and 4 are stored but not applied to the final ideo (low priority)
- Long-term DESIGN.md vision (2 distinct belief systems per pawn) not yet implemented
