from __future__ import annotations

import argparse
import hashlib
import shutil
from pathlib import Path
from script_common import (
    add_configuration_argument,
    fail,
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
    return parser.parse_args()


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
    plugins_dir.mkdir(parents=True, exist_ok=True)

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
    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
