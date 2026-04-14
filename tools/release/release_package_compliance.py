from __future__ import annotations

import shutil
from pathlib import Path

from release_package_upstream import download_text


FORBIDDEN_GAME_DLLS = {
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
}


def stage_license_files(repo_root: Path, metadata: dict, staging_root: Path) -> list[dict]:
    licenses_directory = staging_root / "licenses"
    licenses_directory.mkdir(parents=True, exist_ok=True)

    bundled_components = metadata.get("bundledComponents", [])
    staged_components = []
    for component in bundled_components:
        license_text = download_text(component["licenseUrl"]).strip()
        if not license_text:
            raise OSError("License text for '{0}' was empty.".format(component["name"]))

        output_name = component["licenseOutputName"]
        output_path = licenses_directory / output_name
        output_path.write_text(license_text + "\n", encoding="utf-8")
        staged_components.append(component)

    randomloadout_license_source = repo_root / "LICENSE"
    if not randomloadout_license_source.is_file():
        raise FileNotFoundError("Repository license file not found: {0}".format(randomloadout_license_source))

    shutil.copyfile(
        str(randomloadout_license_source),
        str(licenses_directory / "RandomLoadout-LICENSE.txt"),
    )

    return staged_components


def write_install_readme(version_tag: str, staging_root: Path) -> None:
    install_text = """RandomLoadout {0} Install Guide

1. Close `Enter the Gungeon`.
2. Open this archive.
3. Extract all files into the game root directory that contains `Enter the Gungeon.exe`.
4. Allow overwrite if Windows asks.
5. Launch the game. `BepInEx` and `RandomLoadout` should now be installed.

Uninstall:
- Remove `BepInEx\\plugins\\RandomLoadout.dll`
- Remove `BepInEx\\config\\randomgun.randomloadout.cfg`
- Remove `BepInEx\\config\\RandomLoadout.aliases.json5`
- Remove `BepInEx\\config\\RandomLoadout.rules.json5`
- Optionally remove the generated `RandomLoadout.pickups*.json` and `RandomLoadout.rules.full-pool.json5` files from `BepInEx\\config\\`
- If you installed `BepInEx` only for this mod, you may also remove the `BepInEx` folder and the loader files that came from `BepInExPack_EtG`
""".format(version_tag)

    (staging_root / "README-INSTALL.txt").write_text(install_text, encoding="utf-8")


def write_third_party_notices(
    version_tag: str,
    metadata: dict,
    staged_components: list[dict],
    staging_root: Path,
) -> None:
    upstream_package = metadata["upstreamPackage"]
    lines = [
        "# Third-Party Notices",
        "",
        "This release package contains `RandomLoadout {0}` together with an unmodified redistribution of `{1} {2}`.".format(
            version_tag, upstream_package["name"], upstream_package["version"]
        ),
        "",
        "## RandomLoadout",
        "",
        "- Project: `RandomLoadout`",
        "- Homepage: <https://github.com/camellia2077/items>",
        "- License: `MIT`",
        "- Bundled license file: `licenses/RandomLoadout-LICENSE.txt`",
        "",
        "## Redistributed Upstream Package",
        "",
        "- Package: `{0}`".format(upstream_package["name"]),
        "- Version: `{0}`".format(upstream_package["version"]),
        "- Official package page: <{0}>".format(upstream_package["packagePageUrl"]),
        "- Official download URL: <{0}>".format(upstream_package["downloadUrl"]),
        "- Upstream project homepage: <{0}>".format(upstream_package["projectHomepageUrl"]),
        "- Package license: `{0}`".format(upstream_package["licenseId"]),
        "- Redistribution note: this release package redistributes the upstream package unmodified and preserves its separate license terms.",
        "",
        "## Bundled Open Source Components From The Upstream Package",
        "",
    ]

    for component in staged_components:
        lines.extend(
            [
                "- `{0}`".format(component["name"]),
                "  - Homepage: <{0}>".format(component["homepageUrl"]),
                "  - License: `{0}`".format(component["licenseId"]),
                "  - Bundled license file: `licenses/{0}`".format(component["licenseOutputName"]),
            ]
        )

    lines.extend(
        [
            "",
            "## Distribution Notes",
            "",
            "- This package does not include `Enter the Gungeon` game files.",
            "- This package does not include development-only DLLs from the repository `lib/` folder such as `Assembly-CSharp.dll` or `UnityEngine*.dll`.",
            "- Users remain subject to the original licenses and notices of the bundled upstream components.",
            "",
        ]
    )

    (staging_root / "THIRD_PARTY_NOTICES.md").write_text("\n".join(lines), encoding="utf-8")


def ensure_no_game_owned_dlls(staging_root: Path) -> None:
    for path in staging_root.rglob("*.dll"):
        name = path.name
        if name in FORBIDDEN_GAME_DLLS or (name.startswith("UnityEngine") and name.endswith(".dll")):
            raise OSError("Forbidden game-owned DLL found in release package staging area: {0}".format(path))


def ensure_required_package_files(staging_root: Path) -> None:
    required_paths = (
        staging_root / "BepInEx" / "plugins" / "RandomLoadout.dll",
        staging_root / "BepInEx" / "config" / "RandomLoadout.rules.json5",
        staging_root / "BepInEx" / "config" / "RandomLoadout.aliases.json5",
        staging_root / "THIRD_PARTY_NOTICES.md",
        staging_root / "README-INSTALL.txt",
        staging_root / "licenses" / "RandomLoadout-LICENSE.txt",
        staging_root / "licenses" / "BepInEx-LICENSE.txt",
    )

    for required_path in required_paths:
        if not required_path.is_file():
            raise FileNotFoundError("Required packaged file was missing: {0}".format(required_path))
