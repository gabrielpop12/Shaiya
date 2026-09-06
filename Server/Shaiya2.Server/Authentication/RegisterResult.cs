namespace Shaiya2.Server.Authentication;

public enum RegisterResult : byte
{
    Success = 0,
    InvalidUsername = 1,
    InvalidPassword = 2,
    UsernameAlreadyExists = 3,
    ServerError = 4
}