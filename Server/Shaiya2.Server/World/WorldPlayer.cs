using Shaiya2.Server.Networking.Sessions;

namespace Shaiya2.Server.World;

public sealed class WorldPlayer
{
    public long CharacterId { get; }

    public string Name { get; }

    public PlayerSession Session { get; }

    public int MapId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float MovementInputX { get; set; }

    public float MovementInputZ { get; set; }

    public int MaxHp { get; set; } = 100;

    public int CurrentHp { get; set; } = 100;

    public bool IsDead =>
        CurrentHp <= 0;

    public float RespawnX { get; set; }

    public float RespawnY { get; set; }

    public float RespawnZ { get; set; }

    public int AttackDamage { get; set; } = 25;

    public float AttackRange { get; set; } = 2.5f;

    public float AttackCooldown { get; set; } = 1.0f;

    public float AttackCooldownRemaining { get; set; }

    public int Level { get; set; } = 1;

    public long Experience { get; set; } = 0;

    public HashSet<long> VisiblePlayers { get; } =
        new();

    public WorldPlayer(
        long characterId,
        string name,
        PlayerSession session,
        int mapId,
        float x,
        float y,
        float z)
    {
        CharacterId =
            characterId;

        Name =
            name;

        Session =
            session;

        MapId =
            mapId;

        X = x;
        Y = y;
        Z = z;

        RespawnX = x;
        RespawnY = y;
        RespawnZ = z;
    }

    public static long GetExperienceRequiredForLevel(
        int level)
    {
        return 100L * level * level;
    }
}