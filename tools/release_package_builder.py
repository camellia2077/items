from __future__ import annotations

import re
import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

from release_package_compliance import (
    ensure_no_game_owned_dlls,
    ensure_required_package_files,
    stage_license_files,
    write_install_readme,
    write_third_party_notices,
)
from release_package_upstream import (
    ensure_cached_upstream_archive,
    extract_upstream_content,
    sha256_for_file,
)
from tool_common import get_default_sync_paths, get_plugin_output_path, run_process


ASSEMBLY_INFO_PATH = Path("src") / "RandomLoadout" / "Properties" / "AssemblyInfo.cs"
DIST_DIRECTORY = Path("dist")


def detect_mod_version(repo_root: Path) -> str:
    assembly_info_path = repo_root / ASSEMBLY_INFO_PATH
    if not assembly_info_path.is_file():
        raise FileNotFoundError("Assembly info not found: {0}".format(assembly_info_path))

    raw_text = assembly_info_path.read_text(encoding="utf-8")
    patterns = (
        r'AssemblyInformationalVersion\("(?P<version>[^"]+)"\)',
        r'AssemblyVersion\("(?P<version>[^"]+)"\)',
    )
    for pattern in patterns:
        match = re.search(pattern, raw_text)
        if match:
            return match.group("version").strip()

    raise ValueError("Could not detect mod version from '{0}'.".format(assembly_info_path))


def normalize_version_tag(version: str) -> str:
    normalized = version.strip()
    if not normalized:
        raise ValueError("Package version was empty.")
    return normalized if normalized.startswith("v") else "v" + normalized


def build_plugin_if_needed(repo_root: Path, configuration: str, skip_build: bool) -> None:
    if skip_build:
        return

    build_script = repo_root / "tools" / "build.py"
    exit_code = run_process(
        [sys.executable, str(build_script), "--configuration", configuration],
        repo_root,
    )
    if exit_code != 0:
        raise OSError("Build failed with exit code {0}. Release packaging aborted.".format(exit_code))


def overlay_randomloadout_files(repo_root: Path, configuration: str, staging_root: Path) -> None:
    plugin_path = get_plugin_output_path(repo_root, configuration)
    if not plugin_path.is_file():
        raise FileNotFoundError("Build output not found: {0}".format(plugin_path))

    plugin_target = staging_root / "BepInEx" / "plugins" / plugin_path.name
    plugin_target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(str(plugin_path), str(plugin_target))

    config_directory = staging_root / "BepInEx" / "config"
    config_directory.mkdir(parents=True, exist_ok=True)
    for default_path in get_default_sync_paths(repo_root):
        if not default_path.is_file():
            raise FileNotFoundError("Repository default file not found: {0}".format(default_path))

        shutil.copyfile(str(default_path), str(config_directory / default_path.name))


def create_release_zip(staging_root: Path, output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if output_path.exists():
        output_path.unlink()

    with zipfile.ZipFile(str(output_path), "w", compression=zipfile.ZIP_DEFLATED) as zip_file:
        for file_path in sorted(staging_root.rglob("*")):
            if not file_path.is_file():
                continue

            archive_name = str(file_path.relative_to(staging_root)).replace("\\", "/")
            zip_file.write(str(file_path), archive_name)


def build_release_package(
    repo_root: Path,
    metadata: dict,
    configuration: str,
    version: str,
    skip_build: bool,
    cache_directory: Path,
) -> Path:
    build_plugin_if_needed(repo_root, configuration, skip_build)

    mod_version = version.strip() if version else detect_mod_version(repo_root)
    version_tag = normalize_version_tag(mod_version)
    upstream_archive_path = ensure_cached_upstream_archive(cache_directory, metadata)

    with tempfile.TemporaryDirectory(prefix="randomloadout_release_") as temp_directory:
        staging_root = Path(temp_directory) / "staging"
        staging_root.mkdir(parents=True, exist_ok=True)

        extract_upstream_content(upstream_archive_path, metadata, staging_root)
        overlay_randomloadout_files(repo_root, configuration, staging_root)
        staged_components = stage_license_files(repo_root, metadata, staging_root)
        write_install_readme(version_tag, staging_root)
        write_third_party_notices(version_tag, metadata, staged_components, staging_root)
        ensure_no_game_owned_dlls(staging_root)
        ensure_required_package_files(staging_root)

        output_path = repo_root / DIST_DIRECTORY / "RandomLoadout-{0}-ETG.zip".format(version_tag)
        create_release_zip(staging_root, output_path)

    return output_path
