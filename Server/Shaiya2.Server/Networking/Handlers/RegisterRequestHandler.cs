using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Authentication;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class RegisterRequestHandler : IPacketHandler
{
    private readonly AccountService _accountService;

    public PacketId PacketId =>
        PacketId.RegisterRequest;

    public RegisterRequestHandler(
        AccountService accountService)
    {
        _accountService =
            accountService;
    }

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        if (!session.HandshakeCompleted)
        {
            Console.WriteLine(
                $"[Security] Register before handshake | SessionId={session.SessionId}"
            );

            session.Peer.Disconnect();

            return;
        }

        string username =
            reader.GetString();

        string password =
            reader.GetString();

        RegisterResult result =
            _accountService.Register(
                username,
                password
            );

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.RegisterResponse
        );

        writer.Put(
            (byte)result
        );

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );

        Console.WriteLine(
            $"[Auth] RegisterResponse | SessionId={session.SessionId} | Result={result}"
        );
    }
}