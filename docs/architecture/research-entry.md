# Enter the Gungeon 随机开局 Mod 研究入口

最后整理时间：2026-03-25

这份文件现在作为研究入口和摘要使用，详细内容已拆分到 `docs/architecture/research/`，避免单文件继续膨胀。

## 快速结论

- 项目形态：优先做 `BepInEx` 插件，不走外部内存修改器路线。
- 推荐语言：`C#`
- 推荐目标框架：`.NET Framework 3.5`
- 推荐基础依赖：`BepInEx 5`、`ModTheGungeonAPI`
- 推荐工具：`ILSpy`，调试可选 `dnSpyEx`
- Hook 方案：优先找现成事件，不够再用 `HarmonyX` 或 `MonoMod.RuntimeDetour`
- 第一阶段目标：在新 run 的合适时机，随机给玩家基础游戏已有的枪械、被动、主动道具

## 阅读导航

- 总导航：[README.md](../README.md)
- 项目目标与约束：[research/project-scope.md](./research/project-scope.md)
- 技术栈与选型结论：[research/stack-and-tooling.md](./research/stack-and-tooling.md)
- 外部资料索引与用途说明：[research/reference-index.md](./research/reference-index.md)
- 实现建议与后续步骤：[research/implementation-guidance.md](./research/implementation-guidance.md)

## 当前最重要的判断

- `ETGMod / Mod the Gungeon` 已弃用，新项目应以 `BepInEx` 为基线。
- 第一版不需要自定义物品注册、自定义美术或资源管线，只需要把基础游戏已有内容发给玩家。
- 当前最值得尽快验证的代码问题只有两个：
  - “新 run 开始且只执行一次”的最佳触发点
  - 给玩家添加基础游戏枪械 / 道具的最佳 API 调用链

## 当前优先搜索词

- `ETGModMainBehaviour.WaitForGameManagerStart`
- `GameManager`
- `PlayerController`
- `PickupObjectDatabase`
- `LootEngine.TryGivePrefabToPlayer`
- `AcquirePassiveItem`
- `AddGunToInventory`
- `OnNewLevelFullyLoaded`
- `new run`
