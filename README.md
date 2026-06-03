# ChestAutoSorter

A [Stardew Valley](https://www.stardewvalley.net/) mod that automatically sorts farm chest items by chest dye color at the end of each day.

Built for [SMAPI](https://smapi.io/) 4.0.0+.

## Features

- **Color-based sorting** — dye your chests different colors, items are auto-routed to the matching chest
- **Misplaced item relocation** — items in the wrong chest get moved to the correct one automatically
- **In-chest sorting** — items inside each chest are organized using the game's native Organize logic
- **Big Chest support** — correctly handles 70-slot Big Chests and Big Stone Chests via `GetActualCapacity()`
- **Stack-aware merging** — stackable items merge into existing stacks before taking new slots; single-stack, no scatter
- **Catch-all Misc chest** — unmatched items go to a DarkGrey chest automatically, so nothing is left behind
- **Special container protection** — Junimo Chests, Mini-Fridges, Mini-Shipping Bins, and Hoppers are automatically excluded
- **Detailed sort logs** — before/after snapshots written to `sort-log.txt` with automatic rotation (10 MB × 10 files)
- **Multiplayer support** — host-side only; install on the host and all players benefit

## How It Works

1. At day end, the mod scans configured locations for player-owned chests
2. Each chest's dye color is resolved to a named color group via an RGB lookup table
3. Items that don't match their chest's accepted categories are moved to the correct color's chest
4. Items with no matching color group go to the DarkGrey (misc catch-all) chest
5. Each chest's contents are then organized using the native in-game Organize function
6. A full log with before/after snapshots is written to `sort-log.txt`

**Color resolution rules:**
- Chests in `ExcludedColors` are skipped entirely
- Chest colors with no entry in `ColorCategoryMap` are skipped
- Uncolored (brown) chests `RGB(0,0,0)` map to the `"Default"` group

## Sort Log Format

Each sort run appends a block to `sort-log.txt`:

```
========================================
[2026-06-03 14:30:05] Sort started  (Day 15, Spring 15, Y1)
Total tagged chests: 12

--- BEFORE SORT ---
  [Blue] (10,15) Items: 42/70  (Items.Count=70)
    [0] Tuna x5  (Cat=-4)
    [1] Sardine x12  (Cat=-4)
  [Green] (12,15) Items: 35/70  (Items.Count=70)
    [0] Parsnip x20  (Cat=-75)
    [1] Cauliflower x8  (Cat=-75)
  ...

--- AFTER SORT ---
  [Blue] (10,15) Items: 45/70  (Items.Count=70)
    [0] Sardine x17  (Cat=-4)
    [1] Tuna x5  (Cat=-4)
  ...

Result: moved 7 items, 3 source chests involved, 0 failed.
========================================
```

Logs auto-rotate: `sort-log.txt` → `sort-log.1.txt` → … → `sort-log.9.txt` (oldest deleted).

## Color → Category Mapping (Default)

Dye your chests to auto-sort items by type. All mappings are defaults and can be customized in `config.json` via `ColorCategoryMap`.

| Chest Color | Purpose | Category IDs | Accepted Items |
|---|---|---|---|
| **Blue** | Fishing | `-4`, `-21`, `-22` | Fish, Bait, Tackle |
| **Green** | Crops | `-75`, `-79`, `-80`, `-81`, `-74`, `-19` | Vegetables, Fruits, Flowers, Forage, Seeds, Fertilizer |
| **Pink** | Animal Products | `-5`, `-6`, `-14`, `-18` | Eggs, Milk, Animal Products, Cooking |
| **Yellow** | Artisan Goods | `-26`, `-27` | Artisan Goods, Syrup/Honey |
| **Orange** | Cooking | `-7`, `-25` | Cooked Food, Ingredients |
| **Purple** | Minerals | `-2`, `-12`, `-15` | Minerals, Gems, Metal Resources |
| **Teal** | Resources | `-16`, `-8`, `-20` | Building Resources, Crafting, Fishing Junk |
| **Red** | Combat | `-28`, `-102`, `-103` | Monster Loot, Weapons, Tools |
| **Default** (brown) | Equipment | `-24`, `-29`, `-95` ~ `-101` | Furniture, Decor, Hats, Boots, Rings, Clothing |
| **DarkGrey** | Misc | `0`, `-9` | Catch-all for unmatched items, Trash |

### Excluded Colors (Default)

These chest colors are never touched by the mod. Use them for chests you want to manage manually.

`White` · `LightBlue` · `Aqua` · `LimeGreen` · `LightOrange` · `DarkRed` · `LightPink` · `Magenta` · `DarkPurple` · `MediumGrey` · `LightGrey`

### Stardew Item Category Reference

Use these IDs when customizing `ColorCategoryMap`:

| ID | Category | ID | Category |
|---|---|---|---|
| `-2` | Minerals | `-22` | Tackle |
| `-4` | Fish | `-24` | Furniture |
| `-5` | Eggs | `-25` | Ingredients |
| `-6` | Milk | `-26` | Artisan Goods |
| `-7` | Cooking (recipes) | `-27` | Syrup / Honey |
| `-8` | Crafting | `-28` | Monster Loot |
| `-9` | Trash | `-29` | Decor |
| `-12` | Gems | `-74` | Seeds |
| `-14` | Animal Products | `-75` | Vegetables |
| `-15` | Metal Resources | `-79` | Fruits |
| `-16` | Building Resources | `-80` | Flowers |
| `-18` | Cooking (edible) | `-81` | Forage |
| `-19` | Fertilizer | `-95` ~ `-101` | Equipment (Hats, Boots, Rings, etc.) |
| `-20` | Fishing Junk | `-102` | Weapons |
| `-21` | Bait | `-103` | Tools |

## Installation

1. Install [SMAPI](https://smapi.io/) (4.0.0 or later)
2. Download the latest release from [Releases](../../releases)
3. Unzip the `ChestAutoSorter` folder into your `Stardew Valley/Mods/` directory
4. Launch the game through SMAPI — a `config.json` with defaults will be generated on first run

Your `Mods` folder should look like:

```
Mods/
└── ChestAutoSorter/
    ├── ChestAutoSorter.dll
    ├── manifest.json
    └── config.json
```

## Building from Source

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [SMAPI 4.0.0+](https://smapi.io/) installed
- Stardew Valley game files

### Steps

1. Clone this repository
2. Copy the required DLLs into the `lib/` folder:

   | DLL | Source |
   |---|---|
   | `StardewModdingAPI.dll` | SMAPI install directory |
   | `SMAPI.Toolkit.CoreInterfaces.dll` | SMAPI install directory |
   | `0Harmony.dll` | SMAPI install directory |
   | `Stardew Valley.dll` | Game install directory |
   | `StardewValley.GameData.dll` | Game install directory |
   | `MonoGame.Framework.dll` | Game install directory |
   | `xTile.dll` | Game install directory |

3. Build:

   ```bash
   dotnet build -c Release
   ```

4. The output will be in `deploy/` — copy the contents to your `Mods/ChestAutoSorter/` directory.

## Configuration

Edit `config.json` in the mod folder (auto-generated on first run):

| Setting | Default | Description |
|---|---|---|
| `EnableAutoSort` | `true` | Master switch for the mod |
| `SortOnDayEnd` | `true` | Trigger sorting at the end of each day |
| `SortLocations` | `["Farm", "FarmHouse"]` | Game locations to scan for chests |
| `ColorCategoryMap` | *(see above)* | Color name → accepted item category IDs |
| `ExcludedColors` | *(see above)* | Chest colors excluded from sorting |

## Compatibility

- **Stardew Valley**: 1.6+
- **SMAPI**: 4.0.0+
- **Multiplayer**: Fully supported (host-side mod, benefits all players)
- **Platform**: Windows, Linux, macOS
- **Other mods**: No known conflicts. Does not patch chest behavior — only reads/writes items.

### Protected Containers

These special containers are automatically excluded from sorting:

| Container | Reason |
|---|---|
| Junimo Chest `(BC)256` | Shared global inventory |
| Mini-Fridge `(BC)216` | Linked to specific farmhouse |
| Mini-Shipping Bin `(BC)248` | Container + shipping hybrid |
| Hopper `(BC)275` | Auto-feed machine |

## License

MIT
