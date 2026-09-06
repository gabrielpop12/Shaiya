namespace Shaiya2.Client.Networking
{
    public enum PacketId : ushort
    {
        ClientHello = 1,
        ServerHello = 2,

        RegisterRequest = 10,
        RegisterResponse = 11,

        LoginRequest = 12,
        LoginResponse = 13,

        CharacterListRequest = 100,
        CharacterListResponse = 101,

        CharacterCreateRequest = 102,
        CharacterCreateResponse = 103,

        CharacterSelectRequest = 104,
        CharacterSelectResponse = 105,

        EnterWorldRequest = 200,
        EnterWorldResponse = 201,

        MoveRequest = 210,
        MoveResponse = 211,

        PlayerSpawn = 220,
        PlayerDespawn = 221,
        PlayerMovementSnapshot = 222,

        WorldSnapshot = 230,

        MonsterSpawn = 300,
        MonsterDespawn = 301,
        MonsterSnapshot = 302,

        PlayerVitalsSnapshot = 310,

        PlayerDeath = 311,
        RespawnRequest = 312,
        RespawnResponse = 313,

        PlayerAttackRequest = 320,
        MonsterVitalsSnapshot = 321,
        MonsterDeath = 322,

        PlayerExperienceSnapshot = 330,
        PlayerLevelUp = 331,
    }
}