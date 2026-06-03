namespace ChestAutoSorter;

/// <summary>Config model. SMAPI auto-serializes to/from config.json.</summary>
public sealed class ModConfig
{
    public bool EnableAutoSort { get; set; } = true;
    public bool SortOnDayEnd { get; set; } = true;

    /// <summary>Game location names to scan. Items move freely across locations.</summary>
    public List<string> SortLocations { get; set; } = new() { "Farm", "FarmHouse" };

    /// <summary>Chest color name -> accepted item Category IDs.</summary>
    public Dictionary<string, List<int>> ColorCategoryMap { get; set; } = new()
    {
        ["Blue"]     = new List<int> { -4, -21, -22 },
        ["Green"]    = new List<int> { -75, -79, -80, -81, -74, -19 },
        ["Pink"]     = new List<int> { -5, -6, -14, -18 },
        ["Yellow"]   = new List<int> { -26, -27 },
        ["Orange"]   = new List<int> { -7, -25 },
        ["Purple"]   = new List<int> { -2, -12, -15 },
        ["Teal"]     = new List<int> { -16, -8, -20 },
        ["Red"]      = new List<int> { -28, -102, -103 },
        ["Default"]  = new List<int> { -24, -29, -95, -96, -97, -98, -99, -100, -101 },
        ["DarkGrey"] = new List<int> { 0, -9 }
    };

    /// <summary>Chest colors to exclude from sorting entirely.</summary>
    public List<string> ExcludedColors { get; set; } = new()
    {
        "White", "LightBlue", "Aqua", "LimeGreen", "LightOrange",
        "DarkRed", "LightPink", "Magenta", "DarkPurple", "MediumGrey", "LightGrey"
    };
}
