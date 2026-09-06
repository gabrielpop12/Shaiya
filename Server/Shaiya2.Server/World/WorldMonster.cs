namespace Shaiya2.Server.World;

public sealed class WorldMonster
{
    public long EntityId { get; }

    public int MonsterId { get; }

    public string Name { get; }

    public int Level { get; }

    public int MapId { get; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public int MaxHp { get; }

    public int CurrentHp { get; set; }

    public long? TargetCharacterId { get; set; }

    public float SpawnX { get; }
    public float SpawnY { get; }
    public float SpawnZ { get; }

    public float MovementSpeed { get; }

    public float DetectionRadius { get; }

    public float AttackRange { get; }

    public int AttackDamage { get; }

    public float AttackCooldown { get; }

    public float AttackCooldownRemaining { get; set; }

    public bool IsDead =>
        CurrentHp <= 0;

    public WorldMonster(
        long entityId,
        int monsterId,
        string name,
        int level,
        int mapId,
        float x,
        float y,
        float z,
        int maxHp)
    {
        EntityId = entityId;
        MonsterId = monsterId;
        Name = name;
        Level = level;
        MapId = mapId;

        X = x;
        Y = y;
        Z = z;

        MaxHp = maxHp;
        CurrentHp = maxHp;

        SpawnX = x;
        SpawnY = y;
        SpawnZ = z;

        MovementSpeed = 2.5f;
        DetectionRadius = 10f;
        AttackRange = 1.5f;

        AttackDamage = 10;

        AttackCooldown = 2f;

        AttackCooldownRemaining = 0f;
    }
}