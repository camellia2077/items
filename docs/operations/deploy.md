# Deploy

Use this page when you need to copy the built mod bundle into an `Enter the Gungeon` installation.

If you are new to the project, read [Start Here](../getting-started/start-here.md) first.

## Must Read First

Before deploying runtime changes, read:

1. [Development Setup](../getting-started/development-setup.md)
2. [Testing Matrix](../reference/testing-matrix.md)
3. [Tools README](../../tools/README.md)

If the change touches ETG runtime flow, also read:

4. [Runtime Hotspots](../architecture/runtime-hotspots.md)
5. [Logging](./logging.md)

## 30-Second Commands

Deploy release and overwrite repo-shipped config defaults:

```powershell
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config
```

Deploy release without overwriting existing game-side config:

```powershell
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release
```

Deploy an already-built DLL without triggering a build:

```powershell
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --skip-build
```

## Recommended Flow

For normal runtime work:

1. build
2. deploy
3. run smoke checks
4. review logs

Typical sequence:

```powershell
python .\tools\build\build.py --configuration Release
python .\tools\deploy\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config
```

Then continue with:

- [Smoke Checklist](./smoke-checklist.md)
- [Logging](./logging.md)

## What The Deploy Script Copies

`deploy_mod.py` builds first by default, then copies:

- `RandomLoadout.dll`
- the MTG API runtime bundle from `lib\`
- repository default config files and catalog snapshots when requested

Current MTG API runtime bundle copy targets:

- `0Harmony.dll` -> `BepInEx\plugins\`
- `ModTheGungeonAPI.dll` -> `BepInEx\plugins\MtGAPI\`
- `Ionic.Zip.dll` -> `BepInEx\plugins\MtGAPI\`
- `Newtonsoft.Json.dll` -> `BepInEx\plugins\MtGAPI\`
- `System.Xml.dll` -> `BepInEx\plugins\MtGAPI\`
- `System.Xml.Linq.dll` -> `BepInEx\plugins\MtGAPI\`
- `UnityEngine.CoreModule.MTGAPIPatcher.mm.dll` -> `monomod\`

Keep these DLLs present under `lib\` before deploying.

## Config And Catalog Notes

Game-side config lives under `BepInEx\config\`.

Important files:

- `randomgun.randomloadout.cfg`
- `RandomLoadout.aliases.json5`
- `RandomLoadout.rules.json5`
- `RandomLoadout.pickups.txt`
- `RandomLoadout.pickups.json`
- `RandomLoadout.pickups.by-category.json`
- `RandomLoadout.rules.full-pool.json5`

Default behavior:

- existing config files are preserved unless `--overwrite-config` is used
- repository copies under `defaults/` are read-only baselines
- the plugin exports fresh runtime catalog files into the game directory after startup

Fallback behavior:

- missing or invalid `RandomLoadout.rules.json5` falls back to `RandomLoadout.rules.full-pool.json5`
- if both rule files are unavailable, the plugin falls back to built-in emergency defaults
- missing `RandomLoadout.aliases.json5` falls back to built-in aliases for that session

## Common Failure Cases

- deployment fails while the game is open:
  close the game and try again
- deploy works but runtime still fails:
  review [Logging](./logging.md)
- DLL copy succeeds but gameplay is broken:
  continue with [Smoke Checklist](./smoke-checklist.md)

## Read Next

- Smoke workflow:
  [./smoke-checklist.md](./smoke-checklist.md)
- Log workflow:
  [./logging.md](./logging.md)
- Build workflow:
  [../getting-started/development-setup.md](../getting-started/development-setup.md)
