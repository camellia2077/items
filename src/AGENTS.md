# Source Guide

This file is the runtime-editing checklist for agents and contributors working under `src/`.

Do not treat it as optional reading for ETG runtime changes.

## Must Read First

Before changing ETG runtime code, read:

- [`../docs/getting-started/start-here.md`](../docs/getting-started/start-here.md)
- [`../docs/reference/terminology.md`](../docs/reference/terminology.md)
- [`../docs/architecture/runtime-hotspots.md`](../docs/architecture/runtime-hotspots.md)
- [`../docs/reference/testing-matrix.md`](../docs/reference/testing-matrix.md)

## Task Index

Use this quick map after you know which area you are changing.

### Architecture And Boundaries

- [`../docs/architecture/system-overview.md`](../docs/architecture/system-overview.md)

### Command Panel, Pickup Lookup, And Item Grant

- [`../docs/reference/commands.md`](../docs/reference/commands.md)
- [`../docs/reference/modthegungeonapi.md`](../docs/reference/modthegungeonapi.md)
- [`../docs/decisions/pickup-grant-strategy.md`](../docs/decisions/pickup-grant-strategy.md)
- [`../docs/reference/pickups.md`](../docs/reference/pickups.md)

### Character-Select-Hub Switching

- [`../docs/reference/commands.md`](../docs/reference/commands.md)
- [`../docs/decisions/character-switch-strategy.md`](../docs/decisions/character-switch-strategy.md)
- [`../docs/reference/modthegungeonapi.md`](../docs/reference/modthegungeonapi.md)

### Config, Aliases, And Rule Format

- [`../docs/reference/config-format.md`](../docs/reference/config-format.md)
- [`../docs/reference/commands.md`](../docs/reference/commands.md)

### Build, Deploy, And Logs

- [`../docs/getting-started/development-setup.md`](../docs/getting-started/development-setup.md)
- [`../docs/operations/deploy.md`](../docs/operations/deploy.md)
- [`../docs/operations/logging.md`](../docs/operations/logging.md)

### Release History And Prior Decisions

- [`../docs/history/`](../docs/history/)
- [`../docs/decisions/`](../docs/decisions/)

## Directory Intent

### `src/RandomLoadout/`

Treat this as the runtime integration layer.

- BepInEx startup
- Unity and ETG hooks
- scene and run lifecycle
- config and alias loading
- pickup resolution and granting
- logging and in-game command UI

### `src/RandomLoadout.Core/`

Treat this as the pure logic layer.

- rule and request models
- parsing models
- selection logic
- deterministic seed behavior
- structured warnings and results

## ETG Runtime Checklist

Before editing:

- Do not assume a scene token is the same thing as a gameplay state.
- Confirm whether the target area is listed in [`../docs/architecture/runtime-hotspots.md`](../docs/architecture/runtime-hotspots.md).
- Prefer the documented gameplay term first, then map it to the ETG token or ETG runtime type.
- Check whether the change can stay in `src/RandomLoadout.Core/` instead of runtime ETG code.

When editing:

- Prefer keeping new game-specific behavior in `src/RandomLoadout/`.
- Prefer moving reusable or testable decision logic into `src/RandomLoadout.Core/`.
- Prefer vanilla ETG flow over hard custom transitions.
- Do not directly hard-cut scene or player state if a stable vanilla path exists.
- Before editing a Harmony hook, verify the target signature and parameter names first.
- When a type already uses `*.cs` partial siblings, add or move behavior into the matching responsibility file instead of growing the main entry file again.
- Examples in the current codebase include `Plugin*.cs`, `InGameCommandController*.cs`, `FoyerCharacterSwitchService*.cs`, `GrantCommandService*.cs`, `EtgPickupResolver*.cs`, `JsonLoadoutRuleFileProvider*.cs`, and `BossRushService*.cs`.

After editing:

- Run the checks required by [`../docs/reference/testing-matrix.md`](../docs/reference/testing-matrix.md).
- Read the BepInEx log after any ETG runtime, hook, or scene-transition change.
- If a change affects deployment, generated files, workflow, or terminology, update the matching doc under `docs/`.
