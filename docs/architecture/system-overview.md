# System Overview

Use this page when you need the repository responsibility map, want to decide where a change belongs, or need a quick reminder of which layer owns what.

If you are just joining the project, start with [Start Here](../getting-started/start-here.md) first.

## When To Read This

Read this page before:

1. moving code across `src/`, `tools/`, `defaults/`, or `docs/`
2. adding a new runtime feature and deciding whether it belongs in `RandomLoadout` or `RandomLoadout.Core`
3. splitting a large class or introducing a new project area

## Must Read First

Before planning a code change, read:

1. [Start Here](../getting-started/start-here.md)
2. [Terminology And Naming](../reference/terminology.md)
3. [Runtime Hotspots](./runtime-hotspots.md)
4. [Source Guide](../../src/AGENTS.md)

## 30-Second Summary

`RandomLoadout` is split into:

- ETG runtime integration in `src/RandomLoadout/`
- pure selection and config logic in `src/RandomLoadout.Core/`
- automated tests for core behavior in `tests/RandomLoadout.Core.Tests/`
- operational tooling in `tools/`
- deploy baselines in `defaults/`
- project knowledge in `docs/`

If a change needs Unity, ETG, BepInEx, scene state, live pickups, or runtime hooks, it belongs in `src/RandomLoadout/`.

If a change is pure parsing, selection, or rule evaluation, it should go in `src/RandomLoadout.Core/` first when possible.

## Responsibility Map

### `src/RandomLoadout/`

This is the BepInEx and ETG-facing runtime layer.

It owns:

- plugin startup and BepInEx lifecycle
- scene and run observation
- config, alias, and generated catalog loading
- ETG name and ID resolution against live game data
- pickup grant flow
- logging and in-game command UI
- ETG runtime features such as Boss Rush and character-select-hub behavior

This layer is allowed to depend on Unity, BepInEx, and game assemblies.

### `src/RandomLoadout.Core/`

This is the pure logic layer.

It owns:

- loadout rule models
- command parsing models
- random selection and duplicate filtering
- seed-driven reproducible selection behavior
- warnings and result objects returned back to the runtime layer

This layer should stay free of Unity, BepInEx, and ETG runtime types.

### `tests/RandomLoadout.Core.Tests/`

This is the lightweight automated test layer.

It owns:

- parser validation
- selection behavior validation
- fallback and compatibility checks for core logic

### `tools/`

This is the operational tooling layer.

It owns:

- build and test entrypoints
- deployment into the game directory
- generated documentation helpers
- log extraction and workflow utilities
- repository-specific static checks

### `defaults/`

This is the repository baseline data layer.

It owns:

- shipped config defaults
- shipped pickup catalog snapshots
- shipped fallback full-pool template

These files are deploy baselines, not the live source of truth once the plugin has exported fresh runtime data into the game directory.

### `docs/`

This is the long-form documentation layer.

It owns:

- handoff and workflow guidance
- reference docs
- architecture notes
- operations and testing guidance
- historical notes and decisions

## Placement Rules

When adding or changing behavior:

- put pure decision logic in `src/RandomLoadout.Core/` first when possible
- keep ETG-specific reflection, runtime database lookup, scene logic, and item grant behavior in `src/RandomLoadout/`
- put workflow logic in `tools/`, not in source runtime code
- put user, operator, and project knowledge in `docs/`, not in source comments unless it directly explains code behavior

## Structure Rules

For larger runtime classes, prefer:

- a thin entry file
- responsibility-focused partial files

Current examples:

- `Plugin*.cs`
- `InGameCommandController*.cs`
- `FoyerCharacterSwitchService*.cs`
- `GrantCommandService*.cs`
- `EtgPickupResolver*.cs`
- `JsonLoadoutRuleFileProvider*.cs`
- `BossRushService*.cs`

If a type already uses this pattern, extend the matching partial instead of growing the thin entry file again.

## Common Placement Decisions

Use these quick calls when the boundary feels fuzzy:

- new ETG hook or scene transition behavior:
  `src/RandomLoadout/`
- new parser or deterministic selection rule:
  `src/RandomLoadout.Core/`
- build, deploy, log, or repository workflow helper:
  `tools/`
- shipped baseline JSON, localization, or exported catalog seed:
  `defaults/`
- explanations, checklists, architecture notes, or handoff guidance:
  `docs/`

## Read Next

- Runtime constraints:
  [./runtime-hotspots.md](./runtime-hotspots.md)
- Runtime editing checklist:
  [../../src/AGENTS.md](../../src/AGENTS.md)
- Build and test workflow:
  [../getting-started/development-setup.md](../getting-started/development-setup.md)
- Runtime validation:
  [../reference/testing-matrix.md](../reference/testing-matrix.md)
