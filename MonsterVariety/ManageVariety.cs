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
    internal static string ModData_AppliedVariety = $"{ModEntry.ModId}/HasAppliedVariety";

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

    private static bool IsValidVariety(
        Monster monster,
        VarietyData variety,
        bool onlyAlwaysOverride,
        GameStateQueryContext gameStateQueryContext
    )
    {
        if (onlyAlwaysOverride && !variety.AlwaysOverride)
            return false;
        if (variety.Sprite == null)
            return false;
        if (!Game1.content.DoesAssetExist<Texture2D>(variety.Sprite))
            return false;
        if (variety.Season != null && variety.Season != Game1.GetSeasonForLocation(monster.currentLocation))
            return false;
        if (variety.Condition != null && !GameStateQuery.CheckConditions(variety.Condition, gameStateQueryContext))
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
#if SDV1615
                    monster.objectsToDrop.Add(item.QualifiedItemId);
#else
                    monster.drops.Add(item);
#endif
                }
            }
        }
    }

    private static void ApplyMonsterVariety(Monster monster)
    {
        Type type = monster.GetType();
        string monsterName = monster.Name;
        // special case Armored Bug & Assassin Bug
        if (monster is Bug bug && bug.isArmoredBug.Value)
        {
            monsterName = "Armored Bug";
        }
        else if (monster.Sprite?.textureName?.Value == "Characters\\Monsters\\Assassin Bug")
        {
            monsterName = "Assassin Bug";
        }

        ModEntry.LogOnce(
            $"Try ApplyMonsterVariety on '{monsterName}' ({type.Namespace} : {type.Name} '{monster.Sprite?.textureName?.Value}' HardMode:{monster.isHardModeMonster.Value})"
        );
        if (!AssetManager.VarietyData.TryGetValue(monsterName, out MonsterVarietyData? data))
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
                new(monster.currentLocation, Game1.player, null, $"{ModEntry.ModId}:{monsterName}");
            if (monster.isHardModeMonster.Value)
            {
                varieties = data.DangerousVarieties.Count > 0 ? data.DangerousVarieties : data.Varieties;
                AddExtraDrops(monster, data.DangerousSharedExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }
            else
            {
                varieties = data.Varieties;
                AddExtraDrops(monster, data.SharedExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }

            List<VarietyData> validVariety = varieties
                .Values.Where(variety => IsValidVariety(monster, variety, onlyAlwaysOverride, gameStateQueryContext))
                .ToList();
            if (validVariety.Count > 0)
            {
                int minPrecedence = validVariety.Min(variety => variety.Precedence);
                List<VarietyData> validVarietyList = validVariety
                    .Where(variety => variety.Precedence == minPrecedence)
                    .ToList();
                var chosenVariety = Random.Shared.ChooseFrom(validVarietyList);
                textureName = chosenVariety.Sprite!;
                monster.modData[ModData_AppliedVariety] = textureName;
                AddExtraDrops(monster, chosenVariety.ExtraDrops?.Values, gameStateQueryContext, itemQueryContext);
            }
            else
            {
                return;
            }
        }

        if (monster.Sprite?.textureName.Value != textureName)
        {
            if (monster.Sprite == null)
                monster.Sprite = new AnimatedSprite(textureName);
            else
                monster.Sprite.textureName.Value = textureName;
        }
    }
}
