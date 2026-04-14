# Development Setup

Use this page when you need the local build, test, dependency, or day-to-day development workflow.

If you are new to the project, start with [Start Here](./start-here.md) first.

## Must Read First

Before touching ETG runtime code, read:

1. [Start Here](./start-here.md)
2. [Terminology And Naming](../reference/terminology.md)
3. [Testing Matrix](../reference/testing-matrix.md)
4. [Tools README](../../tools/README.md)

## 30-Second Commands

Build debug:

```powershell
python .\tools\build\build.py --configuration Debug
```

Build release:

```powershell
python .\tools\build\build.py --configuration Release
```

Run automated tests:

```powershell
python .\tools\build\test.py --configuration Debug
```

Run naming check:

```powershell
python .\tools\devtools\check_naming.py --verbose
```

Deploy release to game:

```powershell
python .\tools\deploy\deploy_mod.py "<game path>" --configuration Release --overwrite-config
```

## Build And Test Expectations

Use [Testing Matrix](../reference/testing-matrix.md) to decide which checks are required for your change.

At minimum:

- C# or `.csproj` change:
  build Debug and Release
- `RandomLoadout.Core` logic change:
  run automated tests
- ETG runtime change:
  build, then run smoke checks and review logs

## Smoke And Runtime Validation

After changing runtime hooks, deployment logic, Boss Rush flow, character-select-hub flow, or scene transitions, also run:

- [Smoke Checklist](../operations/smoke-checklist.md)
- [Logging](../operations/logging.md)

## Local Tooling

This repository uses Python tooling under [`tools/`](../../tools/).

Relevant entrypoints:

- `tools/build/`
- `tools/deploy/`
- `tools/logs/`
- `tools/devtools/`

For the quick command map, read:

- [Tools README](../../tools/README.md)

## Local Dependencies

Required local DLLs and dependency-drop expectations are documented in:

- [lib/README.md](../../lib/README.md)

## IDE

The repository includes:

- `RandomLoadout.sln`

Use it for IDE navigation if helpful, but keep the documented Python command paths as the source of truth for build/test workflow.

## Read Next

- Deploy workflow:
  [../operations/deploy.md](../operations/deploy.md)
- Log workflow:
  [../operations/logging.md](../operations/logging.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
