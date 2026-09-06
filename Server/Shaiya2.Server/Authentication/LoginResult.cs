namespace Shaiya2.Server.Authentication;

public enum LoginResult : byte
{
    Success = 0,
    InvalidCredentials = 1,
    AccountBanned = 2,
    AccountSuspended = 3,
    AlreadyAuthenticated = 4,
    ServerError = 5
}