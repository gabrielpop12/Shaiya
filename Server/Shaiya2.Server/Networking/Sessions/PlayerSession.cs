using LiteNetLib;
using Shaiya2.Server.Authentication;
using Shaiya2.Server.World;

namespace Shaiya2.Server.Networking.Sessions;

public sealed class PlayerSession
{
    public int SessionId { get; }

    public NetPeer Peer { get; }

    public DateTime ConnectedAt { get; }

    public bool HandshakeCompleted { get; set; }

    public PlayerLoginContext LoginContext { get; }

    public long? CharacterId { get; set; }

    public WorldPlayer? WorldPlayer { get; set; }

    public PlayerSession(
        int sessionId,
        NetPeer peer)
    {
        SessionId = sessionId;
        Peer = peer;

        ConnectedAt =
            DateTime.UtcNow;

        HandshakeCompleted =
            false;

        LoginContext =
            new PlayerLoginContext();
    }
}