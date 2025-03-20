using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MonsterVariety;

internal sealed class VarietyData
{
    public bool AlwaysOverride { get; set; } = false;

    public string? Condition { get; set; }

    public Season? Season { get; set; }

    public string? Sprite { get; set; }

    public int Precedence { get; set; } = 0;
}

internal sealed class MonsterVarietyData
{
    public string? Id { get; set; } = null;
    public string? MonsterName { get; set; } = null;
    public Dictionary<string, VarietyData> Varieties { get; set; } = [];
    public Dictionary<string, VarietyData> DangerousVarieties { get; set; } = [];

    internal void Merge(MonsterVarietyData other)
    {
        foreach (var kv in other.Varieties)
        {
            Varieties[kv.Key] = kv.Value;
        }
        foreach (var kv in other.DangerousVarieties)
        {
            DangerousVarieties[kv.Key] = kv.Value;
        }
    }
}

internal sealed class AssetManager
{
    internal static string Asset_VarietyData = $"{ModEntry.ModId}/Data";

    private static List<MonsterVarietyData>? varietyDataRaw = null;
    private static readonly Dictionary<string, MonsterVarietyData> varietyData = [];
    internal static Dictionary<string, MonsterVarietyData> VarietyData
    {
        get
        {
            if (varietyDataRaw == null)
            {
                varietyDataRaw = Game1.content.Load<List<MonsterVarietyData>>(Asset_VarietyData);
                int discarded = 0;
                foreach (MonsterVarietyData vd in varietyDataRaw)
                {
                    if (vd.MonsterName == null)
                    {
                        discarded++;
                        continue;
                    }
                    if (!varietyData.ContainsKey(vd.MonsterName))
                        varietyData[vd.MonsterName] = new();
                    varietyData[vd.MonsterName].Merge(vd);
                }
                if (discarded > 0)
                {
                    ModEntry.Log($"Discarded {discarded} entries without 'MonsterName'", LogLevel.Warn);
                }
            }
            return varietyData;
        }
    }

    internal static void Register(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Asset_VarietyData))
            e.LoadFrom(() => new List<MonsterVarietyData>(), AssetLoadPriority.Low);
    }

    private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo(Asset_VarietyData)))
        {
            varietyDataRaw = null;
            varietyData.Clear();
        }
    }
}
