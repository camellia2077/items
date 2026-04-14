# ModTheGungeonAPI Reference

Use this page when you need curated upstream references from `ModTheGungeonAPI` that are relevant to `RandomLoadout`.

This is not a full upstream mirror. It is a project-focused reference index for adapting patterns safely.

## When To Read This

Read this page before:

1. copying a `ModTheGungeonAPI` pattern into runtime code
2. changing character-select-hub flow, item grant flow, or Boss Rush transitions
3. assuming an upstream helper is safe as a drop-in replacement

## Must Read First

Before copying or adapting an ETG runtime pattern from upstream, read:

1. [Terminology And Naming](./terminology.md)
2. [Runtime Hotspots](../architecture/runtime-hotspots.md)
3. [Testing Matrix](./testing-matrix.md)
4. [Source Guide](../../src/AGENTS.md)

## 30-Second Summary

Use `ModTheGungeonAPI` as a behavioral reference, not as a promise that the exact same call sequence is safe in `RandomLoadout`.

The highest-value references in this project are:

- character switch flow
- item grant flow
- level load and return-to-character-select flow
- unlock and persistence-related call patterns

If upstream already uses a stable vanilla route, prefer that route over inventing a harder custom transition.

## Primary Upstream

- Repository:
  [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI)
- Main console implementation:
  [`ETGModConsole.cs`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs)

## High-Value Upstream References

### Character Switch Flow

Reference:

- [`ETGModConsole.cs#L1942`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs#L1942)

Why it matters here:

- useful reference for force-switch behavior
- useful when native character-select callback routes are unstable or have side effects

### Give Command Flow

Reference:

- `PlayerControllerExt.GiveItem(this PlayerController player, string id)`
- internally uses `LootEngine.TryGivePrefabToPlayer(...)`

Why it matters here:

- primary behavioral reference for runtime item grant flow
- one reason this project prefers `pickupId` and `internalName` over localized display names

### Runtime Object Unlock Pattern

Reference:

- [`ETGModConsole.cs#L1746`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs#L1746)

Why it matters here:

- shows a common unlock call shape in mod code paths
- should be treated as an object-unlock reference, not a guaranteed persistent character-unlock recipe

### Level Load And Character Select Flow

Relevant upstream behaviors:

- `load_level` calls `Foyer.Instance.OnDepartedFoyer()` before `LoadCustomLevel(...)`
- `charselect` uses `GameManager.Instance.DelayedLoadCharacterSelect(...)`

Why it matters here:

- these flows are safer references than hard custom transitions when leaving or returning to the character-select hub
- they are directly relevant to Boss Rush floor entry and Boss Rush exit behavior

## Related External Reference

When the question is really about save or flag persistence, also check:

- [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
- [`SaveAPIManager.cs#L417`](https://github.com/SpecialAPI/SaveAPI/blob/main/SaveAPIManager.cs#L417)

## How RandomLoadout Uses These References

Current project patterns:

- character switching:
  uses `ETGModConsole.SwitchCharacter` as a behavioral reference, but relies on project-specific force-switch paths when native routes are unstable
- item grant:
  uses `ModTheGungeonAPI give` as the primary runtime reference
- pickup lookup:
  prefers `pickupId` and `internalName` first, then uses `displayName` only as a compatibility fallback
- Boss Rush flow:
  uses upstream character-select and level-load behavior as a guide for safer flow transitions

## Local Code Areas That Commonly Use These Patterns

- [`../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs`](../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs)
- [`../../src/RandomLoadout/Etg/EtgPickupResolver.cs`](../../src/RandomLoadout/Etg/EtgPickupResolver.cs)
- [`../../src/RandomLoadout/Etg/EtgPickupGranter.cs`](../../src/RandomLoadout/Etg/EtgPickupGranter.cs)
- [`../../src/RandomLoadout/Runtime/BossRushService.cs`](../../src/RandomLoadout/Runtime/BossRushService.cs)

Implementation details for several of these are split across matching `*.cs` partial files.

## Rules For Using Upstream Examples

- treat upstream code as a behavioral reference, not a drop-in guarantee
- prefer vanilla ETG flow over custom hard cuts when upstream already uses a stable built-in route
- validate behavior in the live ETG runtime after adapting a pattern
- for persistence-sensitive logic, require save-slot and restart validation
- do not assume upstream naming matches this project's terminology rules; align with [Terminology And Naming](./terminology.md)

## Read Alongside This Page

- [Commands](./commands.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)
- [Runtime Hotspots](../architecture/runtime-hotspots.md)

## Read Next

- Runtime terminology:
  [./terminology.md](./terminology.md)
- Runtime validation:
  [./testing-matrix.md](./testing-matrix.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
- Development workflow:
  [../getting-started/development-setup.md](../getting-started/development-setup.md)
