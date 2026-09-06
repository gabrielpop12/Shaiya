using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shaiya2.Client.Networking
{
    public sealed class GameNetworkClient : MonoBehaviour, INetEventListener
    {
        private const string ClientVersion = "0.0.1";

        private NetManager _netManager;
        private NetPeer _serverPeer;

        [SerializeField]
        private const string TestUsername =
            "TestAccount";

        [SerializeField]
        private const string TestPassword =
            "Shaiya2Test123!";


        private GameObject _localPlayer;

        [SerializeField]
        private float movementSpeed = 4f;

        [SerializeField]
        private float movementSendRate = 20f;

        [SerializeField]
        private float interpolationSpeed = 15f;

        private Vector2 _movementInput;

        private float _movementSendTimer;

        private Vector3 _serverPosition;

        private bool _hasServerPosition;

        private Vector2 _lastSentMovementInput =
            Vector2.zero;

        private readonly Dictionary<long, Vector3>
            _remoteTargetPositions = new();

        private readonly Dictionary<long, GameObject>
            _remotePlayers = new();

        private readonly Dictionary<long, GameObject>
            _monsters = new();

        private readonly Dictionary<long, Vector3>
            _monsterTargetPositions = new();

        private int _localCurrentHp;

        private int _localMaxHp;

        private readonly Dictionary<long, int>
            _playerCurrentHp = new();

        private readonly Dictionary<long, int>
            _playerMaxHp = new();

        private bool _isLocalPlayerDead = false;

        private long? _selectedMonsterEntityId;

        private int _localLevel = 1;

        private long _localExperience = 0;

        private long _localExperienceRequired = 100;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
        }

        private long _firstCharacterId;




        private void Start()
        {
            Debug.Log("[Network] Starting Shaiya2 client...");

            if (!_netManager.Start())
            {
                Debug.LogError(
                    "[Network] Could not start LiteNetLib."
                );

                return;
            }

            Debug.Log(
                "[Network] Connecting to 127.0.0.1:7777..."
            );

            _serverPeer = _netManager.Connect(
                "127.0.0.1",
                7777,
                "Shaiya2ConnectionKey"
            );
        }

        private void Update()
        {
            _netManager?.PollEvents();

            HandleMovementInput();

            HandleMovementNetworkTick();

            HandleMovementInterpolation();

            HandleRemotePlayerInterpolation();

            HandleMonsterInterpolation();

            HandleRespawnInput();

            HandleTargetSelection();

            HandleAttackInput();
        }

        private void OnApplicationQuit()
        {
            _netManager?.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log(
                $"[Network] Connected! PeerId={peer.Id}"
            );

            SendClientHello(peer);
        }

        private void SendClientHello(NetPeer peer)
        {
            var writer = new NetDataWriter();

            writer.Put(
                (ushort)PacketId.ClientHello
            );

            writer.Put(ClientVersion);

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                $"[Protocol] ClientHello sent. Version={ClientVersion}"
            );
        }

        public void OnNetworkReceive(
            NetPeer peer,
            NetPacketReader reader,
            byte channelNumber,
            DeliveryMethod deliveryMethod)
        {
            try
            {
                PacketId packetId =
                    (PacketId)reader.GetUShort();

                switch (packetId)
                {
                    case PacketId.ServerHello:
                        HandleServerHello(reader);
                        break;

                    case PacketId.RegisterResponse:
                        HandleRegisterResponse(
                            peer,
                            reader
                        );
                        break;

                    case PacketId.LoginResponse:
                        HandleLoginResponse(
                            reader
                        );
                        break;

                    case PacketId.CharacterListResponse:
                        HandleCharacterListResponse(
                            peer,
                            reader
                        );
                        break;

                    case PacketId.CharacterCreateResponse:
                        HandleCharacterCreateResponse(
                            peer,
                            reader
                        );
                        break;

                    case PacketId.CharacterSelectResponse:
                        HandleCharacterSelectResponse(
                            reader
                        );
                        break;

                    case PacketId.EnterWorldResponse:
                        HandleEnterWorldResponse(
                            reader
                        );
                        break;

                    case PacketId.MoveResponse:
                        HandleMoveResponse(
                            reader
                        );
                        break;

                    case PacketId.PlayerSpawn:
                        HandlePlayerSpawn(
                            reader
                        );
                        break;

                    case PacketId.PlayerMovementSnapshot:
                        HandlePlayerMovementSnapshot(
                            reader
                        );
                        break;

                    case PacketId.PlayerDespawn:
                        HandlePlayerDespawn(
                            reader
                        );
                        break;

                    case PacketId.WorldSnapshot:
                        HandleWorldSnapshot(
                            reader
                        );
                        break;

                    case PacketId.MonsterSpawn:
                        HandleMonsterSpawn(
                            reader
                        );
                        break;

                    case PacketId.MonsterDespawn:
                        HandleMonsterDespawn(
                            reader
                        );
                        break;

                    case PacketId.MonsterSnapshot:
                        HandleMonsterSnapshot(
                            reader
                        );
                        break;

                    case PacketId.PlayerVitalsSnapshot:
                        HandlePlayerVitalsSnapshot(
                            reader
                        );
                        break;

                    case PacketId.PlayerDeath:
                        HandlePlayerDeath(
                            reader
                        );
                        break;

                    case PacketId.RespawnResponse:
                        HandleRespawnResponse(
                            reader
                        );
                        break;

                    case PacketId.MonsterVitalsSnapshot:
                        HandleMonsterVitalsSnapshot(
                            reader
                        );
                        break;

                    case PacketId.MonsterDeath:
                        HandleMonsterDeath(
                            reader
                        );
                        break;

                    case PacketId.PlayerExperienceSnapshot:
                        HandlePlayerExperienceSnapshot(
                            reader
                        );
                        break;

                    case PacketId.PlayerLevelUp:
                        HandlePlayerLevelUp(
                            reader
                        );
                        break;

                    default:
                        Debug.LogWarning(
                            $"[Protocol] Unknown PacketId={(ushort)packetId}"
                        );
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[Network] Error processing packet: {ex}"
                );
            }
        }

        private void HandleServerHello(
            NetPacketReader reader)
        {
            int sessionId = reader.GetInt();

            string serverVersion =
                reader.GetString();

            string message =
                reader.GetString();

            Debug.Log(
                $"[Protocol] ServerHello received | SessionId={sessionId} | ServerVersion={serverVersion}"
            );

            Debug.Log(
                $"[Server] {message}"
            );

            SendRegisterRequest(
                _serverPeer
            );
        }

        public void OnPeerDisconnected(
            NetPeer peer,
            DisconnectInfo disconnectInfo)
        {
            Debug.LogWarning(
                $"[Network] Disconnected. Reason={disconnectInfo.Reason}"
            );
        }

        public void OnConnectionRequest(
            ConnectionRequest request)
        {
            request.Reject();
        }

        public void OnNetworkError(
            System.Net.IPEndPoint endPoint,
            System.Net.Sockets.SocketError socketError)
        {
            Debug.LogError(
                $"[Network] Error {socketError} from {endPoint}"
            );
        }

        public void OnNetworkLatencyUpdate(
            NetPeer peer,
            int latency)
        {
        }

        public void OnNetworkReceiveUnconnected(
            System.Net.IPEndPoint remoteEndPoint,
            NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
        }

        private void SendRegisterRequest(
        NetPeer peer)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.RegisterRequest
            );

            writer.Put(
                TestUsername
            );

            writer.Put(
                TestPassword
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                $"[Auth] RegisterRequest sent | Username={TestUsername}"
            );
        }

        private void HandleRegisterResponse(
            NetPeer peer,
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            Debug.Log(
                $"[Auth] RegisterResponse | Result={result}"
            );

            if (result == 0)
            {
                Debug.Log(
                    "[Auth] Account registered successfully."
                );

                SendLoginRequest(peer);
                return;
            }

            if (result == 3)
            {
                Debug.Log(
                    "[Auth] Account already exists. Testing login..."
                );

                SendLoginRequest(peer);
                return;
            }

            Debug.LogWarning(
                $"[Auth] Registration failed. Result={result}"
            );
        }

        private void SendLoginRequest(
            NetPeer peer)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.LoginRequest
            );

            writer.Put(
                TestUsername
            );

            writer.Put(
                TestPassword
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                $"[Auth] LoginRequest sent | Username={TestUsername}"
            );
        }

        private void HandleLoginResponse(
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            long accountId =
                reader.GetLong();

            if (result == 0)
            {
                Debug.Log(
                    $"[Auth] LOGIN SUCCESS! AccountId={accountId}"
                );

                SendCharacterListRequest(
                    _serverPeer
                );

                return;
            }

            Debug.LogWarning(
                $"[Auth] Login failed | Result={result}"
            );
        }

        private void SendCharacterListRequest(
            NetPeer peer)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.CharacterListRequest
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                "[Character] CharacterListRequest sent."
            );
        }

        private void HandleCharacterListResponse(
            NetPeer peer,
            NetPacketReader reader)
        {
            byte count =
                reader.GetByte();

            Debug.Log(
                $"[Character] Character list received | Count={count}"
            );

            for (int i = 0; i < count; i++)
            {
                long characterId =
                    reader.GetLong();
                if (i == 0)
                {
                    _firstCharacterId =
                        characterId;
                }

                string characterName =
                    reader.GetString();

                byte faction =
                    reader.GetByte();

                byte characterClass =
                    reader.GetByte();

                byte gender =
                    reader.GetByte();

                int level =
                    reader.GetInt();

                Debug.Log(
                    $"[Character] #{characterId} | {characterName} | Faction={faction} | Class={characterClass} | Gender={gender} | Level={level}"
                );
            }

            if (count == 0)
            {
                Debug.Log(
                    "[Character] No characters found. Creating development character..."
                );

                SendCharacterCreateRequest(
                    peer
                );
            }

            if (count > 0 &&
                _firstCharacterId != 0)
            {
                Debug.Log(
                    $"[Character] Selecting CharacterId={_firstCharacterId}"
                );

                SendCharacterSelectRequest(
                    peer,
                    _firstCharacterId
                );
            }
        }

        private void SendCharacterSelectRequest(
            NetPeer peer,
            long characterId)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.CharacterSelectRequest
            );

            writer.Put(
                characterId
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                $"[Character] CharacterSelectRequest sent | CharacterId={characterId}"
            );
        }

        private void SendCharacterCreateRequest(
            NetPeer peer)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.CharacterCreateRequest
            );

            writer.Put(
                "TestHero"
            );

            // Alliance of Light
            writer.Put(
                (byte)0
            );

            // Fighter
            writer.Put(
                (byte)0
            );

            // Male
            writer.Put(
                (byte)0
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                "[Character] CharacterCreateRequest sent | Name=TestHero"
            );
        }

        private void HandleCharacterCreateResponse(
            NetPeer peer,
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            long characterId =
                reader.GetLong();

            Debug.Log(
                $"[Character] CharacterCreateResponse | Result={result} | CharacterId={characterId}"
            );

            if (result == 0)
            {
                Debug.Log(
                    $"[Character] CHARACTER CREATED SUCCESSFULLY! CharacterId={characterId}"
                );

                SendCharacterListRequest(
                    peer
                );

                return;
            }

            Debug.LogWarning(
                $"[Character] Character creation failed | Result={result}"
            );
        }

        private void HandleCharacterSelectResponse(
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            if (result != 0)
            {
                Debug.LogWarning(
                    $"[Character] Character selection failed | Result={result}"
                );

                return;
            }

            long characterId =
                reader.GetLong();

            string characterName =
                reader.GetString();

            byte faction =
                reader.GetByte();

            byte characterClass =
                reader.GetByte();

            byte gender =
                reader.GetByte();

            int level =
                reader.GetInt();

            long experience =
                reader.GetLong();

            int mapId =
                reader.GetInt();

            float positionX =
                reader.GetFloat();

            float positionY =
                reader.GetFloat();

            float positionZ =
                reader.GetFloat();

            Debug.Log(
                $"[World] CHARACTER SELECTED SUCCESSFULLY!"
            );

            SendEnterWorldRequest(
                _serverPeer
            );

            Debug.Log(
                $"[World] CharacterId={characterId} | Name={characterName} | Level={level}"
            );

            Debug.Log(
                $"[World] MapId={mapId} | Position=({positionX}, {positionY}, {positionZ})"
            );
        }

        private void SendEnterWorldRequest(
            NetPeer peer)
        {
            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.EnterWorldRequest
            );

            peer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                "[World] EnterWorldRequest sent."
            );
        }

        private void HandleEnterWorldResponse(
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            if (result != 0)
            {
                Debug.LogWarning(
                    $"[World] EnterWorld failed | Result={result}"
                );

                return;
            }

            long characterId =
                reader.GetLong();

            string characterName =
                reader.GetString();

            int mapId =
                reader.GetInt();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            Debug.Log(
                $"[World] ENTER WORLD SUCCESS! CharacterId={characterId} | Name={characterName} | MapId={mapId}"
            );

            Debug.Log(
                $"[World] Spawn position=({x}, {y}, {z})"
            );

            SpawnLocalPlayer(
                characterId,
                characterName,
                x,
                y,
                z
            );
        }

        private void SpawnLocalPlayer(
            long characterId,
            string characterName,
            float x,
            float y,
            float z)
        {
            GameObject existing =
                GameObject.Find(
                    $"Player_{characterId}"
                );

            if (existing != null)
            {
                Debug.LogWarning(
                    $"[World] Player_{characterId} already exists."
                );

                return;
            }

            _localPlayer =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            _localPlayer.name =
                $"Player_{characterId}";

            _localPlayer.transform.position =
                new Vector3(
                    x,
                    y,
                    z
                );

            _serverPosition =
                new Vector3(
                    x,
                    y,
                    z
                );

            _hasServerPosition =
                true;

            Debug.Log(
                $"[World] Spawned local player GameObject | Name={characterName}"
            );
        }

        private void HandleMovementInput()
        {
            _movementInput =
                Vector2.zero;

            if (_localPlayer == null ||
                Keyboard.current == null ||
                _isLocalPlayerDead)
            {
                return;
            }

            if (Keyboard.current.wKey.isPressed)
                _movementInput.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                _movementInput.y -= 1f;

            if (Keyboard.current.dKey.isPressed)
                _movementInput.x += 1f;

            if (Keyboard.current.aKey.isPressed)
                _movementInput.x -= 1f;

            if (_movementInput.sqrMagnitude > 1f)
            {
                _movementInput.Normalize();
            }
        }

        private void HandleMovementNetworkTick()
        {
            if (_localPlayer == null ||
                _serverPeer == null)
            {
                return;
            }

            _movementSendTimer +=
                Time.deltaTime;

            float sendInterval =
                1f / movementSendRate;

            bool inputStopped =
                _movementInput == Vector2.zero &&
                _lastSentMovementInput != Vector2.zero;

            if (inputStopped)
            {
                SendMoveRequest(
                    Vector2.zero
                );

                _lastSentMovementInput =
                    Vector2.zero;

                _movementSendTimer =
                    0f;

                return;
            }

            if (_movementInput ==
                Vector2.zero)
            {
                return;
            }

            if (_movementSendTimer <
                sendInterval)
            {
                return;
            }

            _movementSendTimer =
                0f;

            SendMoveRequest(
                _movementInput
            );

            _lastSentMovementInput =
                _movementInput;
        }

        private void HandleMovementInterpolation()
        {
            if (_localPlayer == null ||
                !_hasServerPosition)
            {
                return;
            }

            _localPlayer.transform.position =
                Vector3.Lerp(
                    _localPlayer.transform.position,
                    _serverPosition,
                    interpolationSpeed *
                    Time.deltaTime
                );
        }

        private void SendMoveRequest(
            Vector2 movementInput)
        {
            if (_serverPeer == null)
            {
                return;
            }

            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.MoveRequest
            );

            writer.Put(
                movementInput.x
            );

            writer.Put(
                movementInput.y
            );

            _serverPeer.Send(
                writer,
                DeliveryMethod.Sequenced
            );
        }

        private void HandleMoveResponse(
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            if (result != 0)
            {
                Debug.LogWarning(
                    $"[Movement] Move rejected | Result={result}"
                );

                return;
            }

            long characterId =
                reader.GetLong();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            if (_localPlayer == null)
            {
                return;
            }

            _serverPosition =
                new Vector3(
                    x,
                    y,
                    z
                );

            _hasServerPosition =
                true;
        }

        private void HandlePlayerSpawn(
            NetPacketReader reader)
        {
            long characterId =
                reader.GetLong();

            string characterName =
                reader.GetString();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            if (_remotePlayers.ContainsKey(
                    characterId))
            {
                return;
            }

            GameObject remotePlayer =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            remotePlayer.name =
                $"RemotePlayer_{characterId}_{characterName}";

            remotePlayer.transform.position =
                new Vector3(
                    x,
                    y,
                    z
                );

            _remotePlayers.Add(
                characterId,
                remotePlayer
            );

            Debug.Log(
                $"[World] Remote player spawned | CharacterId={characterId} | Name={characterName}"
            );
        }

        private void HandlePlayerMovementSnapshot(
            NetPacketReader reader)
        {
            long characterId =
                reader.GetLong();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            if (!_remotePlayers.ContainsKey(
                    characterId))
            {
                return;
            }

            _remoteTargetPositions[
                characterId
            ] =
                new Vector3(
                    x,
                    y,
                    z
                );
        }

        private void HandleRemotePlayerInterpolation()
        {
            foreach (var pair in
                _remotePlayers)
            {
                long characterId =
                    pair.Key;

                GameObject player =
                    pair.Value;

                if (!_remoteTargetPositions.TryGetValue(
                        characterId,
                        out Vector3 targetPosition))
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        player.transform.position,
                        targetPosition
                    );

                if (distance < 0.01f)
                {
                    player.transform.position =
                        targetPosition;

                    continue;
                }

                player.transform.position =
                    Vector3.Lerp(
                        player.transform.position,
                        targetPosition,
                        interpolationSpeed *
                        Time.deltaTime
                    );
            }
        }

        private void HandlePlayerDespawn(
            NetPacketReader reader)
        {
            long characterId =
                reader.GetLong();

            if (!_remotePlayers.TryGetValue(
                    characterId,
                    out GameObject player))
            {
                return;
            }

            Destroy(
                player
            );

            _remotePlayers.Remove(
                characterId
            );

            _remoteTargetPositions.Remove(
                characterId
            );

            Debug.Log(
                $"[World] Remote player despawned | CharacterId={characterId}"
            );
        }

        private void HandleWorldSnapshot(
            NetPacketReader reader)
        {
            int playerCount =
                reader.GetInt();

            for (int i = 0;
                 i < playerCount;
                 i++)
            {
                long characterId =
                    reader.GetLong();

                float x =
                    reader.GetFloat();

                float y =
                    reader.GetFloat();

                float z =
                    reader.GetFloat();

                Vector3 position =
                    new Vector3(
                        x,
                        y,
                        z
                    );

                if (_localPlayer != null &&
                    characterId ==
                    _firstCharacterId)
                {
                    _serverPosition =
                        position;

                    _hasServerPosition =
                        true;

                    continue;
                }

                if (_remotePlayers.ContainsKey(
                        characterId))
                {
                    _remoteTargetPositions[
                        characterId
                    ] =
                        position;
                }
            }
        }

        private void HandleMonsterSpawn(
            NetPacketReader reader)
        {
            long entityId =
                reader.GetLong();

            int monsterId =
                reader.GetInt();

            string monsterName =
                reader.GetString();

            int level =
                reader.GetInt();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            int currentHp =
                reader.GetInt();

            int maxHp =
                reader.GetInt();

            if (_monsters.ContainsKey(
                    entityId))
            {
                return;
            }

            GameObject monster =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            monster.name =
                $"Monster_{entityId}_{monsterName}";

            monster.transform.position =
                new Vector3(
                    x,
                    y,
                    z
                );

            // Lo hacemos un poco más bajo y ancho
            // para distinguirlo del jugador.
            monster.transform.localScale =
                new Vector3(
                    1.2f,
                    0.8f,
                    1.2f
                );

            MonsterView monsterView =
                monster.AddComponent<MonsterView>();

            monsterView.Initialize(
                entityId
            );

            _monsters.Add(
                entityId,
                monster
            );

            _monsterTargetPositions[
                entityId
            ] =
                monster.transform.position;

            Debug.Log(
                $"[Monster] Spawned | EntityId={entityId} | MonsterId={monsterId} | Name={monsterName} | Level={level} | HP={currentHp}/{maxHp}"
            );
        }

        private void HandleMonsterDespawn(
            NetPacketReader reader)
        {
            long entityId =
                reader.GetLong();

            if (!_monsters.TryGetValue(
                    entityId,
                    out GameObject monster))
            {
                return;
            }

            Destroy(
                monster
            );

            _monsters.Remove(
                entityId
            );

            _monsterTargetPositions.Remove(
                entityId
            );

            Debug.Log(
                $"[Monster] Despawned | EntityId={entityId}"
            );
        }

        private void HandleMonsterSnapshot(
            NetPacketReader reader)
        {
            long entityId =
                reader.GetLong();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            if (!_monsters.ContainsKey(
                    entityId))
            {
                return;
            }

            _monsterTargetPositions[
                entityId
            ] =
                new Vector3(
                    x,
                    y,
                    z
                );
        }

        private void HandleMonsterInterpolation()
        {
            foreach (var pair in
                     _monsters)
            {
                long entityId =
                    pair.Key;

                GameObject monster =
                    pair.Value;

                if (!_monsterTargetPositions.TryGetValue(
                        entityId,
                        out Vector3 targetPosition))
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        monster.transform.position,
                        targetPosition
                    );

                if (distance < 0.01f)
                {
                    monster.transform.position =
                        targetPosition;

                    continue;
                }

                monster.transform.position =
                    Vector3.Lerp(
                        monster.transform.position,
                        targetPosition,
                        interpolationSpeed *
                        Time.deltaTime
                    );
            }
        }

        private void HandlePlayerVitalsSnapshot(
            NetPacketReader reader)
        {
            long characterId =
                reader.GetLong();

            int currentHp =
                reader.GetInt();

            int maxHp =
                reader.GetInt();

            _playerCurrentHp[
                characterId
            ] =
                currentHp;

            _playerMaxHp[
                characterId
            ] =
                maxHp;

            if (characterId ==
                _firstCharacterId)
            {
                _localCurrentHp =
                    currentHp;

                _localMaxHp =
                    maxHp;

                Debug.Log(
                    $"[Combat] Local HP = {_localCurrentHp}/{_localMaxHp}"
                );
            }
            else
            {
                Debug.Log(
                    $"[Combat] Player {characterId} HP = {currentHp}/{maxHp}"
                );
            }
        }

        private void OnGUI()
        {
            if (_localPlayer == null)
            {
                return;
            }

            GUI.Label(
                new Rect(
                    10,
                    35,
                    400,
                    25
                ),
                $"Level: {_localLevel}"
            );

            GUI.Label(
                new Rect(
                    10,
                    55,
                    400,
                    25
                ),
                $"XP: {_localExperience}/{_localExperienceRequired}"
            );

            GUI.Label(
                new Rect(
                    20,
                    20,
                    300,
                    30
                ),
                $"HP: {_localCurrentHp} / {_localMaxHp}"
            );

            if (_isLocalPlayerDead)
            {
                GUI.Label(
                    new Rect(
                        20,
                        50,
                        400,
                        30
                    ),
                    "YOU DIED - Press R to respawn"
                );
            }
        }

        private void HandlePlayerDeath(
            NetPacketReader reader)
        {
            long characterId =
                reader.GetLong();

            if (characterId ==
                _firstCharacterId)
            {
                _isLocalPlayerDead =
                    true;

                _movementInput =
                    Vector2.zero;

                _lastSentMovementInput =
                    Vector2.zero;

                Debug.Log(
                    "[Combat] YOU DIED."
                );

                return;
            }

            Debug.Log(
                $"[Combat] Player died | CharacterId={characterId}"
            );
        }

        private void HandleRespawnInput()
        {
            if (!_isLocalPlayerDead ||
                Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.rKey.wasPressedThisFrame)
            {
                return;
            }

            SendRespawnRequest();
        }

        private void SendRespawnRequest()
        {
            if (_serverPeer == null)
            {
                return;
            }

            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.RespawnRequest
            );

            _serverPeer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log(
                "[World] RespawnRequest sent."
            );
        }

        private void HandleRespawnResponse(
            NetPacketReader reader)
        {
            byte result =
                reader.GetByte();

            float x =
                reader.GetFloat();

            float y =
                reader.GetFloat();

            float z =
                reader.GetFloat();

            int currentHp =
                reader.GetInt();

            int maxHp =
                reader.GetInt();

            if (result != 0)
            {
                Debug.LogWarning(
                    $"[World] Respawn failed | Result={result}"
                );

                return;
            }

            _isLocalPlayerDead =
                false;

            _localCurrentHp =
                currentHp;

            _localMaxHp =
                maxHp;

            _serverPosition =
                new Vector3(
                    x,
                    y,
                    z
                );

            _hasServerPosition =
                true;

            if (_localPlayer != null)
            {
                _localPlayer.transform.position =
                    _serverPosition;
            }

            Debug.Log(
                $"[World] RESPAWN SUCCESS | HP={currentHp}/{maxHp} | Position=({x}, {y}, {z})"
            );
        }

        private void HandleTargetSelection()
        {
            if (Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Camera camera =
                Camera.main;

            if (camera == null)
            {
                return;
            }

            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            Ray ray =
                camera.ScreenPointToRay(
                    mousePosition
                );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    1000f))
            {
                return;
            }

            MonsterView? monsterView =
                hit.collider.GetComponent<MonsterView>();

            if (monsterView == null)
            {
                return;
            }

            _selectedMonsterEntityId =
                monsterView.EntityId;

            Debug.Log(
                $"[Combat] Monster selected | EntityId={monsterView.EntityId}"
            );
        }

        private void HandleAttackInput()
        {
            if (_isLocalPlayerDead)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return;
            }

            if (!_selectedMonsterEntityId.HasValue)
            {
                return;
            }

            SendPlayerAttackRequest(
                _selectedMonsterEntityId.Value
            );
        }

        private void SendPlayerAttackRequest(
            long monsterEntityId)
        {
            if (_serverPeer == null)
            {
                return;
            }

            var writer =
                new NetDataWriter();

            writer.Put(
                (ushort)PacketId.PlayerAttackRequest
            );

            writer.Put(
                monsterEntityId
            );

            _serverPeer.Send(
                writer,
                DeliveryMethod.ReliableOrdered
            );
        }

        private void HandleMonsterVitalsSnapshot(
            NetPacketReader reader)
        {
            long entityId =
                reader.GetLong();

            int currentHp =
                reader.GetInt();

            int maxHp =
                reader.GetInt();

            Debug.Log(
                $"[Combat] Monster {entityId} HP = {currentHp}/{maxHp}"
            );
        }

        private void HandleMonsterDeath(
            NetPacketReader reader)
        {
            long entityId =
                reader.GetLong();

            if (_monsters.TryGetValue(
                    entityId,
                    out GameObject? monster))
            {
                Destroy(
                    monster
                );

                _monsters.Remove(
                    entityId
                );
            }

            _monsterTargetPositions.Remove(
                entityId
            );

            if (_selectedMonsterEntityId ==
                entityId)
            {
                _selectedMonsterEntityId =
                    null;
            }

            Debug.Log(
                $"[Combat] Monster died | EntityId={entityId}"
            );
        }

        private void HandlePlayerExperienceSnapshot(
            NetPacketReader reader)
        {
            _localLevel =
                reader.GetInt();

            _localExperience =
                reader.GetLong();

            _localExperienceRequired =
                reader.GetLong();

            Debug.Log(
                $"[XP] Level {_localLevel} | XP {_localExperience}/{_localExperienceRequired}"
            );
        }

        private void HandlePlayerLevelUp(
            NetPacketReader reader)
        {
            int newLevel =
                reader.GetInt();

            _localLevel =
                newLevel;

            Debug.Log(
                $"[XP] LEVEL UP! New Level = {newLevel}"
            );
        }
    }
}