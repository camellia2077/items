from __future__ import annotations

import argparse
import sys
from pathlib import Path

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from release_package_builder import build_release_package
from release_package_upstream import load_metadata, sha256_for_file
from tool_common import get_repo_root, run_cli


DEFAULT_CONFIGURATION = "Release"
METADATA_PATH = Path("tools") / "release" / "release_package_metadata.json"
CACHE_DIRECTORY = Path(".cache") / "release"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a player-facing RandomLoadout release zip with a bundled BepInExPack_EtG."
    )
    parser.add_argument(
        "-c",
        "--configuration",
        default=DEFAULT_CONFIGURATION,
        choices=("Debug", "Release"),
        help="Build configuration to package. Defaults to Release.",
    )
    parser.add_argument(
        "--version",
        default="",
        help="Override the mod version used in the output file name. Defaults to AssemblyInfo.cs.",
    )
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Skip the pre-package build step. By default packaging builds the selected configuration first.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = get_repo_root()
    metadata = load_metadata(repo_root / METADATA_PATH)
    output_path = build_release_package(
        repo_root=repo_root,
        metadata=metadata,
        configuration=args.configuration,
        version=args.version,
        skip_build=args.skip_build,
        cache_directory=repo_root / CACHE_DIRECTORY,
    )

    print("Built release package: {0}".format(output_path))
    print("Package SHA-256: {0}".format(sha256_for_file(output_path)))
    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
