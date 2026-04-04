# 实现建议与后续步骤

最后整理时间：2026-03-25

本页记录基于现有资料做出的工程判断，不是对某一篇外部文档的逐字摘录。

## 第一版建议的最小方案

建议先做一个最小可运行插件：

- 一个 `net35` 的 C# 项目
- 继承 `BaseUnityPlugin`
- 依赖：
  - `BepInEx`
  - `ModTheGungeonAPI`
  - `Assembly-CSharp`
  - `UnityEngine`
- 第一版只实现：
  - 插件启动日志
  - 新 run 开始时只执行一次
  - 从白名单中随机挑选基础游戏已有物品发给玩家

## 当前最值得优先验证的代码点

### 触发点

需要确认：

- “新 run 开始且只执行一次”的最佳事件或 Hook 点是什么

优先搜索词：

- `ETGModMainBehaviour.WaitForGameManagerStart`
- `GameManager`
- `OnNewLevelFullyLoaded`
- `new run`

### 发物品调用链

需要确认：

- 给玩家添加枪械、被动、主动道具时，最稳妥的调用链是什么

优先搜索词：

- `PlayerController`
- `PickupObjectDatabase`
- `LootEngine.TryGivePrefabToPlayer`
- `AcquirePassiveItem`
- `AddGunToInventory`

## 随机方式建议

建议先用 `System.Random(seed)`：

- 固定 seed：便于调试和复现
- 日期 seed：可做每日随机开局
- 每 run 新 seed：可做常规伪随机

第一阶段不建议一开始就上复杂 RNG 体系。

## 随机池建议

第一版不要直接全物品池随机，因为会带来：

- 剧情或测试物品
- 平衡破坏过大
- 某些仅应在特殊流程出现的内容

更稳妥的方式：

- 手动维护基础枪白名单
- 手动维护被动白名单
- 手动维护主动白名单

## 实现顺序建议

1. 创建最小 `BepInEx` 插件工程
2. 跑通日志，确认 DLL 已被游戏加载
3. 用 `ILSpy` 打开 `Assembly-CSharp.dll`
4. 找到“新 run 开始且只触发一次”的最佳入口
5. 整理一份小型随机白名单
6. 实现随机发枪 / 发被动 / 发主动
7. 记录日志：
   - 本局 seed
   - 抽到的物品 ID
   - 抽到的物品名
8. 最后再考虑配置文件或游戏内配置界面

## 决策优先级

推荐按这个顺序做判断：

1. 先找现成事件
2. 找不到理想事件，再上 `HarmonyX` 或 `RuntimeDetour`
3. 先发基础游戏已有物品
4. 以后再扩展到自定义物品或 UI 配置

## 当前未完全落地的问题

- 最佳“新 run 开始”触发点到底是哪一个方法或事件
- 给玩家添加基础游戏枪械的最佳 API 调用链
- 主动道具和被动道具是否应走不同发放流程
- 是否需要对合作模式或特殊角色做额外判断

这些问题已经不需要重新搜索生态资料，下一步应当进入本地反编译与最小实验阶段。

