from __future__ import annotations

import argparse
import hashlib
import shutil
from pathlib import Path
from script_common import (
    add_configuration_argument,
    fail,
    get_default_sync_paths,
    get_plugin_output_path,
    get_repo_root,
    require_existing_directory,
    run_cli,
)


def sha256_for_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as file_obj:
        for chunk in iter(lambda: file_obj.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Copy the built mod DLL into an Enter the Gungeon BepInEx plugins folder."
    )
    parser.add_argument(
        "game_path",
        help="Path to the Enter the Gungeon installation directory.",
    )
    add_configuration_argument(parser, "Build configuration to deploy. Defaults to Debug.")
    parser.add_argument(
        "--overwrite-config",
        action="store_true",
        help="Overwrite existing files in BepInEx\\config with the repository defaults and catalog snapshots.",
    )
    return parser.parse_args()


def copy_default_files(repo_root: Path, config_dir: Path, overwrite: bool) -> int:
    default_paths = get_default_sync_paths(repo_root)
    for default_path in default_paths:
        if not default_path.is_file():
            return fail("Repository default file not found: {0}".format(default_path))

    copied_count = 0
    skipped_count = 0
    for default_path in default_paths:
        target_path = config_dir / default_path.name
        target_exists = target_path.exists()
        if target_exists and not overwrite:
            print("Kept existing default target: {0}".format(target_path))
            skipped_count += 1
            continue

        shutil.copy2(default_path, target_path)
        action = "Overwrote" if target_exists else "Copied"
        print("{0} repository default: {1} -> {2}".format(action, default_path, target_path))
        copied_count += 1

    print("Default file sync complete: {0} copied, {1} kept.".format(copied_count, skipped_count))
    return 0


def main() -> int:
    args = parse_args()

    repo_root = get_repo_root()
    source_dll = get_plugin_output_path(repo_root, args.configuration)

    if not source_dll.is_file():
        return fail(
            "Build output not found: {0}\nRun the build first, for example: .\\scripts\\build.ps1".format(
                source_dll
            )
        )

    game_path = require_existing_directory(Path(args.game_path).expanduser(), "Game path")

    plugins_dir = game_path / "BepInEx" / "plugins"
    config_dir = game_path / "BepInEx" / "config"
    plugins_dir.mkdir(parents=True, exist_ok=True)
    config_dir.mkdir(parents=True, exist_ok=True)

    target_dll = plugins_dir / source_dll.name
    shutil.copy2(source_dll, target_dll)

    source_hash = sha256_for_file(source_dll)
    target_hash = sha256_for_file(target_dll)
    if source_hash != target_hash:
        return fail(
            "Deploy verification failed.\nSource: {0} ({1})\nTarget: {2} ({3})".format(
                source_dll, source_hash, target_dll, target_hash
            )
        )

    print(f"Copied {source_dll} -> {target_dll}")
    print(f"Verified SHA-256: {target_hash}")
    return copy_default_files(repo_root, config_dir, args.overwrite_config)


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
