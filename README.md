# RandomLoadout

`RandomLoadout` is an `Enter the Gungeon` mod built on `BepInEx`.
The project started as a random starting-loadout prototype and is now being reorganized into a cleaner runtime layer, pure core logic layer, and script/documentation layer so later feature work can grow on a steadier base.

## Project Overview

- Grants a run-start loadout after the dungeon has actually loaded
- Keeps the current working baseline behavior:
  - `1` gun
  - `1` passive
  - `1` active
- Supports rule-driven selection in code:
  - random rules
  - specific pickup-by-name rules
- Includes a minimal in-game command panel for quick manual grants and debugging
- Separates Unity/ETG integration from testable core selection logic

## Repository Structure

- `src/RandomLoadout/`
  BepInEx plugin project, Unity runtime integration, ETG-facing code
- `src/RandomLoadout.Core/`
  Pure configuration and selection logic, no Unity dependencies
- `tests/RandomLoadout.Core.Tests/`
  Lightweight automated tests for core behavior
- `scripts/`
  Build, test, deploy, and log helper scripts
- `defaults/`
  Repository-shipped baseline config files and pickup catalog snapshots used for first deploys
- `docs/`
  Detailed documentation, notes, research, and operational guidance
- `lib/`
  Local dependency drop folder for game and modding DLLs

## Current Focus

The current codebase is centered on three goals:

- keeping the already-working gameplay behavior stable
- improving maintainability through responsibility-based structure
- making future features easier to add, especially around configurable or manual item grants

## Open Source Dependencies

This project currently builds on the following open source projects and ecosystems:

- [`BepInEx`](https://github.com/BepInEx/BepInEx)
  The plugin loader and runtime framework used to load the mod into `Enter the Gungeon`
- [`HarmonyX`](https://github.com/BepInEx/HarmonyX)
  Part of the BepInEx ecosystem commonly used for runtime patching and compatibility in Unity modding setups
- [`MonoMod`](https://github.com/MonoMod/MonoMod)
  Part of the broader Unity/.NET modding toolchain used alongside BepInEx-based environments

The mod also depends on local game assemblies from `Enter the Gungeon` and Unity for compilation, but those are not open source project dependencies of this repository.

## Documentation

Detailed usage and maintenance notes have been moved under [`docs/`](./docs/README.md).

Useful entry points:

- [`docs/README.md`](./docs/README.md)
  General documentation index
- [`docs/development.md`](./docs/development.md)
  Build, test, dependencies, and tooling workflow
- [`docs/architecture.md`](./docs/architecture.md)
  Lightweight repository responsibility map
- [`docs/deploy.md`](./docs/deploy.md)
  Deployment steps and notes
- [`docs/logging.md`](./docs/logging.md)
  Log prefixes and log extraction workflow
- [`docs/commands.md`](./docs/commands.md)
  In-game command panel usage
- [`docs/pickups.md`](./docs/pickups.md)
  Generated player-facing pickup reference grouped by category
- [`docs/history/0.1.1.md`](./docs/history/0.1.1.md)
  Latest milestone history
- [`docs/history/0.1.0.md`](./docs/history/0.1.0.md)
  Initial working baseline milestone
- [`docs/note/cmd.md`](./docs/note/cmd.md)
  Command-related working notes
- [`docs/research/project-scope.md`](./docs/research/project-scope.md)
  Scope and constraints
- [`docs/research/implementation-guidance.md`](./docs/research/implementation-guidance.md)
  Implementation guidance and next-step context

## Development Entry Points

For day-to-day development, the main entry points are:

- `.\scripts\build.ps1`
- `.\scripts\test.ps1`
- `python .\scripts\deploy_mod.py "<game path>"`
- `python .\scripts\extract_randomloadout_log.py "<BepInEx log path>"`

PowerShell wrappers remain available, but build and test logic now lives in Python scripts under `scripts/`.

## Default Baselines

The repository includes read-only baseline files under `defaults/`.

- `defaults/config/`
  Repository default config files copied into `BepInEx\config` on deploy
- `defaults/catalog/`
  Repository snapshots of the exported pickup catalog and full random rule pool

These files are intended as a stable fallback for this older game.
At deploy time they can be copied into the game directory if missing.
At runtime the plugin writes fresh export files into the game directory and those runtime-generated files replace the game-side copies there.
The repository copies remain the baseline and are not modified by the plugin.

## Solution

The repository also includes `RandomLoadout.sln` for IDE-based work.
