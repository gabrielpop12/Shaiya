namespace Shaiya2.Server.Characters;

public enum CharacterSelectResult : byte
{
    Success = 0,
    NotAuthenticated = 1,
    CharacterNotFound = 2,
    CharacterDoesNotBelongToAccount = 3,
    ServerError = 4
}