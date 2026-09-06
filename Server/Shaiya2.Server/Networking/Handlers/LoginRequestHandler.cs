using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Authentication;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class LoginRequestHandler : IPacketHandler
{
    private readonly AccountService _accountService;

    public PacketId PacketId =>
        PacketId.LoginRequest;

    public LoginRequestHandler(
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
                $"[Security] Login before handshake | SessionId={session.SessionId}"
            );

            session.Peer.Disconnect();

            return;
        }

        string username =
            reader.GetString();

        string password =
            reader.GetString();

        LoginResult result =
            _accountService.Login(
                session.LoginContext,
                username,
                password
            );

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.LoginResponse
        );

        writer.Put(
            (byte)result
        );

        writer.Put(
            session.LoginContext.AccountId ?? 0
        );

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );

        Console.WriteLine(
            $"[Auth] LoginResponse | SessionId={session.SessionId} | Result={result}"
        );
    }
}