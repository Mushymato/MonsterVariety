using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Internal;
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
        foreach (NPC npc in Game1.currentLocation.characters)
            OnMonsterAdded(npc);
        Game1.currentLocation.characters.OnValueAdded += OnMonsterAdded;
    }

    private static void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.OldLocation != null)
            e.OldLocation.characters.OnValueAdded -= OnMonsterAdded;
        if (e.NewLocation != null)
        {
            foreach (NPC npc in e.NewLocation.characters)
                OnMonsterAdded(npc);
            e.NewLocation.characters.OnValueAdded += OnMonsterAdded;
        }
    }

    private static void OnMonsterAdded(NPC value)
    {
        if (value is Monster monster)
            ApplyMonsterVariety(monster);
    }

    private static bool IsVanillaSprite(string? currTextureName)
    {
        if (currTextureName == null)
            return true;
        string[] nameParts = currTextureName.Split(['\\', '/']);
        return nameParts.Length == 3
            && nameParts[0].EqualsIgnoreCase("Characters")
            && nameParts[1].EqualsIgnoreCase("Monsters")
            && (ModEntry.VanillaCharacterMonster?.Contains(nameParts[2].ToLower()) ?? false);
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

    private static void AddExtraDrops(
        Monster monster,
        IEnumerable<GenericSpawnItemDataWithCondition>? dropsQueries,
        GameStateQueryContext gsqContext,
        ItemQueryContext iqContext
    )
    {
        if (dropsQueries == null)
            return;
        foreach (GenericSpawnItemDataWithCondition spawnData in dropsQueries)
        {
            if (!GameStateQuery.CheckConditions(spawnData.Condition, gsqContext))
                continue;
            var results = ItemQueryResolver.TryResolve(spawnData, iqContext, filter: ItemQuerySearchMode.AllOfTypeItem);
            foreach (var res in results)
            {
                if (res.Item is Item item)
                {
                    monster.objectsToDrop.Add(item.QualifiedItemId);
                }
            }
        }
    }

    private static void ApplyMonsterVariety(Monster monster)
    {
        Type type = monster.GetType();
        ModEntry.LogOnce(
            $"Try ApplyMonsterVariety on '{monster.Name}' ({type.Namespace} : {type.Name} '{monster.Sprite?.textureName?.Value}')"
        );
        if (!AssetManager.VarietyData.TryGetValue(monster.Name, out MonsterVarietyData? data))
        {
            // special case Green Slime
            if (monster is not GreenSlime || !AssetManager.VarietyData.TryGetValue("Green Slime", out data))
            {
                return;
            }
        }

        if (!monster.modData.TryGetValue(ModData_AppliedVariety, out string textureName))
        {
            bool onlyAlwaysOverride =
                type.Namespace != "StardewValley.Monsters" || !IsVanillaSprite(monster.Sprite?.textureName?.Value);
            Dictionary<string, VarietyData> varieties;
            GameStateQueryContext gameStateQueryContext = new(monster.currentLocation, Game1.player, null, null, null);
            ItemQueryContext itemQueryContext =
                new(monster.currentLocation, Game1.player, null, $"{ModEntry.ModId}:{monster.Name}");
            if (monster.isHardModeMonster.Value && data.DangerousVarieties.Count > 0)
            {
                varieties = data.DangerousVarieties;
                AddExtraDrops(monster, data.DangerousSharedExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }
            else
            {
                varieties = data.Varieties;
                AddExtraDrops(monster, data.SharedExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }

            IEnumerable<VarietyData> validVariety = varieties.Values.Where(variety =>
                IsValidVariety(monster, variety, onlyAlwaysOverride)
            );
            if (validVariety.Any())
            {
                int minPrecedence = validVariety.Min(variety => variety.Precedence);
                List<VarietyData> validVarietyList = validVariety
                    .Where(variety => variety.Precedence == minPrecedence)
                    .ToList();
                var chosenVariety = validVarietyList[Random.Shared.Next(validVarietyList.Count)];
                textureName = chosenVariety.Sprite!;
                monster.modData[ModData_AppliedVariety] = textureName;
                AddExtraDrops(monster, chosenVariety.ExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }
            else
            {
                return;
            }
        }
        if (monster.Sprite == null)
            monster.Sprite = new AnimatedSprite(textureName);
        else
            monster.Sprite.textureName.Value = textureName;
    }
}
