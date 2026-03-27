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

- `deploy_mod.py` only copies an already-built DLL
- if deployment fails while the game is open, close the game and try again
- the script verifies the copied DLL by SHA-256 after copy
