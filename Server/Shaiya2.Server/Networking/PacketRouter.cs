using LiteNetLib;
using Shaiya2.Server.Authentication;
using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Handlers;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;
using Shaiya2.Server.World;


namespace Shaiya2.Server.Networking;

public sealed class PacketRouter
{
    private readonly Dictionary<PacketId, IPacketHandler> _handlers = new();

    public PacketRouter(
        WorldManager worldManager)
    {
        var accountService =
            new AccountService();

        var characterService =
            new CharacterService();


        Register(
            new ClientHelloHandler()
        );

        Register(
            new RegisterRequestHandler(
                accountService
            )
        );

        Register(
            new LoginRequestHandler(
                accountService
            )
        );

        Register(
            new CharacterListRequestHandler(
                characterService
            )
        );

        Register(
            new CharacterCreateRequestHandler(
                characterService
            )
        );

        Register(
            new CharacterSelectRequestHandler(
                characterService
            )
        );

        Register(
            new EnterWorldRequestHandler(
                characterService,
                worldManager
            )
        );

        Register(
            new MoveRequestHandler(
                worldManager
            )
        );

        Register(
            new RespawnRequestHandler(
                worldManager
            )
        );

        Register(
            new PlayerAttackRequestHandler(
                worldManager
            )
        );
    }

    private void Register(
        IPacketHandler handler)
    {
        if (!_handlers.TryAdd(
                handler.PacketId,
                handler))
        {
            throw new InvalidOperationException(
                $"Handler already registered for PacketId {handler.PacketId}."
            );
        }

        Console.WriteLine(
            $"[PacketRouter] Registered handler: {handler.PacketId}"
        );
    }

    public void Route(
        PlayerSession session,
        PacketId packetId,
        NetPacketReader reader)
    {
        if (!_handlers.TryGetValue(
                packetId,
                out IPacketHandler? handler))
        {
            Console.WriteLine(
                $"[PacketRouter] No handler registered for PacketId={packetId}"
            );

            return;
        }

        handler.Handle(
            session,
            reader
        );
    }
}