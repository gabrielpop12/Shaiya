using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.World;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class EnterWorldRequestHandler : IPacketHandler
{
    private readonly CharacterService _characterService;
    private readonly WorldManager _worldManager;

    public PacketId PacketId =>
        PacketId.EnterWorldRequest;

    public EnterWorldRequestHandler(
        CharacterService characterService,
        WorldManager worldManager)
    {
        _characterService =
            characterService;

        _worldManager =
            worldManager;
    }

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        if (!session.LoginContext.IsAuthenticated ||
            session.LoginContext.AccountId == null)
        {
            SendResponse(
                session,
                EnterWorldResult.NotAuthenticated,
                null
            );

            return;
        }

        if (session.CharacterId == null)
        {
            SendResponse(
                session,
                EnterWorldResult.NoCharacterSelected,
                null
            );

            return;
        }

        if (session.WorldPlayer != null)
        {
            SendResponse(
                session,
                EnterWorldResult.AlreadyInWorld,
                session.WorldPlayer
            );

            return;
        }

        SelectedCharacterData? character =
            _characterService.GetCharacter(
                session.LoginContext.AccountId.Value,
                session.CharacterId.Value
            );

        if (character == null)
        {
            SendResponse(
                session,
                EnterWorldResult.CharacterNotFound,
                null
            );

            return;
        }

        try
        {
            WorldPlayer worldPlayer =
                _worldManager.EnterWorld(
                    session,
                    character
                );

            session.WorldPlayer =
                worldPlayer;


            _worldManager.RefreshVisibility(
                worldPlayer
            );

            _worldManager.SendInitialMonsters(
                worldPlayer
            );

            _worldManager.SendInitialPlayerVitals(
                worldPlayer
            );

            SendResponse(
                session,
                EnterWorldResult.Success,
                worldPlayer
            );

            _worldManager.SendInitialPlayerExperience(
                worldPlayer
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[World] Enter error | SessionId={session.SessionId} | {ex.Message}"
            );

            SendResponse(
                session,
                EnterWorldResult.ServerError,
                null
            );
        }
    }

    private static void SendResponse(
        PlayerSession session,
        EnterWorldResult result,
        WorldPlayer? player)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.EnterWorldResponse
        );

        writer.Put(
            (byte)result
        );

        if (player != null)
        {
            writer.Put(
                player.CharacterId
            );

            writer.Put(
                player.Name
            );

            writer.Put(
                player.MapId
            );

            writer.Put(
                player.X
            );

            writer.Put(
                player.Y
            );

            writer.Put(
                player.Z
            );
        }

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }
}