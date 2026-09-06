using Shaiya2.Server.Networking.Sessions;

namespace Shaiya2.Server.World;

public sealed class MoveCommand
{
    public PlayerSession Session { get; }

    public float InputX { get; }

    public float InputZ { get; }

    public MoveCommand(
        PlayerSession session,
        float inputX,
        float inputZ)
    {
        Session = session;

        InputX = inputX;
        InputZ = inputZ;
    }
}