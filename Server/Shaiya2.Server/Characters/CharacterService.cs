using Microsoft.EntityFrameworkCore;
using Shaiya2.Server.Persistence;
using Shaiya2.Server.Persistence.Entities;

using GameCharacter =
    Shaiya2.Server.Persistence.Entities.Character;

namespace Shaiya2.Server.Characters;

public sealed class CharacterService
{
    private const int MaxCharactersPerAccount = 5;

    public CharacterCreateResult CreateCharacter(
        long accountId,
        string name,
        CharacterFaction faction,
        CharacterClass characterClass,
        CharacterGender gender,
        out long characterId)
    {
        characterId = 0;

        name = name.Trim();

        if (!IsValidName(name))
            return CharacterCreateResult.InvalidName;

        if (!Enum.IsDefined(faction))
            return CharacterCreateResult.InvalidFaction;

        if (!Enum.IsDefined(characterClass))
            return CharacterCreateResult.InvalidClass;

        if (!Enum.IsDefined(gender))
            return CharacterCreateResult.InvalidGender;

        if (!IsClassValidForFaction(
                faction,
                characterClass))
        {
            return CharacterCreateResult.InvalidClass;
        }

        string normalizedName =
            name.ToUpperInvariant();

        try
        {
            using var db =
                new ShaiyaDbContext();

            int characterCount =
                db.Characters.Count(
                    character =>
                        character.AccountId ==
                        accountId
                );

            if (characterCount >=
                MaxCharactersPerAccount)
            {
                return CharacterCreateResult
                    .CharacterLimitReached;
            }

            bool exists =
                db.Characters.Any(
                    character =>
                        character.NormalizedName ==
                        normalizedName
                );

            if (exists)
            {
                return CharacterCreateResult
                    .NameAlreadyExists;
            }

            var character =
                new GameCharacter
                {
                    AccountId = accountId,

                    Name = name,

                    NormalizedName =
                        normalizedName,

                    Faction = faction,

                    Class = characterClass,

                    Gender = gender,

                    Level = 1,

                    Experience = 0,

                    Strength = 10,
                    Dexterity = 10,
                    Reaction = 10,
                    Intelligence = 10,
                    Wisdom = 10,
                    Luck = 10,

                    MapId = 1,

                    PositionX = 0,
                    PositionY = 1,
                    PositionZ = 0,

                    CreatedAtUtc =
                        DateTime.UtcNow
                };

            db.Characters.Add(
                character
            );

            db.SaveChanges();

            characterId =
                character.Id;

            Console.WriteLine(
                $"[Character] Created | CharacterId={character.Id} | AccountId={accountId} | Name={character.Name}"
            );

            return CharacterCreateResult.Success;
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                $"[Character] Database error: {ex.Message}"
            );

            return CharacterCreateResult.ServerError;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Character] Error: {ex.Message}"
            );

            return CharacterCreateResult.ServerError;
        }
    }

    private static bool IsValidName(
        string name)
    {
        if (name.Length < 3 ||
            name.Length > 16)
        {
            return false;
        }

        foreach (char character in name)
        {
            if (!char.IsLetterOrDigit(character))
                return false;
        }

        return true;
    }

    private static bool IsClassValidForFaction(
        CharacterFaction faction,
        CharacterClass characterClass)
    {
        return faction switch
        {
            CharacterFaction.AllianceOfLight =>
                characterClass is
                    CharacterClass.Fighter or
                    CharacterClass.Defender or
                    CharacterClass.Ranger or
                    CharacterClass.Archer or
                    CharacterClass.Mage or
                    CharacterClass.Priest,

            CharacterFaction.UnionOfFury =>
                characterClass is
                    CharacterClass.Warrior or
                    CharacterClass.Guardian or
                    CharacterClass.Assassin or
                    CharacterClass.Hunter or
                    CharacterClass.Pagan or
                    CharacterClass.Oracle,

            _ => false
        };
    }

    public IReadOnlyList<CharacterSummary> GetCharacters(
        long accountId)
    {
        using var db =
            new ShaiyaDbContext();

        return db.Characters
            .Where(
                character =>
                    character.AccountId ==
                    accountId
            )
            .OrderBy(
                character =>
                    character.Id
            )
            .Select(
                character =>
                    new CharacterSummary
                    {
                        Id = character.Id,
                        Name = character.Name,
                        Faction = character.Faction,
                        Class = character.Class,
                        Gender = character.Gender,
                        Level = character.Level
                    }
            )
            .ToList();
    }

    public CharacterSelectResult SelectCharacter(
        long accountId,
        long characterId,
        out SelectedCharacterData? selectedCharacter)
    {
        selectedCharacter = null;

        try
        {
            using var db =
                new ShaiyaDbContext();

            var character =
                db.Characters
                    .SingleOrDefault(
                        character =>
                            character.Id == characterId
                    );

            if (character == null)
            {
                return CharacterSelectResult.CharacterNotFound;
            }

            if (character.AccountId != accountId)
            {
                return CharacterSelectResult
                    .CharacterDoesNotBelongToAccount;
            }

            selectedCharacter =
                new SelectedCharacterData
                {
                    Id = character.Id,
                    Name = character.Name,
                    Faction = character.Faction,
                    Class = character.Class,
                    Gender = character.Gender,
                    Level = character.Level,
                    Experience = character.Experience,
                    MapId = character.MapId,
                    PositionX = character.PositionX,
                    PositionY = character.PositionY,
                    PositionZ = character.PositionZ
                };

            return CharacterSelectResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Character] Select error: {ex.Message}"
            );

            return CharacterSelectResult.ServerError;
        }
    }

    public SelectedCharacterData? GetCharacter(
        long accountId,
        long characterId)
    {
        using var db =
            new ShaiyaDbContext();

        return db.Characters
            .Where(
                character =>
                    character.AccountId == accountId &&
                    character.Id == characterId
            )
            .Select(
                character =>
                    new SelectedCharacterData
                    {
                        Id = character.Id,
                        Name = character.Name,
                        Faction = character.Faction,
                        Class = character.Class,
                        Gender = character.Gender,
                        Level = character.Level,
                        Experience = character.Experience,
                        MapId = character.MapId,
                        PositionX = character.PositionX,
                        PositionY = character.PositionY,
                        PositionZ = character.PositionZ
                    }
            )
            .SingleOrDefault();
    }

    public bool UpdateProgression(
        long characterId,
        int level,
        long experience)
    {
        try
        {
            using var db =
                new ShaiyaDbContext();

            var character =
                db.Characters
                    .SingleOrDefault(
                        character =>
                            character.Id ==
                            characterId
                    );

            if (character == null)
            {
                Console.WriteLine(
                    $"[Character] Progression save failed | CharacterId={characterId} | Not found"
                );

                return false;
            }

            character.Level =
                level;

            character.Experience =
                experience;

            db.SaveChanges();

            Console.WriteLine(
                $"[Character] Progression saved | CharacterId={characterId} | Level={level} | XP={experience}"
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Character] Progression save error | CharacterId={characterId} | {ex.Message}"
            );

            return false;
        }
    }
}