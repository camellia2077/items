# Deploy

This page covers copying the built plugin into an `Enter the Gungeon` installation.

## Deploy Script

Debug build:

```powershell
python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon"
```

Release build:

```powershell
python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release
```

Force refresh the repository default config files too:

```powershell
python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config
```

## Recommended Flow

Build first:

```powershell
.\scripts\build.ps1 -Configuration Release
```

Then deploy:

```powershell
python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release
```

## Notes

- `deploy_mod.py` copies an already-built DLL and syncs the repository default config files
- the repository also ships baseline pickup catalog snapshots and a generated full random rule pool template under `defaults/catalog/`
- if deployment fails while the game is open, close the game and try again
- the script verifies the copied DLL by SHA-256 after copy
- runtime config files are created under `BepInEx\config\`
- the minimum configuration split is:
  - `randomgun.randomloadout.cfg` for simple switches such as enabling or disabling automatic start-of-run grants
  - `RandomLoadout.aliases.json` for shared alias-to-pickup ID mappings
  - `RandomLoadout.rules.json` for loadout rules
  - `RandomLoadout.pickups.txt` for the exported pickup name and ID index
  - `RandomLoadout.pickups.json` for the flat machine-readable pickup catalog snapshot
  - `RandomLoadout.pickups.by-category.json` for the grouped machine-readable pickup catalog snapshot
  - `RandomLoadout.rules.full-pool.json` for an auto-generated full random pool template
- by default, existing config files are preserved
- pass `--overwrite-config` if you want the repository defaults and catalog snapshots to replace files already in the game directory
- if `RandomLoadout.rules.json` is missing at runtime, the plugin now falls back to `RandomLoadout.rules.full-pool.json` in the same `BepInEx\config\` directory
- if `RandomLoadout.rules.json` exists but cannot be parsed, the plugin also falls back to `RandomLoadout.rules.full-pool.json`
- if both rule files are unavailable, the plugin falls back to the built-in emergency default rules for that session
- if `RandomLoadout.aliases.json` is missing at runtime, the plugin logs a warning and falls back to built-in default aliases for that session
- the repository copies under `defaults/` are read-only baselines; the plugin never writes back into the repository
- after the game starts successfully, the plugin exports fresh runtime versions of the pickup catalog files into `BepInEx\config\` and those runtime-generated files overwrite the game-side copies there
- this means the effective priority is:
  - `RandomLoadout.rules.json` for explicit user-authored active rules
  - `RandomLoadout.rules.full-pool.json` as the full-random fallback when the primary rules file is missing
  - runtime-generated files in the game directory
  - repository baseline files copied by `deploy_mod.py`
  - missing-file fallback behavior inside the plugin
