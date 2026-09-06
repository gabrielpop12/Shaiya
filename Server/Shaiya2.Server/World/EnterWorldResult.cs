namespace Shaiya2.Server.World;

public enum EnterWorldResult : byte
{
    Success = 0,
    NotAuthenticated = 1,
    NoCharacterSelected = 2,
    CharacterNotFound = 3,
    AlreadyInWorld = 4,
    ServerError = 5
}