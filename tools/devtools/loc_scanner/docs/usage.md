# Usage

## 基本命令

```bash
python -m tools.devtools.loc_scanner --lang <cs|kt|py|rs> [paths ...] [--over N | --under [N] | --dir-over-files [N]] [--dir-max-depth N] [--log-file <path>]
```

## 示例

```bash
# Python 大文件
python -m tools.devtools.loc_scanner --lang py --over 200

# Kotlin 小文件（使用配置默认阈值）
python -m tools.devtools.loc_scanner --lang kt --under

# C# 大文件
python -m tools.devtools.loc_scanner --lang cs --over 300

# 目录文件密度扫描
python -m tools.devtools.loc_scanner --lang py --dir-over-files --dir-max-depth 2

# Python 大文件（使用配置默认路径）
python -m tools.devtools.loc_scanner --lang py --over 150

# C# 目录文件密度扫描（使用配置默认路径）
python -m tools.devtools.loc_scanner --lang cs --dir-over-files
```

## Windows bat 入口

```bat
tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-over
tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-dir-over-files
tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-over
tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-dir-over-files
tools\devtools\loc_scanner\scripts\run_py_over.bat
tools\devtools\loc_scanner\scripts\run_py_dir_over_files.bat
tools\devtools\loc_scanner\scripts\run_cs_over.bat
tools\devtools\loc_scanner\scripts\run_cs_dir_over_files.bat
```

这些 bat 入口使用固定阈值与固定模式，不透传额外参数；如需自定义阈值或路径，请直接使用 `python -m tools.devtools.loc_scanner ...`。

注意：若语言配置中 `path_mode = "toml_only"`，则命令行 `paths`（包括 bat 透传的位置参数）会被忽略，仅使用 TOML 里的 `default_paths`。

## 配置文件

默认配置文件：`tools/devtools/loc_scanner/config/scan_lines.toml`

配置字段说明：`tools/devtools/loc_scanner/docs/toml_config.md`

