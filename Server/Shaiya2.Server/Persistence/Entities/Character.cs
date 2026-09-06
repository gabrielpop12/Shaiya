namespace Shaiya2.Server.Persistence.Entities;

public sealed class Character
{
    public long Id { get; set; }

    public long AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public CharacterFaction Faction { get; set; }

    public CharacterClass Class { get; set; }

    public CharacterGender Gender { get; set; }

    public int Level { get; set; }

    public long Experience { get; set; }

    public int Strength { get; set; }

    public int Dexterity { get; set; }

    public int Reaction { get; set; }

    public int Intelligence { get; set; }

    public int Wisdom { get; set; }

    public int Luck { get; set; }

    public int MapId { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}