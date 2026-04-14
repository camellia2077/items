# Commands

Use this page when you need the current in-game command panel behavior, supported actions, and developer notes for changing that surface.

## Must Read First

Before changing command UI, pickup grant behavior, Boss Rush entry flow, or character-select-hub UI actions, read:

1. [Start Here](../getting-started/start-here.md)
2. [Terminology And Naming](./terminology.md)
3. [Runtime Hotspots](../architecture/runtime-hotspots.md)
4. [Testing Matrix](./testing-matrix.md)

## Player-Facing Behavior

### Command Panel

- Press `F7` to open or close the command panel.
- The panel is positioned near the bottom center of the screen to avoid the top-left HUD.
- The UI uses a darker ETG-friendly color scheme with clearer text sizing.

### Supported Typed Commands

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

Lookup behavior:

- resolution order is `id -> alias -> internalName -> displayName`
- all lookup inputs are case-insensitive
- if the value starts with a number, the leading number is treated as `pickupId`
- if no known target prefix is present, the input is treated as `item` / `any`

Recommended input style:

- prefer internal names such as `platinumbullets`
- use aliases when you want stable shorthand
- use display names only as a compatibility fallback

### Main Buttons

- `Grant`
  Executes the typed command.
- `Random`
  Grants one random supported pickup without requiring text input.
- `Boss Rush`
  Opens the Boss Rush page.
- `Pickups`
  Opens the in-game pickup browser with search, category filters, and runtime sprite icons.
- `Rapid OFF` / `Rapid ON`
  Toggles hold-to-rapid-fire mode for the current gun.
- `Currency`
  Opens the resource-actions submenu.

### Pickup Browser

- search matches `alias`, `internalName`, `displayName`, and `pickupId`
- category filters support `All`, `Gun`, `Passive`, and `Active`
- clicking a result row or its `Grant` button grants the selected pickup directly
- icons are reused from the game's live pickup sprites

### Currency Menu

In the `Currency` submenu:

- `+1 Key`
  Adds one key to the current player.
- `+50 Casings`
  Adds 50 casings to the current player.
- `+10 Hegemony`
  Adds 10 meta currency via `TrackedStats.META_CURRENCY`.

### Boss Rush Page

On the `Boss Rush` page:

- `Start Boss Rush`
  Starts an independent boss-rush run from the character-select hub.
- `Return to Character Select`
  Aborts an active Boss Rush and returns to character select.

Current Boss Rush v1 behavior:

- starts only in the character-select hub (`tt_foyer`, commonly called the Breach)
- uses the fixed floor order `Keep -> Proper -> Mines -> Hollow -> Forge -> Hell`
- loads each vanilla floor, then routes the player toward the boss encounter
- waits for a boss reward claim before loading the next floor
- returns to character select on death or after clearing Hell

### Character Page Modes

In the `Characters` page:

- `Mode: Unlock`
  Tries to unlock the clicked hidden character in save data.
  `Robot` is excluded from unlock mode in this panel.
- `Mode: Switch Only`
  Performs immediate character switching without writing unlock flags.

### Configurable Start Loadout

The automatic start-of-run loadout uses:

- `randomgun.randomloadout.cfg`
  simple on/off switches
- `RandomLoadout.rules.json5`
  loadout rule definitions
- `RandomLoadout.aliases.json5`
  shared alias definitions for rules and commands

Current minimum behavior:

- `EnableRandomLoadout` controls whether automatic start-of-run grants happen
- rules support:
  - `random`
  - `specific`
- `specific` rules support:
  - `name`
  - `alias`
  - `id`
- `random` rules support:
  - `pool`
  - `poolAliases`
  - `poolIds`

Resolution priority:

- `specific`: `id -> alias -> internalName -> displayName`
- `random`: `poolIds -> poolAliases -> pool`

## Developer Notes

### Implementation References

- [ModTheGungeonAPI Reference](./modthegungeonapi.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)

### Runtime Notes

- command execution logs are tagged with `[RandomLoadout][Command]`
- the command panel is intentionally a compact debug and experimentation surface, not a full console
- Boss Rush entry and return actions touch ETG runtime hotspots and should be treated as manual-verify areas
- character-select-hub actions must not assume scene token meaning equals gameplay-state meaning

### After Editing This Surface

At minimum:

- run the checks from [Testing Matrix](./testing-matrix.md)
- if runtime behavior changed, run [Smoke Checklist](../operations/smoke-checklist.md)
- review [Logging](../operations/logging.md) after testing

## Read Next

- Runtime terminology:
  [./terminology.md](./terminology.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
- Testing expectations:
  [./testing-matrix.md](./testing-matrix.md)
