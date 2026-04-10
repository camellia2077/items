import argparse
import os
from pathlib import Path

from .config import LanguageConfig

UNDER_SENTINEL = -1


class ScanArgumentResolver:
    @staticmethod
    def resolve_mode_and_threshold(
        args: argparse.Namespace,
        config: LanguageConfig,
    ) -> tuple[str, int]:
        if args.under is not None:
            if args.under == UNDER_SENTINEL:
                return "under", config.default_under_threshold
            return "under", args.under
        if args.over is not None:
            return "over", args.over
        if args.threshold is not None:
            return "over", args.threshold
        return "over", config.default_over_threshold

    @staticmethod
    def resolve_paths(
        raw_paths: list[str],
        default_paths: list[str],
        path_mode: str,
        workspace_root: Path,
    ) -> list[Path]:
        source: list[str]
        if path_mode == "toml_only":
            source = default_paths
        elif path_mode == "merge":
            source = [*default_paths, *raw_paths]
        else:
            source = raw_paths if raw_paths else default_paths

        resolved: list[Path] = []
        seen: set[Path] = set()
        for item in source:
            path = Path(item)
            if not path.is_absolute():
                path = (workspace_root / path).resolve()
            else:
                path = path.resolve()
            if path in seen:
                continue
            seen.add(path)
            resolved.append(path)
        return resolved


class LocScanService:
    _LINE_COUNT_CHUNK_SIZE = 1024 * 1024

    def __init__(self, config: LanguageConfig):
        self.config = config
        self._ignore_dirs_lower = {name.lower() for name in config.ignore_dirs}
        self._extensions_tuple = tuple(ext.lower() for ext in config.extensions)

    def analyze_path(
        self,
        target_dir: Path,
        mode: str,
        threshold: int,
    ) -> list[tuple[str, int]]:
        result_files: list[tuple[str, int]] = []

        for root, dirs, files in os.walk(target_dir):
            dirs[:] = [name for name in dirs if not self._should_skip_dir(name)]

            for file_name in files:
                if not file_name.lower().endswith(self._extensions_tuple):
                    continue

                file_path = os.path.join(root, file_name)
                try:
                    line_count = self._count_lines_fast(file_path)
                except Exception as error:
                    print(f"读取文件时发生意外错误 {file_path}: {error}")
                    continue

                if self._matches_threshold(line_count, mode, threshold):
                    result_files.append((file_path, line_count))

        result_files.sort(key=lambda item: item[1], reverse=(mode == "over"))
        return result_files

    def analyze_directory_file_counts(
        self,
        target_dir: Path,
        threshold: int,
        max_depth: int | None = None,
    ) -> list[tuple[str, int]]:
        result_dirs: list[tuple[str, int]] = []
        root_depth = len(target_dir.parts)

        for root, dirs, files in os.walk(target_dir):
            current_path = Path(root)
            current_depth = len(current_path.parts) - root_depth
            dirs[:] = [name for name in dirs if not self._should_skip_dir(name)]
            if max_depth is not None and current_depth >= max_depth:
                dirs[:] = []

            file_count = 0
            for file_name in files:
                if file_name.lower().endswith(self._extensions_tuple):
                    file_count += 1

            if file_count > threshold:
                result_dirs.append((str(current_path), file_count))

        result_dirs.sort(key=lambda item: item[1], reverse=True)
        return result_dirs

    def _should_skip_dir(self, dir_name: str) -> bool:
        dir_lower = dir_name.lower()
        if dir_lower in self._ignore_dirs_lower:
            return True
        return any(dir_lower.startswith(prefix) for prefix in self.config.ignore_prefixes)

    def _matches_threshold(self, line_count: int, mode: str, threshold: int) -> bool:
        if mode == "over":
            if self.config.over_inclusive:
                return line_count >= threshold
            return line_count > threshold
        return line_count < threshold

    def _count_lines_fast(self, file_path: str) -> int:
        line_count = 0
        has_data = False
        last_byte = b""

        with open(file_path, "rb") as handle:
            while True:
                chunk = handle.read(self._LINE_COUNT_CHUNK_SIZE)
                if not chunk:
                    break
                has_data = True
                line_count += chunk.count(b"\n")
                last_byte = chunk[-1:]

        if has_data and last_byte != b"\n":
            line_count += 1
        return line_count
