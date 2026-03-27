from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
from script_common import fail, run_cli


MESSAGE_PREFIX = "[RandomLoadout]"
PLUGIN_SOURCE_PATTERN = re.compile(r"^\[[^\]]*:\s*RandomLoadout\]")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Extract RandomLoadout-owned lines from a BepInEx log file."
    )
    parser.add_argument(
        "log_path",
        help="Path to the BepInEx log file to scan.",
    )
    parser.add_argument(
        "-o",
        "--output",
        help="Optional path to write the filtered lines.",
    )
    parser.add_argument(
        "--include-unprefixed-plugin-lines",
        action="store_true",
        help="Also include lines whose BepInEx source is RandomLoadout even if the message prefix is missing.",
    )
    return parser.parse_args()


def is_randomloadout_line(line: str, include_unprefixed_plugin_lines: bool) -> bool:
    if MESSAGE_PREFIX in line:
        return True

    return include_unprefixed_plugin_lines and PLUGIN_SOURCE_PATTERN.search(line) is not None


def main() -> int:
    args = parse_args()

    log_path = Path(args.log_path).expanduser()
    if not log_path.is_file():
        return fail("Log file not found: {0}".format(log_path))

    lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    matched_lines = [
        line
        for line in lines
        if is_randomloadout_line(line, args.include_unprefixed_plugin_lines)
    ]

    output_text = "\n".join(matched_lines)
    if matched_lines:
        output_text += "\n"

    if args.output:
        output_path = Path(args.output).expanduser()
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(output_text, encoding="utf-8")
        print(f"Wrote {len(matched_lines)} RandomLoadout log lines to {output_path}")
    else:
        sys.stdout.write(output_text)
        print(
            f"Matched {len(matched_lines)} RandomLoadout log lines from {log_path}",
            file=sys.stderr,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
