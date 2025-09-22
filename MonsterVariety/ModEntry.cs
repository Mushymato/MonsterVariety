using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;

namespace MonsterVariety;

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    private static IMonitor? mon;
    internal const string ModId = "mushymato.MonsterVariety";

    internal static HashSet<string>? VanillaCharacterMonster;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        AssetManager.Register(helper);
        ManageVariety.Apply(helper);
        VanillaCharacterMonster = helper.Data.ReadJsonFile<HashSet<string>>("assets/vanilla_character_monsters.json");

        GameStateQuery.Register($"{ModId}_LUCKY_RANDOM", LUCKY_RANDOM);
        GameStateQuery.Register($"{ModId}_SYNCED_LUCKY_RANDOM", SYNCED_LUCKY_RANDOM);
    }

    public static bool RandomImpl(Random random, string[] query, int skipArguments)
    {
        if (!ArgUtility.TryGetFloat(query, skipArguments, out float valueFlt, out string error, "float chance"))
        {
            Log(error);
            return false;
        }
        double value = valueFlt;
        bool addDailyLuck = false;
        float addPlayerLuck = 0;
        for (int i = skipArguments + 1; i < query.Length; i++)
        {
            if (query[i].EqualsIgnoreCase("@addDailyLuck"))
            {
                addDailyLuck = true;
            }
            if (query[i].EqualsIgnoreCase("@addPlayerLuck"))
            {
                ArgUtility.TryGetOptionalFloat(query, i + 1, out addPlayerLuck, out _, 1f, "float playerLuckMod");
            }
        }
        if (addDailyLuck)
        {
            value += Game1.player.DailyLuck;
        }
        value += addPlayerLuck / 100 * Game1.player.LuckLevel;
        return random.NextDouble() < (double)value;
    }

    private bool LUCKY_RANDOM(string[] query, GameStateQueryContext context)
    {
        return RandomImpl(context.Random, query, 1);
    }

    private bool SYNCED_LUCKY_RANDOM(string[] query, GameStateQueryContext context)
    {
        if (
            !ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string interval")
            || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string key")
            || !Utility.TryCreateIntervalRandom(value, value2, out Random random, out error)
        )
        {
            Log(error, LogLevel.Error);
            return false;
        }
        return RandomImpl(random, query, 2);
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }
}
