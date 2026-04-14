# Deploy Tools

This folder contains local game-install deployment tooling.

Files:

- `deploy_mod.py`: builds and copies the mod plus required runtime dependencies into an `Enter the Gungeon` install

Typical usage:

- `python .\tools\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release --overwrite-config`
