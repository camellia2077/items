from __future__ import annotations

import argparse
import re
import sys
from collections import deque
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import fail, run_cli


DEFAULT_LOG_PATH = Path(
    r"C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log"
)

PRESET_PATTERNS = {
    "randomloadout": r"\[RandomLoadout\]",
    "bossrush": r"\[RandomLoadout\]\[BossRush\]",
    "run": r"\[RandomLoadout\]\[Run\]",
    "command": r"\[RandomLoadout\]\[Command\]",
    "init": r"\[RandomLoadout\]\[Init\]",
    "error": r"Error|Exception|NullReference|HarmonyX|Could not load|missing dependencies|failed to patch",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Read a BepInEx log file and print filtered, optionally de-duplicated lines."
    )
    parser.add_argument(
        "log_path",
        nargs="?",
        default=str(DEFAULT_LOG_PATH),
        help="Path to the BepInEx log file. Defaults to the local ETG install log path.",
    )
    parser.add_argument(
        "--preset",
        action="append",
        choices=sorted(PRESET_PATTERNS.keys()),
        help="Add a built-in filter preset. Can be passed multiple times.",
    )
    parser.add_argument(
        "--pattern",
        action="append",
        help="Add a custom regex pattern. Can be passed multiple times.",
    )
    parser.add_argument(
        "--tail",
        type=int,
        default=0,
        help="Read only the last N lines from the log before filtering.",
    )
    parser.add_argument(
        "--dedupe-consecutive",
        action="store_true",
        help="Collapse consecutive identical output lines.",
    )
    parser.add_argument(
        "--ignore-case",
        action="store_true",
        help="Match patterns case-insensitively.",
    )
    parser.add_argument(
        "-o",
        "--output",
        help="Optional path to write the filtered output.",
    )
    return parser.parse_args()


def build_patterns(args: argparse.Namespace) -> list[re.Pattern[str]]:
    raw_patterns: list[str] = []
    for preset in args.preset or []:
        raw_patterns.append(PRESET_PATTERNS[preset])

    for pattern in args.pattern or []:
        raw_patterns.append(pattern)

    if not raw_patterns:
        raw_patterns.extend(
            [
                PRESET_PATTERNS["randomloadout"],
                PRESET_PATTERNS["error"],
            ]
        )

    flags = re.IGNORECASE if args.ignore_case else 0
    return [re.compile(pattern, flags) for pattern in raw_patterns]


def read_lines(log_path: Path, tail: int) -> list[str]:
    lines_iter = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    if tail > 0:
        return list(deque(lines_iter, maxlen=tail))

    return list(lines_iter)


def matches_any_pattern(line: str, patterns: list[re.Pattern[str]]) -> bool:
    for pattern in patterns:
        if pattern.search(line):
            return True

    return False


def dedupe_consecutive(lines: list[str]) -> list[str]:
    if not lines:
        return []

    collapsed: list[str] = []
    previous = None
    repeat_count = 0
    for line in lines:
        if line == previous:
            repeat_count += 1
            continue

        if previous is not None:
            collapsed.append(previous)
            if repeat_count > 0:
                collapsed.append("[previous line repeated {0} more times]".format(repeat_count))

        previous = line
        repeat_count = 0

    if previous is not None:
        collapsed.append(previous)
        if repeat_count > 0:
            collapsed.append("[previous line repeated {0} more times]".format(repeat_count))

    return collapsed


def main() -> int:
    args = parse_args()

    log_path = Path(args.log_path).expanduser()
    if not log_path.is_file():
        return fail("Log file not found: {0}".format(log_path))

    patterns = build_patterns(args)
    lines = read_lines(log_path, args.tail)
    matched_lines = [line for line in lines if matches_any_pattern(line, patterns)]
    if args.dedupe_consecutive:
        matched_lines = dedupe_consecutive(matched_lines)

    output_text = "\n".join(matched_lines)
    if matched_lines:
        output_text += "\n"

    if args.output:
        output_path = Path(args.output).expanduser()
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(output_text, encoding="utf-8")
        print("Wrote {0} filtered log lines to {1}".format(len(matched_lines), output_path))
    else:
        sys.stdout.write(output_text)
        print(
            "Matched {0} lines from {1}".format(len(matched_lines), log_path),
            file=sys.stderr,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
