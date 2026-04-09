# Pickup Grant Strategy

## 背景

`RandomLoadout` 既需要在 `F7` 命令面板里即时发放物品，也需要在开局规则链路里稳定发放配置好的枪械、被动与主动道具。

早期实现更偏向项目本地直连调用：

* `Gun` 使用 `AddGunToInventory(...)`
* `Passive` 使用 `AcquirePassiveItem(...)`
* `Active` 使用 `LootEngine.TryGivePrefabToPlayer(...)`

这条路线可以工作，但会带来两个持续问题：

* 物品字符串输入过度依赖 `displayName`，容易受本地化文本、运行时注册状态和重名条目影响。
* 不同类别的 grant 路径分散，排查“为什么这个物品能发、那个物品不能发”时缺少统一主路径。

与此同时，`ModTheGungeonAPI` 的 `give` 更偏向：

* 使用稳定的物品标识进行查找
* 使用 `LootEngine.TryGivePrefabToPlayer(...)` 作为统一运行时发放路径

这条路线对调试与跨环境一致性更友好。

## 当前策略

当前策略是：

* 在物品查找上，优先向 `ModTheGungeonAPI give` 靠拢。
* 在物品发放上，以 `LootEngine.TryGivePrefabToPlayer(...)` 为主路径。
* 保留分类专用 fallback，避免为了“完全一致”牺牲实际成功率。

换句话说，当前按钮和规则语义优先追求：

* 先稳定找到正确物品
* 再优先走统一 grant 路径
* 如果统一路径失败，再回退到项目本地的类别级 grant 方式

## 具体实现口径

### 物品查找

字符串输入与规则字符串项当前统一按以下顺序解析：

* `pickupId`
* `alias`
* `internalName`
* `displayName`

其中：

* `internalName` 是推荐字符串输入方式
* `displayName` 仅保留为兼容回退，不再视为主路径

### 物品发放

当前 grant 路径是：

* 主路径：`LootEngine.TryGivePrefabToPlayer(...)`
* 回退路径：
  * `Gun` 回退 `AddGunToInventory(...)`
  * `Passive` 回退 `AcquirePassiveItem(...)`
  * `Active` 继续依赖 `LootEngine.TryGivePrefabToPlayer(...)`

### 日志

当前 grant 日志会显式输出：

* `Path=primary`
* `Path=fallback`

并附带 `Detail=...`，用于区分这次 grant 究竟是按统一主路径成功，还是走了兼容兜底。

## 为什么采用这套策略

* 稳定性更高：`pickupId` / `internalName` 比 `displayName` 更少受运行时文本环境影响。
* 调试成本更低：统一主路径后，更容易判断问题是“查找失败”还是“grant 失败”。
* 兼容性更强：保留 fallback 后，不需要把项目正确性完全押在单一 API 行为上。
* 与上游更一致：对外输入语义和主要 grant 思路更接近 `ModTheGungeonAPI give`，降低理解和迁移成本。

## 取舍

该策略的明确取舍是：

* 优点：
  * 输入语义更稳定
  * grant 结果更易观察
  * 行为更接近社区常见实现
* 缺点：
  * 不是对 `ModTheGungeonAPI give` 的逐行复刻
  * 仍然保留项目本地 fallback，因此实现层比“纯单一路径”更复杂

## 后续演进条件

如果未来要进一步收紧为“只保留统一主路径”，至少需要先满足以下条件：

* 在常见枪械、被动、主动样本上验证 `LootEngine.TryGivePrefabToPlayer(...)` 单独使用的成功率足够稳定。
* 确认不存在必须依赖 `AddGunToInventory(...)` 或 `AcquirePassiveItem(...)` 才能正常加入玩家状态的高频物品类别。
* 保留足够清晰的日志，能在移除 fallback 后快速定位具体失败对象。
