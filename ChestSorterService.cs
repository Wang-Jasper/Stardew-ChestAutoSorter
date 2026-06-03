using System.Text;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ChestAutoSorter;

/// <summary>Scans chests on a map, moves misplaced items, and sorts chest contents.</summary>
public sealed class ChestSorterService
{
    private readonly ModConfig _config;
    private readonly CategoryMapper _mapper;
    private readonly IMonitor _monitor;
    private readonly string _logPath;

    private const long MaxLogFileSize = 10L * 1024 * 1024; // 10 MB
    private const int MaxLogFileCount = 10;

    /// <summary>Special container QIDs that should never participate in sorting.</summary>
    private static readonly HashSet<string> ExcludedChestIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(BC)256",  // Junimo Chest (shared global inventory)
        "(BC)216",  // Mini-Fridge
        "(BC)248",  // Mini-Shipping Bin
        "(BC)275",  // Hopper (auto-feed)
    };

    /// <summary>
    /// Use the game's native capacity method. This correctly handles normal chests (36),
    /// big chests (70), and any capacity overrides from other mods via Harmony patches.
    /// </summary>
    private static int GetChestCapacity(Chest chest)
    {
        return chest.GetActualCapacity();
    }

    /// <summary>Count non-null items in a chest without LINQ allocation.</summary>
    internal static int CountNonNullItems(Chest chest)
    {
        int count = 0;
        for (int i = 0; i < chest.Items.Count; i++)
            if (chest.Items[i] != null) count++;
        return count;
    }

    public ChestSorterService(ModConfig config, CategoryMapper mapper, IMonitor monitor, string modDir)
    {
        _config = config;
        _mapper = mapper;
        _monitor = monitor;
        _logPath = Path.Combine(modDir, "sort-log.txt");
    }

    public void RunSort()
    {
        if (!Game1.IsMasterGame)
        {
            _monitor.Log("Aborted: not master game.", LogLevel.Trace);
            return;
        }

        if (!_config.EnableAutoSort)
        {
            _monitor.Log("Aborted: EnableAutoSort is false.", LogLevel.Trace);
            return;
        }

        // Only scan locations listed in config. Farm-type locations also scan building interiors.
        var chestsByColor = new Dictionary<string, List<Chest>>(StringComparer.OrdinalIgnoreCase);
        var scannedLocations = new HashSet<GameLocation>(ReferenceEqualityComparer.Instance);
        int totalChests = 0;

        foreach (string locName in _config.SortLocations)
        {
            var location = Game1.getLocationFromName(locName);
            if (location == null)
            {
                _monitor.Log($"Location '{locName}' not found, skipping.", LogLevel.Warn);
                continue;
            }

            if (!scannedLocations.Add(location)) continue;

            totalChests += CollectAndGroupChests(location, chestsByColor);

            // If this location is a Farm, also scan all building interiors (Cabin, Coop, Barn, Shed, etc.)
            if (location is StardewValley.Farm farmLoc)
            {
                foreach (var building in farmLoc.buildings)
                {
                    if (building?.indoors?.Value == null) continue;
                    var interior = building.indoors.Value;

                    if (!scannedLocations.Add(interior)) continue;
                    totalChests += CollectAndGroupChests(interior, chestsByColor);
                }
            }
        }

        if (chestsByColor.Count == 0)
        {
            _monitor.Log("No tagged chests found. Nothing to sort.", LogLevel.Warn);
            return;
        }

        // --- File log ---
        var log = new StringBuilder();
        log.AppendLine("========================================");
        log.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Sort started  (Day {Game1.Date.TotalDays}, {Game1.Date.Season} {Game1.Date.DayOfMonth}, Y{Game1.Date.Year})");
        log.AppendLine($"Total tagged chests: {totalChests}");
        log.AppendLine();

        AppendChestSnapshot(log, "BEFORE SORT", chestsByColor);

        var (moved, involved, failed) = MoveItems(chestsByColor);
        SortChestContents(chestsByColor);

        AppendChestSnapshot(log, "AFTER SORT", chestsByColor);

        log.AppendLine($"Result: moved {moved} items, {involved} source chests involved, {failed} failed.");
        log.AppendLine("========================================");
        log.AppendLine();

        WriteLog(log);

        _monitor.Log($"Sort done: {totalChests} chests, moved {moved} items, {failed} failed.", LogLevel.Info);
    }

    /// <summary>Append a full snapshot of all chests to the log builder.</summary>
    private static void AppendChestSnapshot(StringBuilder log, string label, Dictionary<string, List<Chest>> chestsByColor)
    {
        log.AppendLine($"--- {label} ---");
        foreach (var (colorName, chests) in chestsByColor)
        {
            foreach (var chest in chests)
            {
                int cap = GetChestCapacity(chest);
                int nonNull = CountNonNullItems(chest);
                log.AppendLine($"  [{colorName}] ({chest.TileLocation.X},{chest.TileLocation.Y}) Items: {nonNull}/{cap}  (Items.Count={chest.Items.Count})");
                for (int i = 0; i < chest.Items.Count; i++)
                {
                    var it = chest.Items[i];
                    if (it != null)
                        log.AppendLine($"    [{i}] {it.DisplayName} x{it.Stack}  (Cat={it.Category})");
                }
            }
        }
        log.AppendLine();
    }

    /// <summary>Write log to file with automatic rotation (up to <see cref="MaxLogFileCount"/> files).</summary>
    private void WriteLog(StringBuilder log)
    {
        try
        {
            // Rotate when the current log exceeds the size limit.
            if (File.Exists(_logPath))
            {
                var info = new FileInfo(_logPath);
                if (info.Length > MaxLogFileSize)
                    RotateLogFiles();
            }

            File.AppendAllText(_logPath, log.ToString());
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to write sort log: {ex.Message}", LogLevel.Warn);
        }
    }

    /// <summary>
    /// Rotate log files: sort-log.txt → sort-log.1.txt → sort-log.2.txt → … → sort-log.9.txt (deleted).
    /// </summary>
    private void RotateLogFiles()
    {
        string dir = Path.GetDirectoryName(_logPath)!;
        string name = Path.GetFileNameWithoutExtension(_logPath);
        string ext = Path.GetExtension(_logPath);

        // Delete the oldest file if it exists.
        string oldest = Path.Combine(dir, $"{name}.{MaxLogFileCount - 1}{ext}");
        if (File.Exists(oldest))
            File.Delete(oldest);

        // Shift: .8 → .9, .7 → .8, … .1 → .2
        for (int i = MaxLogFileCount - 2; i >= 1; i--)
        {
            string src = Path.Combine(dir, $"{name}.{i}{ext}");
            string dst = Path.Combine(dir, $"{name}.{i + 1}{ext}");
            if (File.Exists(src))
                File.Move(src, dst);
        }

        // Current → .1
        string first = Path.Combine(dir, $"{name}.1{ext}");
        File.Move(_logPath, first);
    }

    private int CollectAndGroupChests(GameLocation location, Dictionary<string, List<Chest>> result)
    {
        int total = 0;

        foreach (var obj in location.Objects.Values)
        {
            if (obj is not Chest chest) continue;
            if (!chest.playerChest.Value) continue;

            // Skip special containers (Junimo Chest, Mini-Fridge, Mini-Shipping Bin, Hopper, etc.)
            string? qid = chest.QualifiedItemId;
            if (!string.IsNullOrEmpty(qid) && ExcludedChestIds.Contains(qid))
                continue;

            string? color = _mapper.ResolveChestCategory(chest);
            if (color == null) continue;

            if (!result.TryGetValue(color, out var list))
            {
                list = new List<Chest>();
                result[color] = list;
            }
            list.Add(chest);
            total++;
        }

        if (total > 0)
            _monitor.Log($"[{location.Name}] Found {total} tagged chest(s).", LogLevel.Trace);

        return total;
    }

    private const string MiscGroupName = "DarkGrey";

    private (int moved, int involved, int failed) MoveItems(Dictionary<string, List<Chest>> chestsByColor)
    {
        int moved = 0, failed = 0;
        var involvedSet = new HashSet<Chest>();
        bool hasMiscGroup = chestsByColor.ContainsKey(MiscGroupName);

        // Collect first, move later — avoids modifying collections during iteration.
        var pending = new List<(Chest src, Item item, string targetColor)>();

        foreach (var (colorName, chests) in chestsByColor)
        {
            var accepted = _mapper.GetAcceptedCategories(colorName);
            if (accepted == null) continue;

            foreach (var chest in chests)
            {
                for (int i = chest.Items.Count - 1; i >= 0; i--)
                {
                    Item? item = chest.Items[i];
                    if (item == null) continue;

                    int cat = item.Category;
                    if (accepted.Contains(cat)) continue;

                    if (_mapper.TryGetTargetColor(cat, out string target)
                        && !string.Equals(target, colorName, StringComparison.OrdinalIgnoreCase)
                        && chestsByColor.ContainsKey(target))
                    {
                        pending.Add((chest, item, target));
                    }
                    else if (hasMiscGroup
                             && !string.Equals(colorName, MiscGroupName, StringComparison.OrdinalIgnoreCase))
                    {
                        pending.Add((chest, item, MiscGroupName));
                    }
                }
            }
        }

        foreach (var (src, item, target) in pending)
        {
            if (TryMoveItem(src, item, chestsByColor[target]))
            {
                moved++;
                involvedSet.Add(src);
            }
            else
            {
                failed++;
                _monitor.Log($"Failed to move '{item.DisplayName}' x{item.Stack} -> '{target}' (targets full).",
                             LogLevel.Warn);
            }
        }

        return (moved, involvedSet.Count, failed);
    }

    private bool TryMoveItem(Chest source, Item item, List<Chest> targets)
    {
        foreach (var target in targets)
        {
            if (ReferenceEquals(target, source)) continue;

            int added = TryAddItem(target, item);
            if (added <= 0) continue;

            if (added >= item.Stack)
                source.Items.Remove(item);
            else
                item.Stack -= added;

            return true;
        }
        return false;
    }

    /// <summary>
    /// Try to place an item (or part of it) into a chest.
    /// Returns the number of items actually added (0 = nothing moved).
    /// Only touches ONE existing stack or ONE empty slot per call — no scatter.
    /// </summary>
    private int TryAddItem(Chest chest, Item item)
    {
        int cap = GetChestCapacity(chest);
        int nonNullCount = CountNonNullItems(chest);

        // Refuse to add anything to a chest that is already at or over capacity.
        if (nonNullCount >= cap)
            return 0;

        // Strategy 1: Find a single existing stack that can accept some or all.
        foreach (var existing in chest.Items)
        {
            if (existing == null || !existing.canStackWith(item)) continue;

            int space = existing.maximumStackSize() - existing.Stack;
            if (space <= 0) continue;

            int toAdd = Math.Min(space, item.Stack);
            existing.Stack += toAdd;
            return toAdd;
        }

        // Strategy 2: Place into an empty slot (null hole or append).

        // Clone the item to avoid sharing the same reference between source and target chests.
        var clone = item.getOne();
        clone.Stack = item.Stack;

        // Try filling a null hole first.
        for (int i = 0; i < chest.Items.Count; i++)
        {
            if (chest.Items[i] == null)
            {
                chest.Items[i] = clone;
                return clone.Stack;
            }
        }

        // No null hole — append only if list size is within capacity.
        if (chest.Items.Count < cap)
        {
            chest.Items.Add(clone);
            return clone.Stack;
        }

        return 0;
    }

    private void SortChestContents(Dictionary<string, List<Chest>> chestsByColor)
    {
        foreach (var chests in chestsByColor.Values)
        {
            foreach (var chest in chests)
            {
                // Use the game's native organize logic (same as the in-game chest Organize button).
                ItemGrabMenu.organizeItemsInList(chest.Items);
            }
        }
    }
}
