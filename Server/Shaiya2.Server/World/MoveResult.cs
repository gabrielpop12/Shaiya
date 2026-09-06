namespace Shaiya2.Server.World;

public enum MoveResult : byte
{
    Success = 0,
    NotInWorld = 1,
    InvalidMovement = 2,
    ServerError = 3
}