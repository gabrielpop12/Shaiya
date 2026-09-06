using LiteNetLib;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public interface IPacketHandler
{
    PacketId PacketId { get; }

    void Handle(
        PlayerSession session,
        NetPacketReader reader
    );
}