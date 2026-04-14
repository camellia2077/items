# Release Package

This page covers building a player-facing release zip for `RandomLoadout`.

## Goal

The release package is a standard directory overlay zip for `Enter the Gungeon`.

Users should be able to:

* close the game
* extract the zip into the game root
* allow overwrite
* launch the game without needing extra modding tools

## Build Script

Default release package:

```powershell
python .\tools\build_release_package.py
```

Explicit release configuration:

```powershell
python .\tools\build_release_package.py --configuration Release
```

Override the package version label:

```powershell
python .\tools\release\build_release_package.py --version 0.2.3
```

Skip the build step and package the current compiled DLL:

```powershell
python .\tools\build_release_package.py --skip-build
```

## Output

The script writes a zip under `dist/`:

* `dist/RandomLoadout-v<version>-ETG.zip`

The package root is the game root overlay layout, not a repository layout.

## What The Package Contains

The generated zip includes:

* the official `BepInExPack_EtG` package, redistributed unmodified
* `BepInEx\plugins\RandomLoadout.dll`
* repository default config and catalog files in `BepInEx\config\`
* `README-INSTALL.txt`
* `THIRD_PARTY_NOTICES.md`
* `licenses\`

## What The Package Does Not Contain

The generated zip does not include:

* repository build helpers
* repository documentation
* test outputs
* local development DLLs from `lib\`
* game-owned files such as `Assembly-CSharp.dll` or `UnityEngine*.dll`

## Upstream Source And Verification

The packaging script uses pinned metadata from:

* `tools/release_package_metadata.json`

That metadata defines:

* the official upstream download URL for `BepInExPack_EtG`
* the pinned upstream version
* the expected `SHA-256`
* the upstream homepage and license identifiers

Packaging fails if:

* the upstream archive hash does not match the pinned `SHA-256`
* the upstream license text cannot be fetched and staged
* forbidden game-owned DLLs appear in the package staging area

Downloaded upstream files are cached under:

* `.cache/release/`

## License And Attribution Compliance

The release package is designed to redistribute `BepInExPack_EtG` compliantly.

It stages:

* `licenses/RandomLoadout-LICENSE.txt`
* `licenses/BepInEx-LICENSE.txt`
* additional bundled component license files when available
* `THIRD_PARTY_NOTICES.md`

`THIRD_PARTY_NOTICES.md` records:

* the upstream package name and version
* the official package page and download URL
* the upstream project homepage
* the bundled open source components and their license files

## Notes

* This workflow is for player-facing release zips.
* Use `tools/deploy_mod.py` for local developer deployment into an existing game install.
