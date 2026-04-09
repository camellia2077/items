# ModTheGungeonAPI Reference

This page tracks practical `ModTheGungeonAPI` references used by `RandomLoadout` development.
It is intentionally curated for day-to-day implementation work, not a full mirror of upstream docs or source.

## Primary Upstream

* Repository: [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI)
* Main console implementation: [`ETGModConsole.cs`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs)

## Verified Useful References

### Character Switch Flow

* `SwitchCharacter` implementation (Breach/runtime character replacement flow):
  * [`ETGModConsole.cs#L1942`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs#L1942)
* Practical value for this project:
  * Useful as a reference for force-switch behavior when normal character select flag routes are unstable.

### Give Command Flow

* `give` / `GiveItem` implementation:
  * `PlayerControllerExt.GiveItem(this PlayerController player, string id)`
  * Internally uses `LootEngine.TryGivePrefabToPlayer(...)` with the item prefab looked up from the game item registry.
* Practical value for this project:
  * Useful as the primary behavioral reference for runtime item grant flow.
  * Useful as the reason `RandomLoadout` now prefers `pickupId` / `internalName` over localized display names for command and rule input.

### Runtime Object Unlock Pattern

* `ForceUnlock` usage example:
  * [`ETGModConsole.cs#L1746`](https://github.com/SpecialAPI/ModTheGungeonAPI/blob/main/ModTheGungeonAPI/ETGMod/ETGGUI/ETGModConsole.cs#L1746)
* Practical value for this project:
  * Shows a common unlock call shape in mod code paths.
  * Should be treated as an object unlock reference, not as a guaranteed persistent character-unlock recipe.

## Related External Reference

Although separate from `ModTheGungeonAPI`, this repository is frequently relevant when discussing save/flag persistence:

* `SaveAPI` repository:
  * [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
* Save hook reference:
  * [`SaveAPIManager.cs#L417`](https://github.com/SpecialAPI/SaveAPI/blob/main/SaveAPIManager.cs#L417)

## How We Use This In RandomLoadout

Current `RandomLoadout` behavior for `Robot` prioritizes reliable in-session character switching over persistent unlock writes.

Current `RandomLoadout` behavior for item grant and lookup is:

* Character switching:
  * Uses `ETGModConsole.SwitchCharacter` as a behavioral reference, but the project currently relies on its own force-switch path in `Breach` when native character-select callback routes are unstable or have side effects.
* Item grant:
  * Uses `ModTheGungeonAPI give` as the primary runtime reference.
  * Prefers `LootEngine.TryGivePrefabToPlayer(...)` as the main grant path.
  * Keeps category-specific fallback calls such as `AddGunToInventory(...)` and `AcquirePassiveItem(...)` as project-level compatibility backups when the prefab path is not sufficient.
* Pickup lookup:
  * Prefers `pickupId` and `internalName` first, then uses `displayName` only as a compatibility fallback.
  * This is intentionally closer to `ModTheGungeonAPI give`, which is more stable when localized display names vary across runtime environments.

Related local docs:

* Strategy decision:
  * [`../decisions/character-switch-strategy.md`](../decisions/character-switch-strategy.md)
* Pickup grant strategy:
  * [`../decisions/pickup-grant-strategy.md`](../decisions/pickup-grant-strategy.md)
* Command panel behavior:
  * [`./commands.md`](./commands.md)

Related local code:

* Character switch service:
  * [`../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs`](../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs)
* Pickup resolver:
  * [`../../src/RandomLoadout/Etg/EtgPickupResolver.cs`](../../src/RandomLoadout/Etg/EtgPickupResolver.cs)
* Pickup granter:
  * [`../../src/RandomLoadout/Etg/EtgPickupGranter.cs`](../../src/RandomLoadout/Etg/EtgPickupGranter.cs)

## Usage Notes

* Prefer using upstream examples as behavioral references, not drop-in guarantees.
* Always validate behavior in the live ETG runtime after adopting a pattern.
* For persistence-sensitive logic, treat save-slot behavior and restart validation as required checks.
