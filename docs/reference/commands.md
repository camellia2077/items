# Commands

This page covers the current in-game command panel behavior.

## Command Panel

- Press `F7` to open or close the command panel
- The panel is positioned near the bottom center of the screen to avoid the top-left HUD
- The UI uses a darker ETG-friendly color scheme with clearer text sizing

## Supported Commands

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

Name matching is exact primary display name, case-insensitive.
If the value starts with a number, the command treats that leading number as the pickup ID.
If the value is not an ID, the command resolves `alias` first and `name` second.
This means pasted values like `gun 541 Casey Baseball_Bat_Gun` also work.

## Buttons

- `Grant`
  Executes the typed command
- `Random`
  Grants one random supported pickup without requiring text input
- `Rapid OFF` / `Rapid ON`
  Toggles hold-to-rapid-fire mode. When enabled, semi-automatic gun modules are temporarily treated as automatic while active, so holding left mouse can match rapid clicking speed.

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
  - `name`: exact primary display name, case-insensitive
  - `alias`: shared alias from `RandomLoadout.aliases.json5`
  - `id`: numeric pickup ID from `RandomLoadout.pickups.txt`
- `random` rules support either or both:
  - `pool`: display name list
  - `poolAliases`: alias list
  - `poolIds`: numeric pickup ID list
- when `specific` contains multiple references, resolution priority is `id` -> `alias` -> `name`
- when `random` contains multiple pool sources, resolution priority is `poolIds` -> `poolAliases` -> `pool`
- `RandomLoadout.pickups.txt` is the exported lookup index for `Category`, `ID`, `DisplayName`, and internal name
- `RandomLoadout.aliases.json5` is the shared readable alias layer that points back to `pickupId`

The default generated rule file currently starts with:

- `Casey` via gun ID `541`
- `Eyepatch` via passive ID `118`

The default alias file currently starts with:

- `casey_bat` -> `541`
- `casey_nail` -> `616`
- `eyepatch` -> `118`

This is intended as a strong and flavorful first preset rather than a fully randomized default.
