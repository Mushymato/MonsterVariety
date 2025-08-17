using StardewModdingAPI;

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
