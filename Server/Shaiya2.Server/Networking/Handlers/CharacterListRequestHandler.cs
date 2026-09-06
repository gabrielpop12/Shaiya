using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Shared.Network;

namespace Shaiya2.Server.Networking.Handlers;

public sealed class CharacterListRequestHandler : IPacketHandler
{
    private readonly CharacterService _characterService;

    public PacketId PacketId =>
        PacketId.CharacterListRequest;

    public CharacterListRequestHandler(
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
            Console.WriteLine(
                $"[Security] CharacterList before authentication | SessionId={session.SessionId}"
            );

            return;
        }

        IReadOnlyList<CharacterSummary> characters =
            _characterService.GetCharacters(
                session.LoginContext.AccountId.Value
            );

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.CharacterListResponse
        );

        writer.Put(
            (byte)characters.Count
        );

        foreach (CharacterSummary character in characters)
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
        }

        session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );

        Console.WriteLine(
            $"[Character] CharacterListResponse | SessionId={session.SessionId} | Count={characters.Count}"
        );
    }
}