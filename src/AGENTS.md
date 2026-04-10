# Source Guide

This file is a thin index for agents and contributors working under `src/`.
It is intentionally brief so it stays stable as the code evolves.

## Start Here

- Read [`../docs/architecture/system-overview.md`](../docs/architecture/system-overview.md) for the current responsibility split.
- Read [`../docs/getting-started/development-setup.md`](../docs/getting-started/development-setup.md) for build and test workflow.
- Read [`../docs/operations/deploy.md`](../docs/operations/deploy.md) if your change affects shipped files or the game install layout.

## Task Index

Use this quick map after you know which area you are changing.

### Architecture And Boundaries

- [`../docs/architecture/system-overview.md`](../docs/architecture/system-overview.md)

### Command Panel, Pickup Lookup, And Item Grant

- [`../docs/reference/commands.md`](../docs/reference/commands.md)
- [`../docs/reference/modthegungeonapi.md`](../docs/reference/modthegungeonapi.md)
- [`../docs/decisions/pickup-grant-strategy.md`](../docs/decisions/pickup-grant-strategy.md)
- [`../docs/reference/pickups.md`](../docs/reference/pickups.md)

### Breach Character Switching

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

## Editing Guidance

- Prefer keeping new game-specific behavior in `src/RandomLoadout/`.
- Prefer moving reusable or testable decision logic into `src/RandomLoadout.Core/`.
- Prefer following the current `thin entry file + responsibility-focused partial files` pattern for larger runtime classes.
- When a type already uses `*.cs` partial siblings, add or move behavior into the matching responsibility file instead of growing the main entry file again.
- Examples in the current codebase include `Plugin*.cs`, `InGameCommandController*.cs`, `FoyerCharacterSwitchService*.cs`, `GrantCommandService*.cs`, `EtgPickupResolver*.cs`, and `JsonLoadoutRuleFileProvider*.cs`.
- Prefer updating `docs/` instead of expanding this file with detailed behavior notes.
- If a change affects deployment, generated files, or user workflow, add or update the matching doc under `docs/`.
