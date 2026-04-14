# Runtime Hotspots

These are the highest-risk ETG runtime areas in this repository.

Treat them as "slow down and verify" zones.

## Boss Rush

Files:

- `src/RandomLoadout/Runtime/BossRush*.cs`
- `src/RandomLoadout/Commands/InGameCommandController.BossRush.cs`
- `src/RandomLoadout/Runtime/BossRushHooks.cs`

Risk:

- floor loading
- boss-room handoff timing
- reward progression timing
- return-to-character-select flow
- interaction with vanilla cutscenes and pause menu flow

Must verify:

- start from character-select hub only
- movement and combat after floor handoff
- reward claim to next-floor transition
- exit / death / completion return path
- log output after any hook or flow change

## Character-Select-Hub Switching

Files:

- `src/RandomLoadout/Commands/FoyerCharacterSwitchService*.cs`
- `src/RandomLoadout/Commands/InGameCommandController.Character*.cs`

Risk:

- ETG `Foyer` callbacks are sensitive to side effects
- native callbacks may deduct currency or depend on scene objects
- prefab replacement can desync camera, input, or selected character state

Must verify:

- switch-only flow
- unlock flow
- hidden character handling
- no unintended currency side effects

## Scene Transition And Run Lifecycle

Files:

- `src/RandomLoadout/Plugin.RunLifecycle.cs`
- `src/RandomLoadout/Runtime/RunLifecycle*.cs`
- `src/RandomLoadout/Runtime/RunSceneWatcher.cs`

Risk:

- scene token vs gameplay-state confusion
- loadout grant firing at the wrong time
- PrimaryPlayer replacement during scene changes

Must verify:

- grant suppression during Boss Rush
- reset behavior when entering character-select hub
- no early grant during loading scenes

## Reward Hooks

Files:

- `src/RandomLoadout/Runtime/BossRushHooks.cs`

Risk:

- boss clear timing
- reward pickup timing
- false positives from non-boss rewards

Must verify:

- reward spawn detected only at the intended time
- next-floor load waits until reward is actually claimed
- normal runs still behave normally

## Pause Menu And Game Over Hooks

Files:

- `src/RandomLoadout/Runtime/BossRushHooks.cs`
- `src/RandomLoadout/Runtime/BossRushService*.cs`

Risk:

- wrong hook signature causes startup failure
- wrong return path can soft-lock on load
- pause/game-over interception can bypass vanilla cleanup

Must verify:

- Harmony target signature before editing
- return-to-character-select flow after exit
- death flow after hook changes

## General Rule

If a change touches a hotspot:

1. Read [Terminology And Naming](../reference/terminology.md).
2. Prefer vanilla flow over custom hard transitions.
3. Check hook signatures before editing.
4. Re-read the runtime log after testing.
