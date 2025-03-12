using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MonsterVariety;

internal sealed class VarietyData
{
    public string? Condition { get; set; }

    public Season? Season { get; set; }

    public string? Sprite { get; set; }

    public int Precedence { get; set; } = 0;
}

internal sealed class MonsterVarietyData
{
    public bool AlwaysOverride { get; set; } = false;
    public Dictionary<string, VarietyData> Varieties { get; set; } = [];
    public Dictionary<string, VarietyData> DangerousVarieties { get; set; } = [];
}

internal sealed class AssetManager
{
    internal static string Asset_VarietyData = $"{ModEntry.ModId}/Data";
    private static Dictionary<string, MonsterVarietyData>? varietyData = null;
    internal static Dictionary<string, MonsterVarietyData> VarietyData =>
        varietyData ??= Game1.content.Load<Dictionary<string, MonsterVarietyData>>(Asset_VarietyData);

    internal static void Register(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Asset_VarietyData))
            e.LoadFrom(() => new Dictionary<string, MonsterVarietyData>(), AssetLoadPriority.Low);
    }

    private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo(Asset_VarietyData)))
            varietyData = null;
    }
}
