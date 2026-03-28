---
description: Agent 专用 Python 工程历史模板
---

# Python Engineering History Guide

本文件定义 agent 编写 Python 相关工程历史时必须遵守的最小规则。该类历史面向组件演进与工作流变更，使用更新时间，不写版本号。

## Scope

- 适用于 `tools/`、`tools/tests/`、`test/framework/` 及相关 Python 工程文档
- 适用于 toolchain、test framework、Python CLI、测试基建等工程演进记录
- 不适用于正式版本发布说明；正式 release 继续使用 `history-style-guide.md`

## Hard Rules

- 最新条目必须写在最前面
- 标题格式必须为 `## YYYY-MM-DD`
- 日期必须使用 ISO 8601：`YYYY-MM-DD`
- 文件名必须使用日期命名，如 `2026-03-24.md`
- 同一组件同一天只保留一个日期文件，后续更新直接追加到同一个文件
- 一个日期文件只记录一个组件或子系统，不同组件分别建各自的日期文件
- 分类只使用以下几类：
  - `### 新增功能 (Added)`
  - `### 技术改进/重构 (Changed/Refactor)`
  - `### 修复 (Fixed)`
  - `### 安全性 (Security)`
  - `### 弃用/删除 (Deprecated/Removed)`
- 列表统一使用 `* `
- 空分类不要保留
- 条目应以动词开头，简短直接
- 文件名、命令、路径、配置键统一使用反引号

## Recommended Paths

- `docs/toolchain/history/YYYY-MM-DD.md`
- `docs/test_framework/history/YYYY-MM-DD.md`

## Template

```md
## YYYY-MM-DD

### 新增功能 (Added)
* 新增 `<workflow or module>`

### 技术改进/重构 (Changed/Refactor)
* 重构 `<module or workflow>`

### 修复 (Fixed)
* 修复 `<bug or regression>`

### 安全性 (Security)
* 修复 `<security issue>`

### 弃用/删除 (Deprecated/Removed)
* 删除 `<removed item>`
```

## Writing Rules

- 只写用户可感知或工程上重要的变化
- 同类改动尽量合并表达
- 涉及目录迁移时，明确写出路径
- 若涉及命令参数、测试分组、执行顺序、产物目录或配置格式变化，应明确写出旧口径与新口径
- `tools/tests/*` 的回归测试变更归入对应 Python 工具链组件历史，不单独拆成测试发布说明
- `test/framework/core/*` 的结构调整、导入迁移与运行时装配变化归入 test framework 历史

## Generic Example

```md
## 2026-03-24

### 技术改进/重构 (Changed/Refactor)
* 重构 `tools/toolchain/commands/cmd_validate/`，把轨道执行、日志记录与汇总输出拆成独立模块。

### 修复 (Fixed)
* 更新 `tools/tests/verify/` 与 `tools/tests/validate/` 回归用例，使其与新的 `verify` 执行语义保持一致。
```
