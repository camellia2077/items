# LOC Scanner TOML 配置说明

默认配置文件位置：`tools/devtools/loc_scanner/config/scan_lines.toml`

## 1. 基本结构

配置按语言分段，每个语言一个 section：

```toml
[py]
display_name = "Python"
default_paths = ["."]
path_mode = "cli_override"
extensions = [".py", ".pyw"]
ignore_dirs = [".git", "__pycache__", "venv"]
ignore_prefixes = [".", "site-packages"]
default_over_threshold = 250
default_under_threshold = 120
default_dir_over_files = 10
over_inclusive = true
```

支持的语言 section：

- `[cs]`
- `[kt]`
- `[py]`
- `[rs]`

## 2. 字段说明

- `display_name`:
  - 用途：报告标题显示名。
  - 类型：字符串。
- `default_paths`:
  - 用途：默认扫描路径。
  - 类型：字符串数组。
  - 规则：相对路径按 `--workspace-root` 解析。
- `path_mode`:
  - 用途：控制命令行 `paths` 与 `default_paths` 的关系。
  - 类型：字符串。
  - 可选值：
    - `cli_override`：有命令行 `paths` 时覆盖 `default_paths`（默认）。
    - `toml_only`：始终只使用 `default_paths`，忽略命令行 `paths`。
    - `merge`：先用 `default_paths`，再追加命令行 `paths`（自动去重）。
- `extensions`:
  - 用途：纳入统计的文件扩展名。
  - 类型：字符串数组。
  - 示例：`[".cs"]`、`[".py"]`。
- `ignore_dirs`:
  - 用途：目录名精确匹配忽略。
  - 类型：字符串数组。
- `ignore_prefixes`:
  - 用途：目录名前缀匹配忽略（不区分大小写）。
  - 类型：字符串数组。
- `default_over_threshold`:
  - 用途：`over` 模式默认阈值（大文件阈值）。
  - 类型：正整数。
- `default_under_threshold`:
  - 用途：`under` 模式默认阈值（小文件阈值）。
  - 类型：正整数。
- `default_dir_over_files`:
  - 用途：`--dir-over-files` 不带值时的默认目录文件数阈值。
  - 类型：正整数。
- `over_inclusive`:
  - 用途：控制 `over` 判定是否包含等号。
  - 类型：布尔值。
  - `true` 表示 `>=`，`false` 表示 `>`。

## 3. 常见配置场景

- 只扫描某个子目录：
  - 在目标语言的 `default_paths` 写入该目录，例如 `["apps/android"]`。
- 强制只能用 TOML 控制扫描目录（忽略 bat/CLI 的路径参数）：
  - 设置 `path_mode = "toml_only"`。
- 提高大文件告警门槛：
  - 调大 `default_over_threshold`。
- 只统计源码，不统计头文件：
  - 调整 `extensions`，移除不需要的扩展名。
- 避免扫描产物目录：
  - 在 `ignore_dirs` 加入 `build`、`out`、`target` 等目录名。

## 4. 常见错误

- `default_paths` 不是数组，或数组中有空字符串。
- `path_mode` 不是 `cli_override` / `toml_only` / `merge`。
- `default_*_threshold` 不是正整数。
- 漏写语言 section（例如没有 `[py]`）。
- 扩展名未带点（建议写成 `.py`、`.cs` 形式）。

当配置不合法时，命令会输出 `[ERROR] 配置加载失败` 并返回非 0 状态码。
