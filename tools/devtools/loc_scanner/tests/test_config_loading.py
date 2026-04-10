import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
SRC_ROOT = PROJECT_ROOT / "src"
if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))

from loc_scanner.config import load_language_config


def test_load_default_config_for_python() -> None:
    config_path = PROJECT_ROOT / "config" / "scan_lines.toml"
    config = load_language_config(config_path=config_path, lang="py")

    assert config.lang == "py"
    assert ".py" in config.extensions
    assert config.default_over_threshold > 0
    assert config.default_under_threshold > 0
    assert config.path_mode == "toml_only"
    assert config.default_paths == ["tools"]


def test_load_default_config_for_csharp_uses_toml_only_paths() -> None:
    config_path = PROJECT_ROOT / "config" / "scan_lines.toml"
    config = load_language_config(config_path=config_path, lang="cs")

    assert config.path_mode == "toml_only"
    assert config.default_paths == ["src", "tests"]
    assert ".cs" in config.extensions
