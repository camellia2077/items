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
  - specific pickup rules via `pickupId`, `alias`, `internalName`, or compatible `displayName` input
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

This project currently has one direct open source runtime dependency:

- [`BepInEx`](https://github.com/BepInEx/BepInEx)
  The plugin loader and runtime framework used to load the mod into `Enter the Gungeon`

The mod also depends on local game and Unity assemblies for compilation, including `Assembly-CSharp.dll` and `UnityEngine*.dll`, but those are installation-local dependencies rather than open source repository dependencies of this project.

`HarmonyX` and `MonoMod` remain relevant parts of the broader `BepInEx` ecosystem, but they are not currently direct project references in this repository.

## Referenced Repositories

The project also uses the following repositories as implementation references and workflow guidance:

- [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI)
  Used as a practical reference for ETG runtime interaction patterns, especially command-style item grant flow and character switching behavior.
- [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
  Used as a practical reference for save and persistence workflow patterns in ETG modding.

## Documentation

Detailed usage and maintenance notes have been moved under [`docs/`](./docs/README.md).

Useful entry points:

- [`docs/README.md`](./docs/README.md)
  General documentation index
- [`docs/getting-started/development-setup.md`](./docs/getting-started/development-setup.md)
  Build, test, dependencies, and tooling workflow
- [`docs/architecture/system-overview.md`](./docs/architecture/system-overview.md)
  Lightweight repository responsibility map
- [`docs/operations/deploy.md`](./docs/operations/deploy.md)
  Deployment steps and notes
- [`docs/operations/logging.md`](./docs/operations/logging.md)
  Log prefixes and log extraction workflow
- [`docs/reference/commands.md`](./docs/reference/commands.md)
  In-game command panel usage
- [`docs/reference/pickups.md`](./docs/reference/pickups.md)
  Generated player-facing pickup reference grouped by category
- [`docs/history/`](./docs/history/)
  Version history snapshots
- [`docs/notes/cmd.md`](./docs/notes/cmd.md)
  Command-related working notes
- [`docs/architecture/research/project-scope.md`](./docs/architecture/research/project-scope.md)
  Scope and constraints
- [`docs/architecture/research/implementation-guidance.md`](./docs/architecture/research/implementation-guidance.md)
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
