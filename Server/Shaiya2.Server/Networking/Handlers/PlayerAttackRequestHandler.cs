using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.World;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class PlayerAttackRequestHandler
    : IPacketHandler
{
    private readonly WorldManager
        _worldManager;

    public PacketId PacketId =>
        Shaiya2.Shared.Network.PacketId.PlayerAttackRequest;

    public PlayerAttackRequestHandler(
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

        if (player == null)
        {
            return;
        }

        long monsterEntityId =
            reader.GetLong();

        _worldManager.TryPlayerAttackMonster(
            player,
            monsterEntityId
        );
    }
}