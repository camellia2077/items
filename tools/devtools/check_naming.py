from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List


SCRIPT_PATH = Path(__file__).resolve()
SCRIPT_DIRECTORY = SCRIPT_PATH.parent
REPOSITORY_ROOT = SCRIPT_DIRECTORY.parent.parent
DEFAULT_RULES_PATH = SCRIPT_DIRECTORY / "naming_rules.json"


@dataclass(frozen=True)
class NamingRule:
    rule_id: str
    description: str
    pattern: re.Pattern[str]
    replacement: str
    includes: List[str]
    excludes: List[str]


@dataclass(frozen=True)
class NamingFinding:
    rule_id: str
    description: str
    replacement: str
    path: Path
    line_number: int
    line_text: str


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Check repository C# files for project-specific naming rules."
    )
    parser.add_argument(
        "--rules",
        default=str(DEFAULT_RULES_PATH),
        help="Path to the naming rules JSON file.",
    )
    parser.add_argument(
        "--paths",
        nargs="*",
        default=None,
        help="Optional glob overrides. Defaults to the targets declared in the rules file.",
    )
    parser.add_argument(
        "--repo-root",
        default=str(REPOSITORY_ROOT),
        help="Repository root used to resolve relative globs.",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print scanned file counts even when no findings are present.",
    )
    return parser.parse_args()


def load_rules(rules_path: Path) -> tuple[list[str], list[NamingRule]]:
    content = json.loads(rules_path.read_text(encoding="utf-8"))
    targets = content.get("targets", ["src/**/*.cs"])
    rules: list[NamingRule] = []
    for raw_rule in content.get("rules", []):
        flags = re.IGNORECASE if raw_rule.get("ignore_case", False) else 0
        rules.append(
            NamingRule(
                rule_id=raw_rule["id"],
                description=raw_rule["description"],
                pattern=re.compile(raw_rule["pattern"], flags),
                replacement=raw_rule.get("replacement", ""),
                includes=list(raw_rule.get("includes", [])),
                excludes=list(raw_rule.get("excludes", [])),
            )
        )
    return targets, rules


def resolve_target_files(repo_root: Path, globs: Iterable[str]) -> list[Path]:
    files: set[Path] = set()
    for pattern in globs:
        for path in repo_root.glob(pattern):
            if path.is_file():
                files.add(path.resolve())
    return sorted(files)


def should_apply_rule(path: Path, repo_root: Path, rule: NamingRule) -> bool:
    relative_path = path.relative_to(repo_root).as_posix()
    if rule.includes and not any(path_matches(relative_path, pattern) for pattern in rule.includes):
        return False
    if rule.excludes and any(path_matches(relative_path, pattern) for pattern in rule.excludes):
        return False
    return True


def path_matches(relative_path: str, pattern: str) -> bool:
    return Path(relative_path).match(pattern)


def scan_file(path: Path, repo_root: Path, rules: list[NamingRule]) -> list[NamingFinding]:
    findings: list[NamingFinding] = []
    lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    for line_number, line_text in enumerate(lines, start=1):
        for rule in rules:
            if not should_apply_rule(path, repo_root, rule):
                continue
            if rule.pattern.search(line_text):
                findings.append(
                    NamingFinding(
                        rule_id=rule.rule_id,
                        description=rule.description,
                        replacement=rule.replacement,
                        path=path,
                        line_number=line_number,
                        line_text=line_text.strip(),
                    )
                )
    return findings


def print_findings(findings: list[NamingFinding], repo_root: Path) -> None:
    for finding in findings:
        relative_path = finding.path.relative_to(repo_root).as_posix()
        message = (
            f"{relative_path}:{finding.line_number}: "
            f"{finding.rule_id}: {finding.description}"
        )
        if finding.replacement:
            message += f" Use `{finding.replacement}`."
        print(message)
        if finding.line_text:
            print(f"  {finding.line_text}")


def main() -> int:
    args = parse_arguments()
    repo_root = Path(args.repo_root).resolve()
    rules_path = Path(args.rules).resolve()
    target_globs, rules = load_rules(rules_path)
    files = resolve_target_files(repo_root, args.paths or target_globs)

    findings: list[NamingFinding] = []
    for path in files:
        findings.extend(scan_file(path, repo_root, rules))

    if findings:
        print_findings(findings, repo_root)
        print(f"Naming check failed: {len(findings)} issue(s) across {len(files)} file(s).")
        return 1

    if args.verbose:
        print(f"Naming check passed: scanned {len(files)} file(s), 0 issues.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
