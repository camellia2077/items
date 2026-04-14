# Testing Matrix

Use this page to decide what must run after a change.

## Static Checks

Run for any runtime, terminology, or file-structure change:

- `python .\tools\devtools\check_naming.py --verbose`

Purpose:

- catches repository-specific naming regressions
- protects ETG terminology alignment

## Build Checks

Run for any C# or project-file change:

- `python .\tools\build\build.py --configuration Debug`
- `python .\tools\build\build.py --configuration Release`

Purpose:

- verifies compile-time correctness
- verifies `.csproj` include changes

## Automated Tests

Run for any core logic, parser, rule, or config-resolution change:

- `python .\tools\build\test.py --configuration Debug`

Purpose:

- validates `RandomLoadout.Core`
- protects parser and selection behavior

Limits:

- does not validate ETG runtime hooks
- does not validate scene transitions or Boss Rush gameplay

## Manual Smoke Checks

Run after any ETG runtime integration change:

- follow [Smoke Checklist](../operations/smoke-checklist.md)

Minimum smoke set for runtime changes:

- game boots
- `F7` panel opens
- normal dungeon entry still works
- automatic loadout still behaves as expected outside Boss Rush

## Manual Boss Rush Checks

Run after any Boss Rush, hook, reward, pause, or return-flow change:

- start Boss Rush from character-select hub
- verify floor handoff
- verify movement and combat
- defeat boss and claim reward
- verify next-floor transition
- verify pause-menu return path
- verify death return path

## Log Review

Run after any hook, scene, or ETG runtime change:

- inspect `BepInEx\LogOutput.log`
- use [Logging](../operations/logging.md) and `tools/logs/read_log.py`

Must check for:

- hook install failures
- null references during scene transition
- Boss Rush status logs
- unexpected startup warnings after your change

## Manual-Only Areas

These cannot be trusted to automated tests alone:

- Boss Rush floor-to-floor flow
- character-select-hub switching
- pause-menu interception
- game-over interception
- ETG scene transition timing
- vanilla intro / reward / camera interactions

## Recommended Post-Change Matrix

Core-only change:

- naming check
- build
- automated tests

Runtime change outside Boss Rush:

- naming check
- build
- smoke checklist
- log review

Boss Rush or hook change:

- naming check
- build
- smoke checklist
- Boss Rush manual checks
- log review
