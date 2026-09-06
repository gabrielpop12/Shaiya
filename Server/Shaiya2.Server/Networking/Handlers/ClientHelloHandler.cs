using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class ClientHelloHandler : IPacketHandler
{
    private const string ServerVersion = "0.0.1";
    private const string SupportedClientVersion = "0.0.1";

    public PacketId PacketId => PacketId.ClientHello;

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        string clientVersion = reader.GetString();

        Console.WriteLine(
            $"[Protocol] ClientHello | SessionId={session.SessionId} | Version={clientVersion}"
        );

        if (clientVersion != SupportedClientVersion)
        {
            Console.WriteLine(
                $"[Protocol] Unsupported client version: {clientVersion}"
            );

            session.Peer.Disconnect();

            return;
        }

        session.HandshakeCompleted = true;

        SendServerHello(session);
    }

    private static void SendServerHello(
        PlayerSession session)
    {
        var writer = new NetDataWriter();

        writer.Put(
            (ushort)PacketId.ServerHello
        );

        writer.Put(
            session.SessionId
        );

        writer.Put(
            ServerVersion
        );

        writer.Put(
            "Welcome to Shaiya2."
        );

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );

        Console.WriteLine(
            $"[Protocol] ServerHello sent | SessionId={session.SessionId}"
        );
    }
}