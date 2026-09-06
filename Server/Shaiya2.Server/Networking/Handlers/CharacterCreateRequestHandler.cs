using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.Persistence.Entities;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class CharacterCreateRequestHandler : IPacketHandler
{
    private readonly CharacterService _characterService;

    public PacketId PacketId =>
        PacketId.CharacterCreateRequest;

    public CharacterCreateRequestHandler(
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
                CharacterCreateResult.NotAuthenticated,
                0
            );

            return;
        }

        string name =
            reader.GetString();

        CharacterFaction faction =
            (CharacterFaction)reader.GetByte();

        CharacterClass characterClass =
            (CharacterClass)reader.GetByte();

        CharacterGender gender =
            (CharacterGender)reader.GetByte();

        CharacterCreateResult result =
            _characterService.CreateCharacter(
                session.LoginContext.AccountId.Value,
                name,
                faction,
                characterClass,
                gender,
                out long characterId
            );

        SendResponse(
            session,
            result,
            characterId
        );

        Console.WriteLine(
            $"[Character] CreateResponse | SessionId={session.SessionId} | Result={result} | CharacterId={characterId}"
        );
    }

    private static void SendResponse(
        PlayerSession session,
        CharacterCreateResult result,
        long characterId)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.CharacterCreateResponse
        );

        writer.Put(
            (byte)result
        );

        writer.Put(
            characterId
        );

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }
}