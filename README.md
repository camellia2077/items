# RandomLoadout

`RandomLoadout` is an `Enter the Gungeon` mod built on `BepInEx`.
It currently combines:

- automatic run-start loadout grant
- in-game command/debug UI
- ETG runtime integration work such as Boss Rush and character-select-hub utilities

## Read This First

Do not start from source files unless you already know the ETG runtime surface.

- [Start Here](./docs/getting-started/start-here.md)
- [Docs Index](./docs/README.md)
- [Source Guide](./src/AGENTS.md)

## Quick Start

- Build:
  `python .\tools\build\build.py --configuration Debug`
- Test:
  `python .\tools\build\test.py --configuration Debug`
- Naming check:
  `python .\tools\devtools\check_naming.py --verbose`
- Deploy:
  `python .\tools\deploy\deploy_mod.py "<game path>" --configuration Release --overwrite-config`

## Documentation Map

- New contributor / agent handoff:
  [docs/getting-started/start-here.md](./docs/getting-started/start-here.md)
- Runtime terminology:
  [docs/reference/terminology.md](./docs/reference/terminology.md)
- High-risk ETG runtime areas:
  [docs/architecture/runtime-hotspots.md](./docs/architecture/runtime-hotspots.md)
- Build / deploy / logs:
  [docs/getting-started/development-setup.md](./docs/getting-started/development-setup.md)
  [docs/operations/deploy.md](./docs/operations/deploy.md)
  [docs/operations/logging.md](./docs/operations/logging.md)
- Testing expectations:
  [docs/reference/testing-matrix.md](./docs/reference/testing-matrix.md)

## Repository Layout

- `src/RandomLoadout/`
  Unity / ETG runtime integration
- `src/RandomLoadout.Core/`
  pure selection and config logic
- `tests/RandomLoadout.Core.Tests/`
  automated tests for core behavior
- `docs/`
  project knowledge base
- `tools/`
  build, deploy, logs, release, and dev utilities
- `defaults/`
  repository-shipped config and catalog baselines
- `lib/`
  local dependency drop folder

## Dependencies

Direct open-source runtime dependencies:

- [`BepInEx`](https://github.com/BepInEx/BepInEx)
- [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI)
- [`HarmonyX`](https://github.com/BepInEx/HarmonyX)

Implementation references:

- [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
- [`Nevernamed22/OnceMoreIntoTheBreach`](https://github.com/Nevernamed22/OnceMoreIntoTheBreach)
