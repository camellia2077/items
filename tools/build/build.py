from __future__ import annotations

import argparse
import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import (
    add_configuration_argument,
    ensure_required_build_dlls,
    get_plugin_project_path,
    get_repo_root,
    resolve_msbuild,
    run_cli,
    run_process,
    sync_generated_version_files,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build the RandomLoadout plugin with .NET Framework MSBuild."
    )
    add_configuration_argument(parser, "Build configuration. Defaults to Debug.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = get_repo_root()
    project_path = get_plugin_project_path(repo_root)

    ensure_required_build_dlls(repo_root)
    sync_generated_version_files(repo_root)
    msbuild_path = resolve_msbuild()

    return run_process(
        [str(msbuild_path), str(project_path), "/p:Configuration={0}".format(args.configuration)],
        repo_root,
    )


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
