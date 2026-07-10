using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Monsters;

namespace MonsterVariety;

public sealed class AlwaysOverrideMode
{
    public bool CustomMonsterClass { get; set; } = false;
    public bool CustomTextures { get; set; } = false;
    public string? SpecificTextureName { get; set; } = null;

    public static implicit operator AlwaysOverrideMode(bool value) =>
        new()
        {
            CustomMonsterClass = value,
            CustomTextures = value,
            SpecificTextureName = null,
        };
}

public sealed class VarietyMonsterInfoOverride
{
    public string? DisplayName { get; set; }
    public int? Health { get; set; }
    public int? DamageToFarmer { get; set; }
    public bool? IsGlider { get; set; }
    public int? Resilience { get; set; }
    public double? Jitteriness { get; set; }
    public int? MoveTowardPlayerThreshold { get; set; }
    public int? Speed { get; set; }
    public double? MissChance { get; set; }
    public int? ExperienceGained { get; set; }

    internal void Apply(Monster monster)
    {
        monster.displayName = DisplayName ?? monster.displayName;
        if (Health.HasValue)
        {
            float priorHealthRatio = (float)monster.Health / monster.MaxHealth;
            monster.MaxHealth = Health.Value;
            monster.Health = (int)MathF.Ceiling(priorHealthRatio * Health.Value);
        }
        if (DamageToFarmer.HasValue)
            monster.DamageToFarmer = DamageToFarmer.Value;
        if (IsGlider.HasValue)
            monster.isGlider.Value = IsGlider.Value;
        if (Resilience.HasValue)
            monster.resilience.Value = Resilience.Value;
        if (Jitteriness.HasValue)
            monster.jitteriness.Value = Jitteriness.Value;
        if (MoveTowardPlayerThreshold.HasValue)
            monster.moveTowardPlayer(MoveTowardPlayerThreshold.Value);
        if (Speed.HasValue)
            monster.Speed = Speed.Value;
        if (MissChance.HasValue)
            monster.missChance.Value = MissChance.Value;
        if (ExperienceGained.HasValue)
            monster.ExperienceGained = ExperienceGained.Value;
    }
}

public sealed class VarietyData
{
    public AlwaysOverrideMode AlwaysOverride { get; set; } = false;

    public string? Condition { get; set; } = null;

    public Season? Season { get; set; } = null;

    public string? Sprite { get; set; } = null;

    public string? LightProps { get; set; } = null;

    public int Precedence { get; set; } = 0;

    public string? HUDNotif { get; set; } = null;

    public string? HUDNotifIconItem { get; set; } = null;

    public VarietyMonsterInfoOverride? InfoOverrides { get; set; } = null;

    public Dictionary<string, GenericSpawnItemDataWithCondition>? ExtraDrops { get; set; } = null;

    public List<string>? ExcludeDrops { get; set; } = null;
}

public sealed class MonsterVarietyData
{
    public string? MonsterName { get; set; } = null;
    public Dictionary<string, VarietyData> Varieties { get; set; } = [];
    public Dictionary<string, GenericSpawnItemDataWithCondition>? SharedExtraDrops { get; set; } = null;
    public Dictionary<string, VarietyData> DangerousVarieties { get; set; } = [];
    public Dictionary<string, GenericSpawnItemDataWithCondition>? DangerousSharedExtraDrops { get; set; } = null;

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
        if (other.SharedExtraDrops != null)
        {
            SharedExtraDrops ??= [];
            foreach (var kv in other.SharedExtraDrops)
                SharedExtraDrops[kv.Key] = kv.Value;
        }
        if (other.DangerousSharedExtraDrops != null)
        {
            DangerousSharedExtraDrops ??= [];
            foreach (var kv in other.DangerousSharedExtraDrops)
                DangerousSharedExtraDrops[kv.Key] = kv.Value;
        }
    }
}

internal sealed class AssetManager
{
    internal const string Asset_VarietyData = $"{ModEntry.ModId}/Data";
    private static Dictionary<string, MonsterVarietyData>? varietyDataRaw = null;
    private static readonly Dictionary<string, MonsterVarietyData> varietyData = [];
    internal static Dictionary<string, MonsterVarietyData> VarietyData
    {
        get
        {
            if (varietyDataRaw == null)
            {
                varietyDataRaw = Game1.content.Load<Dictionary<string, MonsterVarietyData>>(Asset_VarietyData);
                int discarded = 0;
                foreach (MonsterVarietyData vd in varietyDataRaw.Values)
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
            e.LoadFrom(() => new Dictionary<string, MonsterVarietyData>(), AssetLoadPriority.Exclusive);
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
