# Architecture

This page is a lightweight map of the repository.
It intentionally focuses on responsibilities and boundaries rather than detailed implementation notes.

## Goal

`RandomLoadout` is organized around a simple split:

- runtime integration lives close to the game and mod loader
- selection logic lives in a testable core
- generated data, scripts, and user-facing docs stay outside the runtime code

## Responsibility Split

### `src/RandomLoadout/`

This is the BepInEx and ETG-facing runtime layer.

It is responsible for:

- plugin startup and BepInEx lifecycle
- scene and run observation
- loading config, aliases, and generated catalog files
- resolving ETG names and IDs against the live game database
- granting pickups to the player
- logging and in-game command UI

This layer is allowed to depend on Unity, BepInEx, and game assemblies.

### `src/RandomLoadout.Core/`

This is the pure logic layer.

It is responsible for:

- loadout rule models
- command parsing models
- random selection and duplicate filtering
- seed-driven reproducible selection behavior
- warnings and result objects returned back to the runtime layer

This layer should stay free of Unity, BepInEx, and ETG runtime types.

### `tests/RandomLoadout.Core.Tests/`

This is the lightweight automated test layer.

It is responsible for:

- validating core selection behavior
- validating parser and configuration edge cases
- protecting fallback and compatibility rules that are easy to regress

### `scripts/`

This is the operational tooling layer.

It is responsible for:

- build and test entrypoints
- deployment into the game directory
- generated documentation helpers
- log extraction and other workflow utilities

### `defaults/`

This is the repository baseline data layer.

It is responsible for:

- shipped config defaults
- shipped pickup catalog snapshots
- shipped fallback full-pool template

These files are baseline inputs for deploys and documentation generation.
They are not the source of truth for live game state once the plugin has exported fresh runtime data into the game directory.

### `docs/`

This is the long-form documentation layer.

It is responsible for:

- development and deploy workflow
- logging and command usage
- generated pickup reference material
- history and research notes

## Working Rule Of Thumb

When adding or changing behavior:

- put pure decision logic in `src/RandomLoadout.Core/` first when possible
- keep ETG-specific reflection, database lookup, and grant behavior in `src/RandomLoadout/`
- keep generated files and scripted workflows in `scripts/`, `defaults/`, and `docs/`

## Recommended Reading Order

- `src/AGENTS.md`
- `docs/architecture.md`
- `docs/development.md`
- `docs/deploy.md`
- `docs/commands.md`

Then drill into source files as needed.
