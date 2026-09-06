using Shaiya2.Server.Persistence.Entities;

namespace Shaiya2.Server.Characters;

public sealed class SelectedCharacterData
{
    public long Id { get; init; }

    public string Name { get; init; } =
        string.Empty;

    public CharacterFaction Faction { get; init; }

    public CharacterClass Class { get; init; }

    public CharacterGender Gender { get; init; }

    public int Level { get; init; }

    public long Experience { get; init; }

    public int MapId { get; init; }

    public float PositionX { get; init; }

    public float PositionY { get; init; }

    public float PositionZ { get; init; }
}