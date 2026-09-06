using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.World;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class RespawnRequestHandler
    : IPacketHandler
{
    private readonly WorldManager
        _worldManager;

    public PacketId PacketId =>
        Shaiya2.Shared.Network.PacketId.RespawnRequest;

    public RespawnRequestHandler(
        WorldManager worldManager)
    {
        _worldManager =
            worldManager;
    }

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        WorldPlayer? player =
            session.WorldPlayer;

        byte result = 1;

        if (player != null &&
            _worldManager.RespawnPlayer(player))
        {
            result = 0;
        }

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.RespawnResponse
        );

        writer.Put(result);

        if (player != null)
        {
            writer.Put(player.X);
            writer.Put(player.Y);
            writer.Put(player.Z);
            writer.Put(player.CurrentHp);
            writer.Put(player.MaxHp);
        }
        else
        {
            writer.Put(0f);
            writer.Put(0f);
            writer.Put(0f);
            writer.Put(0);
            writer.Put(0);
        }

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }
}