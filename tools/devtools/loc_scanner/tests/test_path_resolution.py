import argparse
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
SRC_ROOT = PROJECT_ROOT / "src"
if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))

from loc_scanner.cli_app import LocCliApplication
from loc_scanner.service import ScanArgumentResolver


def test_resolve_paths_with_workspace_root(tmp_path: Path) -> None:
    resolver = ScanArgumentResolver()
    workspace_root = tmp_path / "workspace"
    workspace_root.mkdir(parents=True, exist_ok=True)

    paths = resolver.resolve_paths(
        raw_paths=["src", str(tmp_path)],
        default_paths=["."],
        path_mode="cli_override",
        workspace_root=workspace_root,
    )

    assert paths[0] == (workspace_root / "src").resolve()
    assert paths[1] == tmp_path.resolve()


def test_resolve_paths_toml_only_ignores_cli_paths(tmp_path: Path) -> None:
    resolver = ScanArgumentResolver()
    workspace_root = tmp_path / "workspace"
    workspace_root.mkdir(parents=True, exist_ok=True)

    paths = resolver.resolve_paths(
        raw_paths=["libs", "apps"],
        default_paths=["apps", "libs"],
        path_mode="toml_only",
        workspace_root=workspace_root,
    )

    assert paths == [
        (workspace_root / "apps").resolve(),
        (workspace_root / "libs").resolve(),
    ]


def test_resolve_paths_merge_combines_and_deduplicates(tmp_path: Path) -> None:
    resolver = ScanArgumentResolver()
    workspace_root = tmp_path / "workspace"
    workspace_root.mkdir(parents=True, exist_ok=True)

    paths = resolver.resolve_paths(
        raw_paths=["libs", "tools"],
        default_paths=["apps", "libs"],
        path_mode="merge",
        workspace_root=workspace_root,
    )

    assert paths == [
        (workspace_root / "apps").resolve(),
        (workspace_root / "libs").resolve(),
        (workspace_root / "tools").resolve(),
    ]


def test_default_log_path_uses_workspace_root(tmp_path: Path) -> None:
    log_path = LocCliApplication._resolve_log_path(
        None,
        "py",
        workspace_root=tmp_path,
    )
    expected = (tmp_path / ".loc_scanner_logs" / "scan_py.json").resolve()
    assert log_path == expected


def test_build_base_payload_contains_workspace_root(tmp_path: Path) -> None:
    args = argparse.Namespace(lang="py")
    payload = LocCliApplication._build_base_payload(args=args, workspace_root=tmp_path)
    assert payload["lang"] == "py"
    assert payload["workspace_root"] == str(tmp_path)


def test_write_json_log_creates_gitignore_for_log_dir(tmp_path: Path) -> None:
    log_path = tmp_path / ".loc_scanner_logs" / "scan_py.json"
    log_path.parent.mkdir(parents=True, exist_ok=True)

    LocCliApplication._write_json_log(log_path, {"status": "ok"})

    gitignore_path = log_path.parent / ".gitignore"
    assert gitignore_path.exists()
    assert (
        gitignore_path.read_text(encoding="utf-8")
        == "# Automatically created by loc_scanner.\n*\n"
    )


def test_write_json_log_preserves_existing_gitignore_and_appends_rules(tmp_path: Path) -> None:
    log_path = tmp_path / ".loc_scanner_logs" / "scan_py.json"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    gitignore_path = log_path.parent / ".gitignore"
    gitignore_path.write_text("# keep custom rule\n", encoding="utf-8")

    LocCliApplication._write_json_log(log_path, {"status": "ok"})

    content = gitignore_path.read_text(encoding="utf-8")
    assert "# keep custom rule" in content
    assert "# Automatically created by loc_scanner." in content.splitlines()
    assert "*" in content.splitlines()
