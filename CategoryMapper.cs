using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Objects;

namespace ChestAutoSorter;

/// <summary>
/// Resolves a chest to its sort-group color name via playerChoiceColor RGB.
/// </summary>
public sealed class CategoryMapper
{
    // Actual chest color RGB values from the game (verified via logs).
    private static readonly Dictionary<(int R, int G, int B), string> ColorNameLookup = new()
    {
        // --- Used for sorting (9) + White (excluded by default) ---
        [(255, 0, 0)]     = "Red",
        [(255, 105, 18)]  = "Orange",
        [(255, 234, 18)]  = "Yellow",
        [(0, 170, 0)]     = "Green",
        [(0, 170, 170)]   = "Teal",
        [(85, 85, 255)]   = "Blue",
        [(143, 0, 255)]   = "Purple",
        [(255, 117, 195)] = "Pink",
        [(254, 254, 254)] = "White",

        // --- DarkGrey: used as misc/catch-all chest ---
        [(64, 64, 64)]    = "DarkGrey",

        // --- Excluded by default (10) ---
        [(100, 100, 100)] = "MediumGrey",
        [(200, 200, 200)] = "LightGrey",
        [(119, 191, 255)] = "LightBlue",
        [(0, 234, 175)]   = "Aqua",
        [(159, 236, 0)]   = "LimeGreen",
        [(255, 167, 18)]  = "LightOrange",
        [(135, 0, 35)]    = "DarkRed",
        [(255, 173, 199)] = "LightPink",
        [(172, 0, 198)]   = "Magenta",
        [(89, 11, 142)]   = "DarkPurple"
    };

    // Reverse index: Category ID -> color name.
    private readonly Dictionary<int, string> _categoryToColor = new();

    // Pre-built cache: color name -> accepted Category IDs.
    private readonly Dictionary<string, HashSet<int>> _acceptedCategoriesCache = new(StringComparer.OrdinalIgnoreCase);

    // Excluded colors as HashSet for O(1) lookup.
    private readonly HashSet<string> _excludedColorsSet;

    public CategoryMapper(ModConfig config, IMonitor monitor)
    {
        _excludedColorsSet = new HashSet<string>(config.ExcludedColors, StringComparer.OrdinalIgnoreCase);

        BuildReverseLookup(config.ColorCategoryMap);
        BuildAcceptedCategoriesCache(config.ColorCategoryMap);

        monitor.Log($"CategoryMapper ready: {config.ColorCategoryMap.Count} color groups, " +
                     $"{_categoryToColor.Count} category mappings, {config.ExcludedColors.Count} excluded colors.",
                     LogLevel.Trace);
    }

    private void BuildReverseLookup(Dictionary<string, List<int>> colorCategoryMap)
    {
        foreach (var (colorName, categoryIds) in colorCategoryMap)
            foreach (int catId in categoryIds)
                _categoryToColor.TryAdd(catId, colorName); // first mapping wins
    }

    private void BuildAcceptedCategoriesCache(Dictionary<string, List<int>> colorCategoryMap)
    {
        foreach (var (colorName, categoryIds) in colorCategoryMap)
            _acceptedCategoriesCache[colorName] = new HashSet<int>(categoryIds);
    }

    public string GetColorName(Color chestColor)
    {
        if (chestColor.R == 0 && chestColor.G == 0 && chestColor.B == 0)
            return "Default"; // uncolored brown chest

        var key = ((int)chestColor.R, (int)chestColor.G, (int)chestColor.B);
        return ColorNameLookup.TryGetValue(key, out string? name) ? name : "Unknown";
    }

    public bool IsExcludedColor(string colorName) => _excludedColorsSet.Contains(colorName);

    public bool HasCategoryMapping(string colorName) => _acceptedCategoriesCache.ContainsKey(colorName);

    public bool TryGetTargetColor(int categoryId, out string targetColor)
        => _categoryToColor.TryGetValue(categoryId, out targetColor!);

    public HashSet<int>? GetAcceptedCategories(string colorName)
        => _acceptedCategoriesCache.TryGetValue(colorName, out var set) ? set : null;

    /// <summary>
    /// Determine which color group a chest belongs to based on its dye color.
    /// Returns null if the chest should not participate in sorting.
    /// </summary>
    public string? ResolveChestCategory(Chest chest)
    {
        string colorName = GetColorName(chest.playerChoiceColor.Value);

        if (IsExcludedColor(colorName))
            return null;
        if (colorName == "Unknown")
            return null;
        if (!HasCategoryMapping(colorName))
            return null;

        return colorName;
    }
}
