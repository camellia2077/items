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

- Press the configured command-panel key to open or close the command panel. The default is `F7`; change `[UI] CommandPanelKey` in `etg-gameplay-dashboard.cfg` to another Unity `KeyCode` name such as `F8`, `Insert`, or `BackQuote`.
- The panel also supports opening from a configurable 360 controller shortcut. The default is `LB+R3`; `LB+X`, `LB+Y`, and `R3 (open 0.5s / close press)` are
  available from `Settings`.
- `Settings -> Controller` can disable the controller shortcut without disabling the keyboard command-panel key; it is enabled by default.
- The panel is positioned near the bottom center of the screen to avoid the top-left HUD.
- The UI uses a darker ETG-friendly color scheme with clearer text sizing.
- The command page can show a separate side stats panel with current player vitals and combat stats from `HealthHaver` and `PlayerStats`.
- Opening the panel does not automatically focus the command input field. Click the input field before typing a command.

### Current Gamepad Support

- Opening the command panel is supported from the configured 360 controller shortcut. Combination shortcuts trigger on the
  `R3` press while the modifier is held; standalone R3 opens after a 0.5-second hold and closes on press.
- The `Settings` page exposes the controller shortcut selector with `LB+R3`, `LB+X`, `LB+Y`, and `R3 (open 0.5s / close press)`.
- Basic gamepad navigation currently exists only on the `Command` and `Settings` pages.
- On those two pages, the current intended control scheme is:
  - d-pad style horizontal/vertical input moves focus
  - `A` confirms the focused button
  - `B` goes back or closes the current page
  - `LB` switches categories on the main `Command` page
- The General page Teleport list uses D-pad up/down to move its visible focused-floor highlight, `A` to teleport to the focused floor, and `B` to close the list. The list is single-column, so D-pad left/right does not change its selection.
- Text-entry interactions are still keyboard/mouse-first. Opening the panel from a gamepad does not make the command input field or pickup-browser search field practical to use from a controller yet.
- Subpages that rely on larger lists or more complex layouts, such as pickup browsing and editor-style pages, do not yet have full gamepad navigation support.

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
  In Grant mode, use the `Set Shortcuts` entry at the top of this page to enter shortcut setup. Each pickup row then has a keyboard shortcut button and a clear button. Click the shortcut button, press a keyboard key, and use the existing `Back` button to return to General; the assigned pickup is granted to the selected player when that key is pressed while the panel is closed. The command-panel key, arrow navigation keys, `Insert`, `Delete`, and the configured `room.rewind` key cannot be assigned to pickups. Keyboard shortcuts are persisted in `[UI] KeyboardShortcuts` as `targetId=KeyCode` pairs. Catalog pickups use numeric target IDs, while currency actions use names such as `currency.max_health`.
- `Start Items`
  Opens the start-items editor for the current rules file.
- `Rapid OFF` / `Rapid ON`
  Toggles hold-to-rapid-fire mode for the current gun.
- `Reload OFF` / `Reload Fast` / `Reload Anim`
  Cycles automatic reload between off, instant reload, and vanilla animated reload when the current gun's clip is empty and ammo is available. During a dual-wield synergy, an empty clip on either gun starts one shared reload operation for both guns, matching a manual reload input rather than independently reloading only the empty gun.
- `General -> Cursor Color`
  Opens the cursor color page. The page has an explicit enable/disable button and the color choices `Cyan`, `Lime`,
  `Yellow`, `Pink`, `Red`, `Orange`, `Electric Violet`, and `Electric Blue`. Selecting a color enables custom coloring.
  Disabling it restores the original cursor color; the original cursor remains drawn above the Control Panel by design.

### Combat Cursor Color Implementation

The selected color is stored as a stable preset ID under `[Combat] CursorColorPreset`. The catalog and user-facing HEX values are defined in
`src/EtgGameplayDashboard/CombatCursorColorCatalog.cs`:

| ID | Display name | HEX |
| --- | --- | --- |
| `preset_01` | Cyan / 青色 | `#00E5FF` |
| `preset_02` | Lime / 亮绿 | `#39FF14` |
| `preset_03` | Yellow / 金黄 | `#FFF000` |
| `preset_04` | Pink / 粉色 | `#FF1493` |
| `preset_05` | Red / 红色 | `#FF0000` |
| `preset_06` | Orange / 橙色 | `#FF8C00` |
| `preset_07` | Electric Violet / 电光紫 | `#9900FF` |
| `preset_08` | Electric Blue / 电光蓝 | `#0066FF` |

The cursor page is implemented in `InGameCommandController.CursorColor.cs`; its controller-focus order is maintained in
`InGameCommandController.State.cs`. `OFF` is represented by `CombatCursorColorCatalog.DisabledId`. When the page is disabled,
pressing the enable button selects the first catalog preset (`preset_01`); choosing any color directly also enables custom coloring.
Display names and HEX values are presentation data and must not be used as config values.

The runtime render path is owned by `Runtime/CommandPanelCursorRenderHooks.cs`. ETG's original `GameCursorController.OnGUI`
pass is suppressed only while custom coloring is enabled. The selected original cursor texture and its screen rectangle are
then drawn through a cached `UI/Default` material with `_Color` set to the selected color. This keeps the normal alpha blend
path and preserves the hollow/transparent portions of ETG cursor textures. Do not replace this with the tk2d tintable shader:
its blend behavior can draw RGB data from transparent pixels and change the cursor shape. If `UI/Default` cannot be found,
the renderer logs `Cursor color shader unavailable` and falls back to the original tinted draw path for diagnostics.
- `Stats OFF` / `Stats ON`
  Toggles the side player-stats panel.
- `God OFF` / `God ON`
  Toggles invincibility. When enabled, the player is kept non-vulnerable and protected from touch damage, pits, and status effects; disabling restores the values captured when the toggle was enabled.
- `Lang Auto` / `Lang EN` / `Lang CN`
  Cycles the command panel language preference through `auto`, `en`, and `zh-CN`, then persists it to `etg-gameplay-dashboard.cfg` under `[UI] Language`.
- `Settings`
  Opens panel preferences. The current settings page includes keyboard key selection, UI size, language, experimental-mode controls, and the About page.
  Display settings also include a persisted `ShowCommandPanelCloseButton` toggle, enabled by default, for showing or hiding the title-bar `×` close button. Hiding it does not disable the keyboard or controller close shortcuts.
- `Theme`
  Cycles the selected dashboard theme. The theme action is grouped with `Settings` and `Language` in the command-panel header.
- `Currency`
  Opens the resource-actions submenu.
- `Room`
  Opens the room-tools submenu.
- `Teleport`
  Opens a separate left-side floor picker. Each row loads one configured floor through the same vanilla level-load route as the console `load_level` flow.
- `Reveal Map`
  Toggles the map-reveal feature. `ON` runs the current-floor reveal pass and promotes teleporter-capable rooms toward a usable teleport state; `OFF` stops the feature. If enabled in the foyer, the foyer is revealed immediately and the first true dungeon floor is revealed automatically after it becomes ready. `Settings -> Display -> Reveal Map Mode` controls the dungeon scope: `Current Floor` performs that foyer-triggered reveal only on the first dungeon floor (or resets after a dungeon-floor change when enabled in a dungeon), while `Every Floor` keeps the feature enabled and runs the same pass when each new dungeon floor becomes ready. This is not the same thing as reproducing the exact vanilla minimap-discovery visuals for every room.

### Pickup Browser

- The `P1/P2/Both` target button is shared with `General -> Characters`. In Grant mode, pickup-browser grants are sent to the selected player, or to both players when `Both` is selected; selecting P2 or Both requires a second player to have joined.
- search matches `alias`, `internalName`, catalog display names, the active game-language display name, and `pickupId`
- in English UI, visible pickup names should prefer the catalog's `EnglishDisplayName`; search still includes both
  localized and English pickup names
- category filters support `All`, `Gun`, `Passive`, and `Active`
- quality filters are ordered `S`, `A`, `B`, `C`, `D`, with `Special`, `Excluded`, and all-quality options available
- when the `Gun` category is selected, gun-class filters support pistol, full-auto, shotgun, rifle, beam, charge, explosive, elemental, and special buckets
- when the `Passive` category is selected, passive subcategory filters support bullet-related items
- when the `Active` category is selected, cooldown filters support uses, damage, time, and room buckets
- clicking a result row or its `Grant` button grants the selected pickup directly
- clicking `Add` writes the selected pickup as a `specific` ID rule in the current start-items rules file
- duplicate `specific` ID rules are blocked by the in-game editor
- icons are reused from the game's live pickup sprites
- implementation details for pickup-browser icons and preset resource icons live in [UI Icon Reuse](./ui-icon-reuse.md)

### Start Items Editor

The Start Items editor opens on a preset preview list:

- clicking a preset row or `Open` selects that preset and opens its detail page
- clicking `Select` only selects that preset; it does not open the detail page
- preset rows show total, specific, and random rule counts
- `Add Item` / `添加物品`
  Available on the preset detail page. Opens the pickup browser in Start Items add mode. In this mode, clicking a row or `Add` writes the selected pickup to the active preset instead of granting it to the current run.
- `Reload Config` / `重新读取配置`
  Invalidates the cached resolved start-items config and manually re-reads the current rules file from disk on the next automatic grant.
- `New` / `新建`
  Creates an empty preset with an auto-generated unique name such as `preset` / `preset-2` in English or `预设` / `预设-2` in Chinese, selects it, and saves it as a separate preset file under `BepInEx\config\presets\`.
- `Copy` / `复制`
  Duplicates the active preset's rules into a new uniquely named preset, selects it, and saves it as a separate preset file under `BepInEx\config\presets\`.
- `Delete` / `删除`
  Deletes the active preset, then selects the next available preset. Deleting the only preset is blocked.
- `Rename` / `改名`
  Available on the preset preview page. Renames the active preset, updates `[StartItems] ActivePreset`, and blocks empty or duplicate names.
- in Chinese UI, the default `default` preset is renamed to `预设`; new presets then start from `预设-2` when `预设` already exists
- `Remove`
  Deletes the selected rule from the current active preset by its current row index.
- `Add` and `Remove` edit only the current active preset file under `BepInEx\config\presets\`
- rows include resolved pickup metadata such as quality, gun class, and active cooldown where available
- preset titles and preset item names use different localization paths: built-in preset titles come from
  `display_name_key`, while item rows are resolved from the live pickup catalog
- `Add` and `Remove` automatically invalidate the cached config and refresh the in-game editor list; the manual reload button is kept for edits made outside the game
- changes affect the next automatic start-of-run grant; they do not immediately grant or remove items in the current run
- preset pickup resource rows such as health, key, rat key, blank, and casings reuse ETG UI atlas sprites at runtime; see [UI Icon Reuse](./ui-icon-reuse.md)

### Currency Menu

In the `Currency` submenu:

- `+1 Key`
  Adds one key to the current player.
- `+1 Rat Key`
  Adds one rat key to the current player.
- `+100 Casings`
  Adds 100 casings to the current player.
- `Casings -> Clear`
  Clears the current run's casing balance only. Hegemony is unaffected.
- `+50 Hegemony`
  Adds 50 meta currency via `TrackedStats.META_CURRENCY`.
- `Hegemony -> Clear`
  Clears the entire saved Hegemony balance via the same global stat-clear path used by vanilla Breach-shop purchases. Casings are unaffected.
- `Player -> Character -> Stats`
  Contains integer inputs for Coolness and Curse. Values from 0 to 999 are accepted; applying Curse 0 clears the curse.
- `Player -> Character -> Pickups`
  Contains health, armor, blank, key, Rat Key, and casing actions. The `Target: P1/P2/Both` control applies to pickup grants and spawns.
- `Player -> Character -> Projectiles`
  Contains projectile size, projectile speed, reload speed, and spread controls. Reload speed accepts `0.25x` to `4.0x`; `1.0x` is normal and higher values reload faster. Spread values accept `0` to `999`; higher values increase projectile spread and `0` means no spread.
- `Player -> Combat`
  Contains Hold Rapid, Skip Charge, Auto Reload, Ammo Mode, invincibility, flight, and the other combat controls. Skip Charge is off by default and causes charged guns to fire at full charge without waiting. Combat toggle selections are persisted under `[Combat]` and restored after scene transitions and plugin restart; one-shot actions such as Full Ammo are not persisted.
- `Player -> Combat -> Controller Aim Lock`
  Toggles locking controller camera offset while maintaining right-stick aiming. Persisted under `[Combat] ControllerAimLockEnabled`.
- `Player -> Combat -> Keyboard Aim Assist`
  Cycles Keyboard Aim Assist modes (`Off`, `Auto Aim`, `Super Auto Aim`) and multipliers (`0.5x`, `1.0x`, `1.5x`, `2.0x`). Uses `PlayerController.DetermineAimPointInWorld` postfix, projectile lead-time prediction, and physics raycast obstacle filtering. The UI description displays base angle prompts (`Auto Aim` at 15°, `Super Auto Aim` at 25°) multiplied by the selected multiplier for effective lock-on cone calculation. Option names inherit vanilla ETG controller aim assist settings.
- `Player -> Combat -> Enemy HP Bars`
  Reuses Scouter's original health-bar prefab without granting Scouter or its damage/accuracy bonuses; damaging a normal enemy reveals its bar. The toggle state is persisted under `[Combat] EnemyHealthBarsEnabled` and restored on the next game launch.
- `Player -> Combat -> Skip Boss Intro`
  This control is available independently of `Settings -> Experimental Mode`. Its enabled state is persisted under `[Combat] BossIntroSkipEnabled`, so returning to the Breach and entering a new run keeps the same state.

### Room Menu

In the `Room` submenu:

- `NPC`
  Provides persistent Breach NPC unlocks for `Cadence & Ox`, `Professor Goopton`, and `Doug`. Goopton uses only the vanilla jailed-NPC PlayMaker `npcCellUnlocked` event from `NPC_Jellyfish_Jailed`, which sets `SHOP_GOOP_ACTIVE`; the popup explains that he must be found and purchased from in a dungeon shop before he appears in the Breach. Doug additionally sets the permanent `SHOP_HAS_MET_BEETLE` rescue flag, but his appearance remains occasional as in vanilla. The actions intentionally leave the vanilla "ever talked" flags unchanged so the original first-encounter dialogue can still run.
- `Enemies`
  Reserved for future enemy-spawn tools; it currently has no actions.
- `Rewind -> Rewind: ON/OFF`
  Controls whether standard and Boss rooms entered on the current floor are recorded for rewind. Turning it off immediately clears the current floor's saved room states and stops any active rewind.
- `Rewind -> Rewind Method`
  Cycles between `Rewind Room` (restores the same recorded enemy batches, types, and positions) and `Respawn Enemies` (generates enemies in the current room, but batches, types, and positions may differ). The Rewind section provides a keyboard shortcut editor with a clear button; while the command panel is closed, the configured key activates the selected method. Rewind mode requires `Rewind: ON`; if it is disabled, the shortcut shows a warning instead of rewinding. The shortcut is persisted as `room.rewind=KeyCode` in `[UI] KeyboardShortcuts`, with `C` as the default. Pickup and Rewind shortcuts cannot share a key.
- `Rewind -> Spawn`
  Executes the currently selected `Rewind Method` directly from the command panel; it is equivalent to the configured `C` shortcut.
  Rewound enemies are immediately marked as engaged because the player is already inside the room when they are spawned.
- Rewind is temporarily disabled while Boss Rush is active because using it can cause a game UI bug. The command shows a warning while the fix is in progress.
- `Rewind Room` requires the player to be physically inside a standard combat or Boss room's interior cells. `Respawn Enemies` is intended for standard combat rooms; when used in a Boss room, it automatically falls back to the exact Boss rewind path so the selected mode cannot block Boss replay. Both modes refuse connected corridors, exit cells, stale next-room selections, and rooms with no enemies; these conditions show a yellow warning and never seal a room or generate enemies.
- `Boss`
  Shows the Boss selected for the next generated floor and the Boss choices valid for that floor's tileset. In the foyer this targets the first floor; while playing, it targets the next floor. Selecting one sets the vanilla pre-generation Boss-room choice (`BossManager.PriorFloorSelectedBossRoom`), so the dungeon incorporates the target Boss when generating the next level (this avoids live map replacement, as differing room prototype geometries would cause severe map mesh corruption).
  Display names are read from `defaults/catalog/EtgGameplayDashboard.boss-names.json` (extracted from the game's `enemies.txt` text assets and keyed by vanilla Boss room prototype names). This static catalog bypasses repeated `AIActor` instantiation and `StringTableManager` lookups on every UI redraw, preventing UI frame stutter (note: baseline timing measurements were collected under a Debug build). If a custom or newly added room is missing from the catalog, the service safely falls back to the live game enemy actor API.
  Boss choices are shown first in one or more rows. No room-layout buttons are shown until a Boss is selected; for a Boss with multiple vanilla layouts, a second row group then exposes the room prototype choices. Selecting only the Boss uses its first vanilla room layout by default.
- `Spawn Gunber Muncher`
  Spawns the vanilla Gunber Muncher (常规吃枪怪) actor directly into the current room. This is a custom runtime spawn path, not a full recreation of the original muncher room.
- `Spawn Evil Muncher`
  Spawns the vanilla Evil Muncher (邪恶吃枪怪) actor directly into the current room. This follows the same non-standard runtime integration as Gunber Muncher, but resolves from a different vanilla asset bundle.
- chest tier buttons
  Selects the chest tier for the next spawn. Current v1 options are `Brown`, `Blue`, `Green`, `Red`, `Black`, `Synergy`, and `Rainbow`.
- `Spawn Chest`
  Spawns one unlocked chest of the selected tier in the current room near the player.

### Breach Shop Pickup Display Resolution

The nearby pickup information overlay has a generic `MetaShopController` compatibility path and a `BaseShopController` path for the three Breach NPC shops. The paths must remain distinct because they expose live shop stock through different controller structures.

#### Meta-shop compatibility path

Some ETG meta shops use `MetaShopController`. Their visible object may be an `ItemBlueprintItem`, whose own pickup ID and copied journal metadata describe the blueprint wrapper rather than the item currently sold. The resolver therefore:

- finds the `ShopItemController` index in `MetaShopController.m_itemControllers`
- reads the corresponding item ID from the current tier, then the proximate tier
- resolves that ID through `PickupObjectDatabase`

The mapping follows the controller order created by the vanilla meta-shop setup. It does not assume that a fixed screen position always represents the same item.

#### Breach NPC shops

Cadence & Ox, Professor Goopton, and Doug use the `BaseShopController` path. Their shops are identified by `baseShopType == FOYER_META`. The live stock is held in `BaseShopController.m_shopItems`, while each visible `ShopItemController` is parented to one of the shop's spawn-position transforms. The resolver therefore:

- verifies that the parent is a foyer meta shop
- finds the visible controller's spawn-position slot
- reads `m_shopItems[slot]`
- resolves the `PickupObject` from that live stock object

This is necessary because the stock can be refreshed or replaced, and the item displayed in a slot is not reliably identified by the blueprint wrapper or by a hard-coded visual position.

The implementation is in `src/EtgGameplayDashboard/Etg/EtgPickupResolver.Catalog.cs`:

- `ResolveMetaShopSlotPickup` provides compatibility for other `MetaShopController` shops.
- `ResolveFoyerMetaShopSlotPickup` and `FindShopSlotIndex` handle the three Breach NPC shops.

### Teleport Picker

The left-side `Teleport` picker loads these floors:

- `load_level keep`: Keep / Chamber 1
- `load_level oubliette`: Oubliette / Chamber 1.5 / Sewer
- `load_level proper`: Proper / Chamber 2
- `load_level abbey`: Abbey / Chamber 2.5 / Old King
- `load_level mine`: Mines / Chamber 3
- `load_level ratden`: Rat Den / Chamber 3.5
- `load_level hollow`: Hollow / Chamber 4
- `load_level R&G_Dept`: R&G Dept / Chamber 4.5
- `load_level forge`: Forge / Chamber 5
- `load_level heli`: Bullet Hell / Chamber 6

The picker refuses normal teleports while Boss Rush is active so the Boss Rush state machine does not conflict with manual floor loads.

Implementation notes:

- Gamepad shortcut detection and controller-navigation state live in `src/EtgGameplayDashboard/Commands/InGameCommandController.cs`, `src/EtgGameplayDashboard/Commands/InGameCommandController.State.cs`, and the shared `src/EtgGameplayDashboard/Commands/InGameCommandController.Navigation.cs` partial.
- Main command-page controller focus and button routing live in `src/EtgGameplayDashboard/Commands/InGameCommandController.CommandPage.cs` and `src/EtgGameplayDashboard/Commands/InGameCommandController.Navigation.cs`.
- Page-specific controller focus is split into matching responsibility partials, including `InGameCommandController.Settings.cs`, `InGameCommandController.LoadoutEditor.Navigation.cs`, `InGameCommandController.PickupBrowser.Navigation.cs`, and `InGameCommandController.Room.BossSelection.cs`.
- Keyboard/controller input diagnostics live in `src/EtgGameplayDashboard/Commands/InGameCommandController.InputDiagnostics.cs`.
- Teleport buttons live in `src/EtgGameplayDashboard/Commands/InGameCommandController.Teleport.cs`.
- Floor-token-to-scene mapping lives in `src/EtgGameplayDashboard/Runtime/EtgFloorSceneResolver.cs`.
- The flow intentionally follows the same high-level route as upstream `load_level` behavior:
  `Foyer.Instance.OnDepartedFoyer()` first when leaving the Breach, then `GameManager.Instance.LoadCustomLevel(sceneName)`.
- Do not assume every floor uses a simple `tt_` scene prefix. The Rat Den is the important exception in the current picker:
  `load_level ratden` must resolve to `ss_resourcefulrat`, not `tt_resourcefulrat`.
- The Rat Den mapping was verified against upstream `Load-Level` / `ETGModConsole` behavior. Using the wrong scene name can appear to "work" while actually loading the wrong floor or failing mid-transition.
- When changing teleport mappings, always verify the actual destination in-game and re-check the BepInEx log after testing.

### Reveal Map

The `General` page now also exposes `Reveal Map`.

High-level behavior:

- `Reveal Map`
  Pushes the current floor through the minimap reveal path and also promotes already-registered teleporter rooms toward a usable teleport state.

Important caveat:

- `Reveal Map` reflects the gameplay result more than the exact minimap presentation. A room can become teleport-usable without fully matching the natural "player walked into this room" minimap state.

Implementation reference:

- [Map Reveal And Teleporter Promotion](./map-teleport.md)

### Boss Rush Page

On the `Boss Rush` page:

- `Start Boss Rush`
  Starts an independent boss-rush run from the character-select hub.
- `Return to Character Select`
  Aborts an active Boss Rush and returns to character select.

Current Boss Rush v1 behavior:

- starts only in the character-select hub
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
  The `General -> Characters -> Character Target: P1/P2` button selects which joined player is replaced.
  P2 selection requires a second player to have joined; otherwise the operation is rejected.
  `The Cultist` (邪教徒) is available as a switch-only character.

### Configurable Start Loadout

The automatic start-of-run loadout uses:

- `etg-gameplay-dashboard.cfg`
  simple on/off switches
- `EtgGameplayDashboard.rules.json5`
  Start Items config anchor and fallback entry point
- `EtgGameplayDashboard.aliases.json5`
  shared alias definitions for rules and commands

Current minimum behavior:

- `EnableRandomLoadout` controls whether automatic start-of-run grants happen
- rules support:
  - `random`
  - `specific`
- the in-game loadout editor writes preset edits back into the active file under `BepInEx\config\presets\`
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

- [Muncher Spawn](./runtime-internals/muncher-spawn.md)
- [Localization And Language Switching](./localization.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)

### Runtime Notes

- command execution logs are tagged with `[EtgGameplayDashboard][Command]`
- the command panel is intentionally a compact debug and experimentation surface, not a full console
- Boss Rush entry and return actions touch ETG runtime hotspots and should be treated as manual-verify areas
- invincibility uses ETG runtime damage flags (`HealthHaver.IsVulnerable`, `HealthHaver.PreventAllDamage`, and player immunity flags), so verify it in combat before release
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
