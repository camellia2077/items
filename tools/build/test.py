from __future__ import annotations

import argparse
import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import (
    add_configuration_argument,
    fail,
    get_repo_root,
    get_test_output_path,
    get_test_project_path,
    resolve_msbuild,
    run_cli,
    run_process,
    sync_generated_version_files,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build and run the RandomLoadout core test executable."
    )
    add_configuration_argument(parser, "Build configuration. Defaults to Debug.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = get_repo_root()
    project_path = get_test_project_path(repo_root)
    test_exe_path = get_test_output_path(repo_root, args.configuration)

    sync_generated_version_files(repo_root)
    msbuild_path = resolve_msbuild()

    build_result = run_process(
        [str(msbuild_path), str(project_path), "/p:Configuration={0}".format(args.configuration)],
        repo_root,
    )
    if build_result != 0:
        return build_result

    if not test_exe_path.is_file():
        return fail("Test executable not found at '{0}'.".format(test_exe_path))

    return run_process([str(test_exe_path)], repo_root)


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
