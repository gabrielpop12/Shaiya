namespace Shaiya2.Server.Characters;

public enum CharacterCreateResult : byte
{
    Success = 0,

    NotAuthenticated = 1,

    InvalidName = 2,

    NameAlreadyExists = 3,

    InvalidFaction = 4,

    InvalidClass = 5,

    InvalidGender = 6,

    CharacterLimitReached = 7,

    ServerError = 8
}