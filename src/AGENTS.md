# Source Guide

This file is a thin index for agents and contributors working under `src/`.
It is intentionally brief so it stays stable as the code evolves.

## Start Here

- Read [`../docs/architecture/system-overview.md`](../docs/architecture/system-overview.md) for the current responsibility split.
- Read [`../docs/getting-started/development-setup.md`](../docs/getting-started/development-setup.md) for build and test workflow.
- Read [`../docs/operations/deploy.md`](../docs/operations/deploy.md) if your change affects shipped files or the game install layout.

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
- Prefer updating `docs/` instead of expanding this file with detailed behavior notes.
- If a change affects deployment, generated files, or user workflow, add or update the matching doc under `docs/`.
