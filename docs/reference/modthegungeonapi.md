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

Related local docs:

* Strategy decision:
  * [`../decisions/character-switch-strategy.md`](../decisions/character-switch-strategy.md)
* Command panel behavior:
  * [`./commands.md`](./commands.md)

Related local code:

* Character switch service:
  * [`../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs`](../../src/RandomLoadout/Commands/FoyerCharacterSwitchService.cs)

## Usage Notes

* Prefer using upstream examples as behavioral references, not drop-in guarantees.
* Always validate behavior in the live ETG runtime after adopting a pattern.
* For persistence-sensitive logic, treat save-slot behavior and restart validation as required checks.
