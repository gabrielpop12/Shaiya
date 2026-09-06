using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class CharacterSelectRequestHandler : IPacketHandler
{
    private readonly CharacterService _characterService;

    public PacketId PacketId =>
        PacketId.CharacterSelectRequest;

    public CharacterSelectRequestHandler(
        CharacterService characterService)
    {
        _characterService =
            characterService;
    }

    public void Handle(
        PlayerSession session,
        NetPacketReader reader)
    {
        if (!session.HandshakeCompleted ||
            !session.LoginContext.IsAuthenticated ||
            session.LoginContext.AccountId == null)
        {
            SendResponse(
                session,
                CharacterSelectResult.NotAuthenticated,
                null
            );

            return;
        }

        long characterId =
            reader.GetLong();

        CharacterSelectResult result =
            _characterService.SelectCharacter(
                session.LoginContext.AccountId.Value,
                characterId,
                out SelectedCharacterData? selectedCharacter
            );

        if (result == CharacterSelectResult.Success &&
            selectedCharacter != null)
        {
            session.CharacterId =
                selectedCharacter.Id;

            Console.WriteLine(
                $"[Character] Selected | SessionId={session.SessionId} | CharacterId={selectedCharacter.Id} | Name={selectedCharacter.Name}"
            );
        }

        SendResponse(
            session,
            result,
            selectedCharacter
        );
    }

    private static void SendResponse(
        PlayerSession session,
        CharacterSelectResult result,
        SelectedCharacterData? character)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.CharacterSelectResponse
        );

        writer.Put(
            (byte)result
        );

        if (result == CharacterSelectResult.Success &&
            character != null)
        {
            writer.Put(character.Id);
            writer.Put(character.Name);

            writer.Put(
                (byte)character.Faction
            );

            writer.Put(
                (byte)character.Class
            );

            writer.Put(
                (byte)character.Gender
            );

            writer.Put(character.Level);
            writer.Put(character.Experience);

            writer.Put(character.MapId);

            writer.Put(character.PositionX);
            writer.Put(character.PositionY);
            writer.Put(character.PositionZ);
        }

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }
}