# LOC Scanner

通用代码行数扫描工具，采用独立子项目目录组织。

## 目录结构

1. `src/loc_scanner/`
   - 工具实现与 CLI 入口
2. `config/`
   - 默认配置（`scan_lines.toml`）
3. `scripts/`
   - Windows bat 快捷入口
4. `tests/`
   - 工具测试
5. `docs/`
   - 使用文档

## 快速使用

从仓库根目录执行：

```bash
python -m tools.devtools.loc_scanner --lang py --under
```

常用参数：

- `--lang`：`cs | kt | py | rs`
- `paths`：可选，待扫描目录；默认可覆盖配置中的 `default_paths`（若该语言 `path_mode = "toml_only"`，则忽略命令行 `paths`）
- `--workspace-root`：相对路径解析根目录，默认当前目录
- `--config`：配置文件路径，默认 `tools/devtools/loc_scanner/config/scan_lines.toml`
- `--log-file`：日志输出路径；不传时写入 `<workspace-root>/.loc_scanner_logs/scan_<lang>.json`
- `--over N` / `--under [N]` / `--dir-over-files [N]`

Windows 阈值扫描入口：

- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-over`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-dir-over-files`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-over`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-dir-over-files`

这些 bat 入口使用固定阈值与固定模式；如果需要自定义阈值、层级或路径，请直接调用 `python -m tools.devtools.loc_scanner`。

更多示例见：[docs/usage.md](docs/usage.md)，配置字段说明见：[docs/toml_config.md](docs/toml_config.md)

