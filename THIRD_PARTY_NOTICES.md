# Third-Party Notices

This repository and its release process use, depend on, or reference third-party open-source projects.

This file separates:

* components redistributed in the player-facing release package
* runtime dependencies required by `RandomLoadout` but not bundled in the player-facing release package
* reference projects that informed implementation details but are not redistributed with this repository's release package

## Redistributed In The Player-Facing Release Package

The release package built by `tools/release/build_release_package.py` redistributes:

* `BepInExPack_EtG`
* its bundled open-source components such as `BepInEx`, `HarmonyX`, `MonoMod`, `BepInEx.MonoMod.Loader`, and `UnityDoorstop`

For redistributed package details, license texts, and upstream links, see:

* `docs/operations/release-package.md`
* `tools/release/release_package_metadata.json`
* the generated release-package `THIRD_PARTY_NOTICES.md`
* the generated release-package `licenses/` directory

The player-facing release package does not redistribute game-owned files such as `Assembly-CSharp.dll` or `UnityEngine*.dll`.

## Required Runtime Dependency Not Bundled In The Player-Facing Release Package

`RandomLoadout` requires:

* [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI) - `MIT`

Current usage:

* runtime integration dependency for `Boss Rush`
* `HarmonyX`-based ETG hook infrastructure
* level-load and character-select flow references such as `LoadCustomLevel(...)`, `OnDepartedFoyer()`, and `DelayedLoadCharacterSelect(...)`

Important note:

* local developer deploy flows may copy `ModTheGungeonAPI.dll` and related support files into a test install
* the player-facing release package described in `docs/operations/release-package.md` does not currently bundle `ModTheGungeonAPI`

License source:

* [`ModTheGungeonAPI/LICENSE`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/LICENSE)

## Reference Projects And Implementation Inspiration

These projects informed implementation details, naming, flow choices, or runtime debugging strategy.
They are acknowledged here for transparency and credit, but are not redistributed as part of the player-facing release package unless explicitly stated elsewhere.

* [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
  * used as a reference when reasoning about save flags and persistence-sensitive ETG behavior
* [`Nevernamed22/OnceMoreIntoTheBreach`](https://github.com/Nevernamed22/OnceMoreIntoTheBreach)
  * used as a reference for ETG room-name conventions such as `Boss Foyer`, runtime teleport patterns, and other community runtime practices

## Distribution Rules Followed By This Project

When publishing `RandomLoadout`, the project intends to follow these rules:

* preserve the original license text for redistributed third-party components
* separate redistributed dependencies from reference-only acknowledgements
* avoid redistributing game-owned DLLs or assets
* keep upstream project names, homepage links, and license identifiers visible in release documentation
* retain this notice and the repository `LICENSE` when redistributing the project source
