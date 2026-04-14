from __future__ import annotations

import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import get_repo_root, run_cli, sync_generated_version_files


def main() -> int:
    repo_root = get_repo_root()
    sync_generated_version_files(repo_root)
    print("Synchronized generated version files from '{0}'.".format(repo_root / "VERSION"))
    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
