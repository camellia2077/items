# 外部资料索引

最后整理时间：2026-03-25

本页记录已确认有用的外部链接，并说明它们是干什么的、为什么对本项目有帮助。

## 路线与生态

### Mod the Gungeon 已弃用

- 链接：<https://modthegungeon.github.io/>
- 作用：
  - 说明旧 `ETGMod` 已官方标记为弃用
  - 页面明确提示社区转向 `BepInEx`
- 对本项目的帮助：
  - 这是技术路线切换的关键证据
  - 旧教程可看概念，但不能照抄安装与依赖方案

### BepInExPack_EtG

- 链接：<https://thunderstore.io/c/enter-the-gungeon/p/BepInEx/BepInExPack_EtG/>
- 作用：
  - ETG 专用的 `BepInEx` 打包版本
  - 作为当前 Mod 加载基础
- 对本项目的帮助：
  - 我们的 DLL 最终会被放进 `BepInEx/plugins`
  - 开发前必须先确认这层加载环境工作正常

### ModTheGungeonAPI

- 链接：<https://thunderstore.io/c/enter-the-gungeon/p/MtG_API/Mod_the_Gungeon_API/>
- 作用：
  - 当前 ETG 的主要 API 层之一
  - 在 `BepInEx` 上提供旧生态的重要能力
- 对本项目的帮助：
  - 发基础物品时，优先站在这层 API 上实现
  - 减少直接和底层游戏类硬拼的工作量

### ModTheGungeonAPI Releases

- 链接：<https://github.com/SpecialAPI/ModTheGungeonAPI/releases>
- 已确认信息：
  - `v1.9.2`
  - 发布时间：`2025-01-21`
- 对本项目的帮助：
  - 证明它不是完全停更的死库
  - 让后续 agent 可以合理默认“先用现成 API，不急着自己造轮子”

## 安装与开发环境

### MTG API 安装指南

- 链接：<https://github.com/SpecialAPI/ModTheGungeonAPI/wiki/BepInEx-and-Mod-the-Gungeon-API-installation-guide>
- 作用：
  - 说明 `BepInEx` 与 `MTG API` 的安装顺序
  - 包含自动安装与手动安装思路
- 对本项目的帮助：
  - 后续搭开发测试环境时的优先参考
  - 可作为环境验收标准的一部分

### EtG Modding Guide: 安装

- 链接：<https://mtgmodders.gitbook.io/etg-modding-guide/getting-started/mod-etg-installation>
- 作用：
  - 社区安装入口
  - 再次确认当前主路线是 `BepInEx`
- 对本项目的帮助：
  - 和官方 wiki 互相印证
  - 为后续开发文档提供统一说法

### EtG Modding Guide: Visual Studio 设置

- 链接：<https://mtgmodders.gitbook.io/etg-modding-guide/getting-started/setting-up-visual-studio>
- 已确认信息：
  - 推荐 `.NET Framework 3.5`
  - 推荐 `Visual Studio 2022`
  - 推荐从 `BepInExExampleModItems` 起步
- 对本项目的帮助：
  - 这是工程基础设置的直接参考
  - 也解释了为什么社区项目仍以 `net35` 为主

### BepInEx 官方插件教程

- 链接：<https://docs.bepinex.dev/v5.4.11/articles/dev_guide/plugin_tutorial/index.html>
- 作用：
  - 说明插件的最小结构
  - 说明 DLL 如何被加载
- 对本项目的帮助：
  - 如果不直接复制示例仓库，这篇就是最小工程基线

## 反编译与 Hook

### ILSpy 使用教程

- 链接：<https://mtgmodders.gitbook.io/etg-modding-guide/getting-started/useful-tools/using-ilspy>
- 作用：
  - 说明 ETG 反编译应从 `Enter the Gungeon\EtG_Data\Managed\Assembly-CSharp.dll` 开始
- 对本项目的帮助：
  - 后续找“新 run 开始”“玩家初始化”“发物品调用链”时的主战场

### BepInEx 运行时补丁文档

- 链接：<https://docs.bepinex.dev/master/articles/dev_guide/runtime_patching.html>
- 作用：
  - 说明 `HarmonyX` 与 `MonoMod.RuntimeDetour` 的定位
- 对本项目的帮助：
  - 当现成事件不足时，可据此选择 Hook 方案

### EtG Modding Guide: Hook 教程

- 链接：<https://mtgmodders.gitbook.io/etg-modding-guide/misc/how-to-create-a-hook>
- 已确认信息：
  - 示例中直接 Hook `PlayerController.AcquirePassiveItem`
  - 使用 `MonoMod.RuntimeDetour.Hook`
- 对本项目的帮助：
  - 说明 `AcquirePassiveItem` 是值得重点搜索的方法名
  - 给未来的精确流程拦截提供样例

### EtG Modding Guide: 事件订阅

- 链接：<https://mtgmodders.gitbook.io/etg-modding-guide/misc/subscribing-methods-to-actions>
- 作用：
  - 说明存在现成事件时，应先订阅事件而不是直接 Hook
- 对本项目的帮助：
  - 非常适合“新 run 开始时发开局物品”这种需求
  - 可作为后续实现时的默认优先级规则

## 数据与示例代码

### 基础游戏 Item / Gun ID 列表

- 链接：
  - <https://mtgmodders.gitbook.io/etg-modding-guide/various-lists-of-ids-sounds-etc./list-of-item-and-gun-ids>
  - <https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/Content/items.txt>
- 作用：
  - 提供基础游戏物品与枪械 ID
- 对本项目的帮助：
  - 可直接用来构建随机白名单
  - 后续很适合整理成 `starter_guns`、`starter_passives`、`starter_actives`

### BepInExExampleMod

- 链接：
  - <https://github.com/SpecialAPI/BepInExExampleMod>
  - <https://raw.githubusercontent.com/SpecialAPI/BepInExExampleMod/master/Plugin.cs>
- 已确认信息：
  - 使用 `[BepInPlugin]`
  - 使用 `[BepInDependency(ETGModMainBehaviour.GUID)]`
  - 通过 `ETGModMainBehaviour.WaitForGameManagerStart(GMStart)` 启动
- 对本项目的帮助：
  - 最接近“第一版随机开局插件”的骨架

### BepInExExampleModItems

- 链接：
  - <https://github.com/SpecialAPI/BepInExExampleModItems>
  - <https://raw.githubusercontent.com/SpecialAPI/BepInExExampleModItems/master/Plugin.cs>
  - <https://raw.githubusercontent.com/SpecialAPI/BepInExExampleModItems/master/ExamplePassive.cs>
- 已确认信息：
  - 示例依赖 `Alexandria`
  - 展示了自定义 `PassiveItem` 注册方式
- 对本项目的帮助：
  - 当前不是最短路径
  - 如果以后做自定义物品，这是直接参考源

### A Bleaker Item Pack

- 链接：
  - <https://github.com/BleakBubbles/A-Bleaker-Item-Pack>
  - <https://raw.githubusercontent.com/BleakBubbles/A-Bleaker-Item-Pack/master/Tools/Module.cs>
  - <https://raw.githubusercontent.com/BleakBubbles/A-Bleaker-Item-Pack/master/Mod.csproj>
- 已确认信息：
  - 插件入口为 `BaseUnityPlugin`
  - 使用 `[BepInDependency("etgmodding.etg.mtgapi")]`
  - `Start()` 中调用 `ETGModMainBehaviour.WaitForGameManagerStart(GMStart)`
  - 工程目标框架仍是 `.NET Framework 3.5`
- 对本项目的帮助：
  - 提供一个更接近真实中型项目的工程组织参考
  - 对依赖引用和初始化顺序很有价值

## 可选扩展

### Alexandria

- 链接：<https://thunderstore.io/c/enter-the-gungeon/p/Alexandria/Alexandria/>
- 作用：
  - ETG 社区常见公共库
- 对本项目的帮助：
  - 未来若要扩展自定义物品或便利 API，可考虑加入
  - 第一阶段不是刚需

### Hat Loader

- 链接：<https://thunderstore.io/c/enter-the-gungeon/p/SpecialAPI/Hat_Loader/>
- 已确认信息：
  - README 提到 `random hats list`
  - 支持新 run 开始时随机给玩家帽子
- 对本项目的帮助：
  - 证明“新 run 开始时随机赋予内容”在社区里是合理模式

### Gunfig

- 链接：<https://thunderstore.io/c/enter-the-gungeon/p/CaptainPretzel/Gunfig/>
- 作用：
  - 配置 API / 配置界面工具
- 对本项目的帮助：
  - 未来若要把随机规则做成游戏内配置，可考虑接入

