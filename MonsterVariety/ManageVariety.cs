using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace MonsterVariety;

internal static class ManageVariety
{
    internal static string ModData_AppliedVariety => $"{ModEntry.ModId}/HasAppliedVariety";

    internal static void Apply(IModHelper helper)
    {
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Player.Warped += OnWarped;
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Game1.currentLocation.characters.OnValueAdded += OnMonsterAdded;
    }

    private static void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.OldLocation != null)
            e.OldLocation.characters.OnValueAdded -= OnMonsterAdded;
        if (e.NewLocation != null)
        {
            foreach (NPC npc in e.NewLocation.characters)
            {
                OnMonsterAdded(npc);
            }
            e.NewLocation.characters.OnValueAdded += OnMonsterAdded;
        }
    }

    private static void OnMonsterAdded(NPC value)
    {
        if (value is Monster monster)
            ApplyMonsterVariety(monster);
    }

    private static bool IsValidVariety(Monster monster, VarietyData variety)
    {
        if (variety.Sprite == null)
            return false;
        if (!Game1.content.DoesAssetExist<Texture2D>(variety.Sprite))
            return false;
        if (variety.Season != null && variety.Season != monster.currentLocation.GetSeason())
            return false;
        if (
            variety.Condition != null
            && !GameStateQuery.CheckConditions(variety.Condition, location: monster.currentLocation)
        )
            return false;
        return true;
    }

    private static void ApplyMonsterVariety(Monster __instance)
    {
        ModEntry.LogOnce($"Try ApplyMonsterVariety on '{__instance.Name}'");
        if (!AssetManager.VarietyData.TryGetValue(__instance.Name, out MonsterVarietyData? data))
        {
            // special case Green Slime
            if (__instance is not GreenSlime || !AssetManager.VarietyData.TryGetValue("Green Slime", out data))
            {
                return;
            }
        }

        var varieties = data.Varieties;
        if (__instance.isHardModeMonster.Value && data.DangerousVarieties.Count > 0)
            varieties = data.DangerousVarieties;

        IEnumerable<VarietyData> validVariety = varieties.Values.Where(variety => IsValidVariety(__instance, variety));
        int minPrecedence = validVariety.Min(variety => variety.Precedence);
        List<VarietyData> validVarietyList = validVariety
            .Where(variety => variety.Precedence == minPrecedence)
            .ToList();

        string textureName = validVarietyList[Random.Shared.Next(validVarietyList.Count)].Sprite!;
        __instance.modData[ModData_AppliedVariety] = textureName;
        if (__instance.Sprite == null)
            __instance.Sprite = new AnimatedSprite(textureName);
        else
            __instance.Sprite.textureName.Value = textureName;
    }
}
