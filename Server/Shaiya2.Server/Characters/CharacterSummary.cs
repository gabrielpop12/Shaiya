using Shaiya2.Server.Persistence.Entities;

namespace Shaiya2.Server.Characters;

public sealed class CharacterSummary
{
    public long Id { get; init; }

    public string Name { get; init; } =
        string.Empty;

    public CharacterFaction Faction { get; init; }

    public CharacterClass Class { get; init; }

    public CharacterGender Gender { get; init; }

    public int Level { get; init; }
}