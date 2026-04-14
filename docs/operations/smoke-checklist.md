# Smoke Checklist

Use this page for the minimum manual gameplay validation after ETG runtime or deployment changes.

If a runtime-facing change was made, do not stop at build success.

## Must Read First

Before running gameplay validation, read:

1. [Testing Matrix](../reference/testing-matrix.md)
2. [Logging](./logging.md)
3. [Runtime Hotspots](../architecture/runtime-hotspots.md)

## 30-Second Flow

Build and deploy:

```powershell
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config
```

Review targeted logs if needed:

```powershell
python .\tools\logs\read_log.py --preset bossrush --preset error --tail 200 --dedupe-consecutive --ignore-case
```

## When To Run This

Run this checklist after:

- changing Harmony patches
- changing Boss Rush lifecycle or dungeon-load behavior
- changing deployment packaging for runtime DLL dependencies
- updating `ModTheGungeonAPI` or other runtime libraries under `lib\`
- changing character-select-hub, reward, pause, or return-flow behavior

## Minimum Smoke Pass

1. Build and deploy the `Release` bundle.

```powershell
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config
```

2. Launch the game and confirm startup succeeds.

Expected:

- plugin startup completes
- no dependency failure
- no hook-signature failure

Healthy log examples:

- `[RandomLoadout][Init] Boss Rush startup self-check complete. Applied hooks=..., Skipped hooks=...`
- `[RandomLoadout][Init] Boss Rush startup self-check passed.`
- `[RandomLoadout][Init] RandomLoadout v... started successfully.`

3. Stay in the character-select hub and press `F7`.

Expected:

- the UI opens
- the `Boss Rush` entry is visible
- no new `Error`, `Exception`, or hook-failure lines appear

4. Start a normal run without Boss Rush.

Expected:

- the game leaves the character-select hub normally
- dungeon loading completes
- automatic random loadout still works outside Boss Rush

5. Return to the character-select hub (`tt_foyer`, commonly called the Breach) and start Boss Rush from `F7`.

Expected:

- Boss Rush starts only from the character-select hub
- `tt_castle` loads
- the player is routed toward the generated boss encounter
- the boss intro still plays

6. Clear the first boss and claim the reward.

Expected:

- reward spawn remains vanilla
- the next floor does not load until the reward is claimed
- after the claim, the mode transitions to `tt5`

7. Abort or fail the run.

Expected:

- manual abort returns to character select
- death during Boss Rush returns to character select
- starting Boss Rush again begins from `Keep`

## Fast Triage

If something fails, pull startup and runtime lines first:

```powershell
python .\tools\logs\read_log.py --preset init --preset run --preset bossrush --preset error --tail 300 --dedupe-consecutive --ignore-case
```

If startup fails, stop there and fix startup before continuing gameplay checks.

## Read Next

- Logging:
  [./logging.md](./logging.md)
- Deploy workflow:
  [./deploy.md](./deploy.md)
- Full validation guidance:
  [../reference/testing-matrix.md](../reference/testing-matrix.md)
