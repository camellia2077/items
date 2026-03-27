# 技术栈与选型

最后整理时间：2026-03-25

## 当前选型结论

- 推荐语言：`C#`
- 推荐目标框架：`.NET Framework 3.5`
- 推荐加载器：`BepInEx 5`
- 推荐 API：`ModTheGungeonAPI`
- 推荐反编译工具：`ILSpy`
- 调试可选：`dnSpyEx`
- 运行时补丁：`HarmonyX` 或 `MonoMod.RuntimeDetour`

## 为什么不是传统修改器技术栈

不推荐把第一版做成：

- C++ 外挂
- 外部内存扫描器
- Cheat Engine 表
- DLL 注入器 + 手写偏移逻辑

原因：

- 这条路对当前需求来说复杂度过高
- ETG 已有现成的 C# / Unity 模组生态
- 你的目标是“改游戏流程”和“发已有物品”，不是破解对抗

## 为什么推荐 .NET Framework 3.5

当前 ETG 社区示例和文档仍普遍以 `net35` 为工程基线：

- `BepInExExampleMod`
- `BepInExExampleModItems`
- `A Bleaker Item Pack`
- 社区 GitBook 的 Visual Studio 设置页

这意味着：

- 第一版工程最好直接跟随现有生态
- 不要在起步阶段额外引入跨版本兼容问题

## 各工具在项目中的角色

### C#

- 编写插件逻辑
- 处理随机种子、白名单、日志与触发条件

### BepInEx

- 负责加载插件 DLL
- 提供 Unity 插件运行环境

### ModTheGungeonAPI

- 提供 ETG 相关 API、兼容层和实用工具
- 降低直接贴原生游戏类的成本

### ILSpy

- 打开 `Enter the Gungeon\EtG_Data\Managed\Assembly-CSharp.dll`
- 查找事件、类、方法和调用链

### dnSpyEx

- 需要动态调试时使用
- 不作为第一步必需工具

### HarmonyX / MonoMod.RuntimeDetour

- 当现成事件不够准确时，用来补 Hook
- 不建议在第一版一开始就重度依赖

## 第一阶段最低依赖集合

建议先只保留这些：

- `BepInEx`
- `ModTheGungeonAPI`
- `Assembly-CSharp`
- `UnityEngine`

## 可选但暂缓的依赖

### Alexandria

- 未来若要做自定义物品或更多便利 API，可考虑加入
- 第一阶段“随机发基础游戏物品”并不强依赖它

### Gunfig

- 未来若要做游戏内配置界面可考虑
- 第一阶段可以先用代码常量或简单配置

