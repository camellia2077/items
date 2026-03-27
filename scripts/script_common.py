from __future__ import annotations

import argparse
import subprocess
import sys
from typing import Callable
from pathlib import Path


PROJECT_NAME = "RandomLoadout"
CONFIGURATION_CHOICES = ("Debug", "Release")
MSBUILD_PATH = Path(r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")
REQUIRED_BUILD_DLLS = (
    "Assembly-CSharp.dll",
    "BepInEx.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
)


def get_repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def add_configuration_argument(parser: argparse.ArgumentParser, help_text: str) -> None:
    parser.add_argument(
        "-c",
        "--configuration",
        default="Debug",
        choices=CONFIGURATION_CHOICES,
        help=help_text,
    )


def resolve_msbuild() -> Path:
    if not MSBUILD_PATH.is_file():
        raise FileNotFoundError("MSBuild not found at '{0}'.".format(MSBUILD_PATH))
    return MSBUILD_PATH


def ensure_required_build_dlls(repo_root: Path) -> None:
    lib_path = repo_root / "lib"
    missing = [dll for dll in REQUIRED_BUILD_DLLS if not (lib_path / dll).is_file()]
    if missing:
        raise FileNotFoundError(
            "Missing required DLLs in '{0}': {1}".format(lib_path, ", ".join(missing))
        )


def get_plugin_project_path(repo_root: Path) -> Path:
    return repo_root / "src" / PROJECT_NAME / "{0}.csproj".format(PROJECT_NAME)


def get_test_project_path(repo_root: Path) -> Path:
    return repo_root / "tests" / "RandomLoadout.Core.Tests" / "RandomLoadout.Core.Tests.csproj"


def get_plugin_output_path(repo_root: Path, configuration: str) -> Path:
    return (
        repo_root / "src" / PROJECT_NAME / "bin" / configuration / "{0}.dll".format(PROJECT_NAME)
    )


def get_test_output_path(repo_root: Path, configuration: str) -> Path:
    return (
        repo_root
        / "tests"
        / "RandomLoadout.Core.Tests"
        / "bin"
        / configuration
        / "RandomLoadout.Core.Tests.exe"
    )


def run_process(command: list[str], cwd: Path) -> int:
    completed = subprocess.run(command, cwd=str(cwd))
    return completed.returncode


def fail(message: str) -> int:
    print(message, file=sys.stderr)
    return 1


def require_existing_directory(path: Path, label: str) -> Path:
    if not path.is_dir():
        raise FileNotFoundError("{0} does not exist: {1}".format(label, path))
    return path


def run_cli(main_func: Callable[[], int]) -> int:
    try:
        return main_func()
    except FileNotFoundError as error:
        return fail(str(error))
    except OSError as error:
        return fail("OS error: {0}".format(error))
    except KeyboardInterrupt:
        return fail("Operation cancelled.")
