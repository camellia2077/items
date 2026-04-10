# Commands

This page covers the current in-game command panel behavior.

## Command Panel

- Press `F7` to open or close the command panel
- The panel is positioned near the bottom center of the screen to avoid the top-left HUD
- The UI uses a darker ETG-friendly color scheme with clearer text sizing

## Supported Commands

- `<name>`
- `<alias>`
- `<id>`
- `gun <name>`
- `gun <alias>`
- `gun <id>`
- `passive <name>`
- `passive <alias>`
- `passive <id>`
- `active <name>`
- `active <alias>`
- `active <id>`
- `item <name>`
- `item <alias>`
- `item <id>`

Command lookup now follows a `ModTheGungeonAPI give`-style order:
`id` -> `alias` -> `internalName` -> `displayName`.
All lookup inputs are case-insensitive.
If the value starts with a number, the command treats that leading number as the pickup ID.
Internal pickup identifiers such as `platinumbullets` are the recommended string form.
Display name input remains supported as a compatibility fallback, but it is less stable because it depends on runtime-localized strings.
If the input does not start with a known target such as `gun`, `passive`, `active`, or `item`, the whole input is treated as an `item` / `any` lookup.
This means pasted values like `gun 541 casey baseball_bat_gun` still work because the leading ID wins.

## Implementation Notes

- Item lookup and grant behavior are intentionally aligned with `ModTheGungeonAPI give` where practical.
- Implementation reference:
  - [`modthegungeonapi.md`](./modthegungeonapi.md)
- Project strategy decision:
  - [`pickup-grant-strategy.md`](../decisions/pickup-grant-strategy.md)

## Buttons

- `Grant`
  Executes the typed command
- `Random`
  Grants one random supported pickup without requiring text input
- `Pickups`
  Opens a small in-game pickup browser with search, category filters, and runtime sprite icons from the live game data
- `Rapid OFF` / `Rapid ON`
  Toggles hold-to-rapid-fire mode. When enabled, semi-automatic gun modules are temporarily treated as automatic while active, so holding left mouse can match rapid clicking speed.
- `Currency`
  Opens a secondary menu for resource actions.

## Pickup Browser

- Search matches `alias`, `internalName`, `displayName`, and `pickupId`
- Category filters support `All`, `Gun`, `Passive`, and `Active`
- Clicking a result row or its `Grant` button grants the selected pickup directly
- Icons are reused from the game's runtime pickup sprites; no separate icon pack is bundled with the mod

## Currency Menu

In the `Currency` submenu:

- `+1 Key`
  Adds one key to the current player.
- `+50 Casings`
  Adds 50 casings to the current player.
- `+10 Hegemony`
  Adds 10 Breach meta currency (`TrackedStats.META_CURRENCY`).

## Character Page Modes

In the `Characters` page, the mode button controls what happens when you click a character:

- `Mode: Unlock`
  Tries to unlock the clicked hidden character in save data.
  `Robot` is excluded from unlock mode in this panel.
- `Mode: Switch Only`
  Performs character switching only, without writing unlock flags.
  This mode is intended for immediate in-session switching behavior.

## Notes

- command execution logs are tagged with `[RandomLoadout][Command]`
- the command panel is intended as a minimum useful debug and experimentation tool, not yet a full in-game console

## Configurable Start Loadout

The automatic start-of-run loadout now uses:

- `randomgun.randomloadout.cfg`
  simple on/off switches
- `RandomLoadout.rules.json5`
  loadout rule definitions
- `RandomLoadout.aliases.json5`
  shared alias definitions for rules and commands

Current minimum behavior:

- `EnableRandomLoadout` in `randomgun.randomloadout.cfg` controls whether automatic start-of-run grants happen
- rules in `RandomLoadout.rules.json5` support:
  - `random`
  - `specific`
- `specific` rules support either:
  - `name`: string lookup using `internalName` first and `displayName` as a fallback
  - `alias`: shared alias from `RandomLoadout.aliases.json5`
  - `id`: numeric pickup ID from `RandomLoadout.pickups.txt`
- `random` rules support either or both:
  - `pool`: string list resolved as `internalName` first and `displayName` second
  - `poolAliases`: alias list
  - `poolIds`: numeric pickup ID list
- when `specific` contains multiple references, resolution priority is `id` -> `alias` -> `internalName` -> `displayName`
- when `random` contains multiple pool sources, resolution priority is `poolIds` -> `poolAliases` -> `pool`, and string pool entries use `internalName` before `displayName`
- `RandomLoadout.pickups.txt` is the exported lookup index for `Category`, `ID`, `DisplayName`, and internal name
- `RandomLoadout.aliases.json5` is the shared readable alias layer that points back to `pickupId`

The default generated rule file currently starts with:

- `541` for `casey_bat`
- `118` for `eyepatch`

The default alias file currently starts with:

- `casey_bat` -> `541`
- `casey_nail` -> `616`
- `eyepatch` -> `118`

This is intended as a strong and flavorful first preset rather than a fully randomized default.
