using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.World;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class MoveRequestHandler :
    IPacketHandler
{
    private readonly WorldManager
        _worldManager;

    public PacketId PacketId =>
        PacketId.MoveRequest;

    public MoveRequestHandler(
        WorldManager worldManager)
    {
        _worldManager =
            worldManager;
    }

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        if (session.WorldPlayer == null)
        {
            return;
        }

        float inputX =
            reader.GetFloat();

        float inputZ =
            reader.GetFloat();

        var command =
            new MoveCommand(
                session,
                inputX,
                inputZ
            );

        _worldManager.EnqueueMove(
            command
        );
    }
}