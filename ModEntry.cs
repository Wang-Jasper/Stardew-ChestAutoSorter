using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ChestAutoSorter;

public sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    private CategoryMapper _mapper = null!;
    private ChestSorterService _sorter = null!;

    // // Chat command polling state.
    // private object? _lastProcessedMsg;
    // private readonly StringBuilder _chatSb = new();

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _mapper = new CategoryMapper(_config, Monitor);
        _sorter = new ChestSorterService(_config, _mapper, Monitor, helper.DirectoryPath);

        helper.Events.GameLoop.DayEnding += OnDayEnding;
        // helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        // helper.Events.GameLoop.DayStarted += OnDayStarted;

        // // SMAPI console command (for direct console access).
        // helper.ConsoleCommands.Add("!sort", "Run chest auto-sort now.", OnSortCommand);

        Monitor.Log($"ChestAutoSorter loaded. AutoSort={(_config.EnableAutoSort ? "ON" : "OFF")}, " +
                    $"DayEnd={(_config.SortOnDayEnd ? "ON" : "OFF")}, " +
                    $"Locations=[{string.Join(", ", _config.SortLocations)}]", LogLevel.Info);
    }

    // // Reset chat tracking each day to avoid stale references.
    // private void OnDayStarted(object? sender, DayStartedEventArgs e)
    // {
    //     _lastProcessedMsg = null;
    // }

    // // Poll chat messages every ~2 seconds for !sort command.
    // private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    // {
    //     if (!Context.IsWorldReady) return;
    //     if (!e.IsMultipleOf(120)) return; // ~2 seconds at 60fps
    //
    //     CheckChatCommands();
    // }

    // private void CheckChatCommands()
    // {
    //     if (Game1.chatBox == null) return;
    //
    //     var messages = Game1.chatBox.messages;
    //     if (messages == null || messages.Count == 0) return;
    //
    //     int count = messages.Count;
    //     int scanLimit = Math.Min(count, 10);
    //     int startIdx = count - scanLimit;
    //
    //     // Find where we left off last time.
    //     for (int i = count - 1; i >= startIdx; i--)
    //     {
    //         if (messages[i] == _lastProcessedMsg)
    //         {
    //             startIdx = i + 1;
    //             break;
    //         }
    //     }
    //
    //     if (startIdx >= count) return;
    //
    //     for (int i = startIdx; i < count; i++)
    //     {
    //         var msg = messages[i];
    //         if (msg?.message == null || msg.message.Count == 0) continue;
    //
    //         // Concat all snippets into one string.
    //         _chatSb.Clear();
    //         foreach (var snippet in msg.message)
    //         {
    //             if (snippet.message != null)
    //                 _chatSb.Append(snippet.message);
    //         }
    //
    //         if (_chatSb.Length == 0) continue;
    //
    //         string fullText = _chatSb.ToString().Trim();
    //         string cmdText = fullText;
    //
    //         // Handle "PlayerName: !cmd" format.
    //         int colonIdx = fullText.IndexOf(':');
    //         if (colonIdx >= 0 && colonIdx < fullText.Length - 1)
    //             cmdText = fullText.Substring(colonIdx + 1).Trim();
    //
    //         // Must start with '!'.
    //         if (cmdText.Length < 2 || cmdText[0] != '!')
    //             continue;
    //
    //         string cmdLower = cmdText.ToLower();
    //
    //         if (cmdLower == "!sort")
    //         {
    //             _lastProcessedMsg = msg;
    //             count = messages.Count;
    //             ExecuteSort(fromChat: true);
    //         }
    //         else if (cmdLower == "!scan")
    //         {
    //             _lastProcessedMsg = msg;
    //             count = messages.Count;
    //             ExecuteScan();
    //         }
    //         else if (cmdLower == "!sorthelp")
    //         {
    //             _lastProcessedMsg = msg;
    //             count = messages.Count;
    //             SendChat("[ChestAutoSorter] Commands:");
    //             SendChat("  !sort     - Run chest sort now");
    //             SendChat("  !scan     - Scan all locations for chests");
    //             SendChat("  !sorthelp - Show this help");
    //         }
    //     }
    // }

    // private void SendChat(string message)
    // {
    //     Game1.chatBox?.addInfoMessage(message);
    // }

    // private void ExecuteScan()
    // {
    //     SendChat("[ChestAutoSorter] Scanning all locations...");
    //
    //     int totalChests = 0;
    //
    //     foreach (var location in Game1.locations)
    //     {
    //         if (location == null) continue;
    //
    //         int found = ScanLocation(location);
    //         totalChests += found;
    //
    //         // Scan building interiors (Cabins, Barns, Coops, Sheds, etc.)
    //         if (location is StardewValley.Farm farmLoc)
    //         {
    //             foreach (var building in farmLoc.buildings)
    //             {
    //                 if (building?.indoors?.Value == null) continue;
    //                 var interior = building.indoors.Value;
    //                 int interiorFound = ScanLocation(interior);
    //                 totalChests += interiorFound;
    //             }
    //         }
    //     }
    //
    //     Monitor.Log($"Chest scan complete: {totalChests} chests found.", LogLevel.Info);
    //     SendChat($"[ChestAutoSorter] Scan done: {totalChests} chests found.");
    // }

    // private int ScanLocation(GameLocation location)
    // {
    //     int found = 0;
    //     foreach (var obj in location.Objects.Values)
    //     {
    //         if (obj is not Chest chest) continue;
    //         if (!chest.playerChest.Value) continue;
    //
    //         found++;
    //         var c = chest.playerChoiceColor.Value;
    //         int itemCount = ChestSorterService.CountNonNullItems(chest);
    //
    //         Monitor.Log($"  [{location.Name}] ({obj.TileLocation.X},{obj.TileLocation.Y}) " +
    //                      $"RGB({c.R},{c.G},{c.B}), {itemCount} items",
    //                      LogLevel.Trace);
    //     }
    //
    //     if (found > 0)
    //         Monitor.Log($"  [{location.Name}]: {found} chest(s)", LogLevel.Info);
    //
    //     return found;
    // }

    // private void ExecuteSort(bool fromChat)
    // {
    //     string source = fromChat ? "chat" : "console";
    //
    //     if (!Game1.IsMasterGame)
    //     {
    //         string msg = "Only the host can run sort.";
    //         Monitor.Log(msg, LogLevel.Warn);
    //         if (fromChat) SendChat($"[ChestAutoSorter] {msg}");
    //         return;
    //     }
    //
    //     Monitor.Log($"Manual sort triggered via {source}.", LogLevel.Info);
    //     if (fromChat) SendChat("[ChestAutoSorter] Sorting...");
    //
    //     try
    //     {
    //         _sorter.RunSort();
    //         if (fromChat) SendChat("[ChestAutoSorter] Sort complete!");
    //     }
    //     catch (Exception ex)
    //     {
    //         Monitor.Log($"Sort failed: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
    //         if (fromChat) SendChat($"[ChestAutoSorter] Sort failed: {ex.Message}");
    //     }
    // }

    // // SMAPI console command handler.
    // private void OnSortCommand(string command, string[] args)
    // {
    //     if (!Context.IsWorldReady)
    //     {
    //         Monitor.Log("No save loaded yet.", LogLevel.Warn);
    //         return;
    //     }
    //
    //     ExecuteSort(fromChat: false);
    // }

    // Auto-sort at day end.
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Game1.IsMasterGame)
            return;

        if (!_config.EnableAutoSort || !_config.SortOnDayEnd)
            return;

        Monitor.Log("Running day-end sort...", LogLevel.Info);

        try
        {
            _sorter.RunSort();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Sort failed: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        }
    }
}
