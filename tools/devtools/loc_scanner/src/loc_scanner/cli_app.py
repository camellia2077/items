import argparse
import json
from datetime import datetime
from pathlib import Path

from .config import LANG_CHOICES, LanguageConfig, load_language_config
from .reporter import LocConsoleReporter
from .service import UNDER_SENTINEL, LocScanService, ScanArgumentResolver


class LocCliApplication:
    DIR_OVER_SENTINEL = -1
    _LOG_DIR_GITIGNORE_CONTENT = "# Automatically created by loc_scanner.\n*\n"

    def run(self) -> int:
        args = self.parse_args()
        workspace_root = self._resolve_workspace_root(args.workspace_root)
        config_path = self._resolve_config_path(args.config, workspace_root=workspace_root)
        log_path = self._resolve_log_path(
            args.log_file,
            args.lang,
            workspace_root=workspace_root,
        )
        log_path.parent.mkdir(parents=True, exist_ok=True)

        exit_code, payload = self._run_scan(
            args,
            workspace_root=workspace_root,
            config_path=config_path,
        )
        self._write_json_log(log_path, payload)

        print(f"[LOG] 扫描日志: {log_path}")
        return exit_code

    def _run_scan(
        self,
        args: argparse.Namespace,
        *,
        workspace_root: Path,
        config_path: Path,
    ) -> tuple[int, dict]:
        payload = self._build_base_payload(args=args, workspace_root=workspace_root)

        if args.dir_max_depth is not None and args.dir_over_files is None:
            return self._error(
                "--dir-max-depth 只能与 --dir-over-files 一起使用。",
                payload,
            )

        try:
            config = load_language_config(config_path=config_path, lang=args.lang)
        except (FileNotFoundError, ValueError, OSError) as error:
            return self._error(f"配置加载失败: {error}", payload)

        resolver = ScanArgumentResolver()
        scan_service = LocScanService(config)
        reporter = LocConsoleReporter(config)
        paths = resolver.resolve_paths(
            args.paths,
            config.default_paths,
            config.path_mode,
            workspace_root=workspace_root,
        )

        if args.dir_over_files is not None:
            return self._run_dir_file_scan(args, payload, paths, config, scan_service, reporter)

        mode, threshold = resolver.resolve_mode_and_threshold(args, config)
        if threshold <= 0:
            return self._error("阈值必须是正整数。", payload)
        return self._run_line_scan(
            payload=payload,
            paths=paths,
            mode=mode,
            threshold=threshold,
            scan_service=scan_service,
            reporter=reporter,
        )

    def _run_line_scan(
        self,
        *,
        payload: dict,
        paths: list[Path],
        mode: str,
        threshold: int,
        scan_service: LocScanService,
        reporter: LocConsoleReporter,
    ) -> tuple[int, dict]:
        reporter.print_line_scan_header(mode, threshold)
        path_results: list[dict] = []
        total_matched_files = 0

        for path in paths:
            if not path.exists():
                reporter.print_missing_path(path)
                path_results.append({"path": str(path), "matched_files": []})
                continue
            matched = scan_service.analyze_path(path, mode, threshold)
            reporter.print_line_path_result(path, mode, threshold, matched)
            matched_files = [{"path": file_path, "lines": lines} for file_path, lines in matched]
            total_matched_files += len(matched_files)
            path_results.append({"path": str(path), "matched_files": matched_files})

        payload["status"] = "ok"
        payload["scan"] = {"mode": mode, "threshold": threshold}
        payload["results"] = path_results
        payload["summary"] = {"matched_files": total_matched_files}
        return 0, payload

    def _run_dir_file_scan(
        self,
        args: argparse.Namespace,
        payload: dict,
        paths: list[Path],
        config: LanguageConfig,
        scan_service: LocScanService,
        reporter: LocConsoleReporter,
    ) -> tuple[int, dict]:
        if args.dir_over_files == self.DIR_OVER_SENTINEL:
            threshold = config.default_dir_over_files
        else:
            threshold = int(args.dir_over_files)

        if threshold <= 0:
            return self._error("--dir-over-files 阈值必须是正整数。", payload)
        if args.dir_max_depth is not None and args.dir_max_depth < 0:
            return self._error("--dir-max-depth 必须是 >= 0 的整数。", payload)

        reporter.print_dir_scan_header(threshold, args.dir_max_depth)
        path_results: list[dict] = []
        total_matched_dirs = 0

        for path in paths:
            if not path.exists():
                reporter.print_missing_path(path)
                path_results.append({"path": str(path), "matched_dirs": []})
                continue
            matched = scan_service.analyze_directory_file_counts(
                path,
                threshold=threshold,
                max_depth=args.dir_max_depth,
            )
            reporter.print_dir_path_result(path, threshold, matched)
            matched_dirs = [
                {"path": dir_path, "files": file_count} for dir_path, file_count in matched
            ]
            total_matched_dirs += len(matched_dirs)
            path_results.append({"path": str(path), "matched_dirs": matched_dirs})

        payload["status"] = "ok"
        payload["scan"] = {"mode": "dir_over_files", "threshold": threshold}
        if args.dir_max_depth is not None:
            payload["scan"]["max_depth"] = args.dir_max_depth
        payload["results"] = path_results
        payload["summary"] = {"matched_dirs": total_matched_dirs}
        return 0, payload

    @staticmethod
    def parse_args() -> argparse.Namespace:
        default_config = str(LocCliApplication._default_config_path())
        parser = argparse.ArgumentParser(
            description="统一代码行数扫描工具（C#/Kotlin/Python/Rust）。"
        )
        parser.add_argument(
            "--lang",
            choices=LANG_CHOICES,
            required=True,
            help="语言类型: cs | kt | py | rs。",
        )
        parser.add_argument(
            "paths",
            nargs="*",
            help=(
                "待扫描目录（可传多个，支持相对/绝对路径）。"
                "未传时使用 TOML default_paths；若 path_mode=toml_only 则忽略该参数。"
            ),
        )
        parser.add_argument(
            "--workspace-root",
            default=".",
            help="工作区根目录。paths/config/log-file 的相对路径基于该目录解析。",
        )
        parser.add_argument(
            "--config",
            default=default_config,
            help=f"TOML 配置文件路径。默认: {default_config}",
        )
        parser.add_argument(
            "--log-file",
            default=None,
            help=(
                "扫描日志输出路径（支持相对/绝对）。"
                "未传时默认写入 <workspace-root>/.loc_scanner_logs/scan_<lang>.json。"
            ),
        )

        group = parser.add_mutually_exclusive_group()
        group.add_argument("--over", type=int, metavar="N", help="扫描大文件（over 模式）。")
        group.add_argument(
            "--under",
            type=int,
            nargs="?",
            const=UNDER_SENTINEL,
            metavar="N",
            help="扫描小文件（under 模式）。不传 N 时使用 TOML 的 default_under_threshold。",
        )
        group.add_argument(
            "--dir-over-files",
            type=int,
            nargs="?",
            const=LocCliApplication.DIR_OVER_SENTINEL,
            metavar="N",
            help=(
                "扫描目录中代码文件数超过 N 的目录。"
                "不传 N 时使用 TOML 中该语言的 default_dir_over_files。"
            ),
        )
        parser.add_argument("-t", "--threshold", type=int, help="兼容旧参数，等价于 --over N。")
        parser.add_argument(
            "--dir-max-depth",
            type=int,
            default=None,
            help="目录扫描最大深度（相对输入根目录；0 仅根目录）。仅与 --dir-over-files 配合使用。",
        )
        return parser.parse_args()

    @staticmethod
    def _default_config_path() -> Path:
        return (Path(__file__).resolve().parents[2] / "config" / "scan_lines.toml").resolve()

    @staticmethod
    def _resolve_workspace_root(workspace_root: str) -> Path:
        return Path(workspace_root).resolve()

    @staticmethod
    def _resolve_config_path(config_path: str, *, workspace_root: Path) -> Path:
        path = Path(config_path)
        if not path.is_absolute():
            path = workspace_root / path
        return path.resolve()

    @staticmethod
    def _resolve_log_path(log_file: str | None, lang: str, *, workspace_root: Path) -> Path:
        if log_file:
            path = Path(log_file)
            if not path.is_absolute():
                path = (workspace_root / path).resolve()
            else:
                path = path.resolve()
            return path
        return (workspace_root / ".loc_scanner_logs" / f"scan_{lang}.json").resolve()

    @staticmethod
    def _build_base_payload(*, args: argparse.Namespace, workspace_root: Path) -> dict:
        return {
            "generated_at": datetime.now().astimezone().isoformat(timespec="seconds"),
            "status": "unknown",
            "lang": args.lang,
            "workspace_root": str(workspace_root),
        }

    @staticmethod
    def _write_json_log(path: Path, payload: dict) -> None:
        path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        LocCliApplication._ensure_log_dir_gitignore(path.parent)

    @staticmethod
    def _ensure_log_dir_gitignore(log_dir: Path) -> None:
        gitignore_path = log_dir / ".gitignore"
        required_lines = ("# Automatically created by loc_scanner.", "*")
        if not gitignore_path.exists():
            gitignore_path.write_text(
                LocCliApplication._LOG_DIR_GITIGNORE_CONTENT,
                encoding="utf-8",
            )
            return

        existing_lines = gitignore_path.read_text(encoding="utf-8").splitlines()
        missing = [line for line in required_lines if line not in existing_lines]
        if not missing:
            return

        output_lines = existing_lines[:]
        if output_lines and output_lines[-1].strip():
            output_lines.append("")
        output_lines.extend(missing)
        gitignore_path.write_text("\n".join(output_lines) + "\n", encoding="utf-8")

    @staticmethod
    def _error(message: str, payload: dict) -> tuple[int, dict]:
        print(f"[ERROR] {message}")
        payload["status"] = "error"
        payload["error"] = message
        return 2, payload
