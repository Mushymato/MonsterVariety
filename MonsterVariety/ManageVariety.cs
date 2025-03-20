using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
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

    private static bool IsValidVariety(Monster monster, VarietyData variety, bool onlyAlwaysOverride)
    {
        if (onlyAlwaysOverride && !variety.AlwaysOverride)
            return false;
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

    private static void ApplyMonsterVariety(Monster monster)
    {
        Type type = monster.GetType();
        ModEntry.LogOnce($"Try ApplyMonsterVariety on '{monster.Name}' ({type.Namespace} : {type.Name})");
        if (!AssetManager.VarietyData.TryGetValue(monster.Name, out MonsterVarietyData? data))
        {
            // special case Green Slime
            if (monster is not GreenSlime || !AssetManager.VarietyData.TryGetValue("Green Slime", out data))
            {
                return;
            }
        }

        string currTextureName = monster.Sprite.textureName.Value;
        bool onlyAlwaysOverride =
            !currTextureName.StartsWithIgnoreCase("Characters\\Monsters\\")
            && !currTextureName.StartsWithIgnoreCase("Characters/Monsters/");

        if (!monster.modData.TryGetValue(ModData_AppliedVariety, out string textureName))
        {
            var varieties = data.Varieties;
            if (monster.isHardModeMonster.Value && data.DangerousVarieties.Count > 0)
                varieties = data.DangerousVarieties;

            IEnumerable<VarietyData> validVariety = varieties.Values.Where(variety =>
                IsValidVariety(monster, variety, onlyAlwaysOverride)
            );
            int minPrecedence = validVariety.Min(variety => variety.Precedence);
            List<VarietyData> validVarietyList = validVariety
                .Where(variety => variety.Precedence == minPrecedence)
                .ToList();

            textureName = validVarietyList[Random.Shared.Next(validVarietyList.Count)].Sprite!;
            monster.modData[ModData_AppliedVariety] = textureName;
        }
        if (monster.Sprite == null)
            monster.Sprite = new AnimatedSprite(textureName);
        else
            monster.Sprite.textureName.Value = textureName;
    }
}
