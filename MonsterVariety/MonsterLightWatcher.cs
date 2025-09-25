using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Network;

namespace MonsterVariety;

internal class MonsterLightWatcher(Monster monster) : IDisposable
{
    private Monster monster = monster;
    private LightSource? lightSource = null;

    ~MonsterLightWatcher() => Dispose();

    internal static MonsterLightWatcher Create(Monster monster) => new MonsterLightWatcher(monster);

    internal bool Activate(string lightPropsStr)
    {
        if (lightSource != null)
        {
            Deactivate();
        }
        string[] lightProps = ArgUtility.SplitBySpaceQuoteAware(lightPropsStr);
        if (
            !ArgUtility.TryGetInt(lightProps, 0, out int radius, out string error, "string radius")
            || !ArgUtility.TryGetOptional(lightProps, 1, out string lightColor, out error, "string lightColor")
        )
        {
            return false;
        }
        Color color = Color.White * 0.7f;
        if (lightColor != null && Utility.StringToColor(lightColor) is Color parsedColor)
        {
            color = new Color(parsedColor.PackedValue ^ 0x00FFFFFF);
        }
        lightSource = new(
            $"{GetType().Name}_{Game1.random.Next(-99999, 99999)}",
            radius,
            new Vector2(monster.Position.X + 32f, monster.Position.Y + 64f + monster.yOffset),
            1f,
            color,
            LightSource.LightContext.None,
            0L,
            Game1.currentLocation.NameOrUniqueName
        );
        Game1.currentLightSources.Add(lightSource.Id, lightSource);
        monster.position.fieldChangeVisibleEvent += OnPositionChanged;
        return true;
    }

    internal void Deactivate()
    {
        if (lightSource == null)
            return;
        Game1.currentLightSources.Remove(lightSource.Id);
        lightSource = null;
        monster.position.fieldChangeVisibleEvent -= OnPositionChanged;
    }

    private void OnPositionChanged(NetPosition field, Vector2 oldValue, Vector2 newValue)
    {
        if (lightSource == null || monster.currentLocation != Game1.currentLocation || monster.Health <= 0)
        {
            Deactivate();
            return;
        }
        lightSource.position.X = newValue.X + 32f;
        lightSource.position.Y = newValue.Y + 64f + monster.yOffset;
    }

    public void Dispose()
    {
        if (monster == null || lightSource == null)
            return;
        Deactivate();
        monster = null!;
        lightSource = null!;
        GC.SuppressFinalize(this);
    }
}
