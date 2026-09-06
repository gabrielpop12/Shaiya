using Shaiya2.Server.Characters;
using Shaiya2.Server.Networking.Sessions;
using LiteNetLib;
using LiteNetLib.Utils;
using Shaiya2.Shared.Network;
using System.Collections.Concurrent;
using System.Linq;

namespace Shaiya2.Server.World;

public sealed class WorldManager
{
    private readonly Dictionary<int, MapInstance>
        _maps = new();

    private const float CellSize = 20f;

    private const float PlayerMovementSpeed = 4f;

    private readonly ConcurrentQueue<MoveCommand>
        _movementCommands = new();

    private long _nextMonsterEntityId =
        10000;

    private readonly List<PendingMonsterRespawn>
        _pendingMonsterRespawns = new();

    private readonly CharacterService
    _characterService = new();

    public WorldManager()
    {
        SpawnMonster(
            mapId: 1,
            monsterId: 1,
            name: "Wolf",
            level: 1,
            x: 5f,
            y: 0f,
            z: 5f,
            maxHp: 100
        );
    }

    public WorldPlayer EnterWorld(
        PlayerSession session,
        SelectedCharacterData character)
    {
        if (!_maps.TryGetValue(
                character.MapId,
                out MapInstance? map))
        {
            map =
                new MapInstance(
                    character.MapId
                );

            _maps.Add(
                character.MapId,
                map
            );

            Console.WriteLine(
                $"[World] Created MapInstance | MapId={character.MapId}"
            );
        }

        var player =
            new WorldPlayer(
                character.Id,
                character.Name,
                session,
                character.MapId,
                character.PositionX,
                character.PositionY,
                character.PositionZ
            )
            {
                Level = character.Level,
                Experience = character.Experience
            };

        map.AddPlayer(
            player
        );

        Console.WriteLine(
            $"[World] Player entered | CharacterId={character.Id} | MapId={character.MapId}"
        );

        return player;
    }

    public void Tick(
        float deltaTime)
    {
        ProcessMovementCommands();

        SimulatePlayerMovement(
            deltaTime
        );

        SimulateMonsters(
            deltaTime
        );

        ProcessMonsterRespawns(
            deltaTime
        );
    }

    public MapInstance? GetMap(
        int mapId)
    {
        _maps.TryGetValue(
            mapId,
            out MapInstance? map
        );

        return map;
    }

    public void BroadcastPlayerSpawn(
        WorldPlayer newPlayer)
    {
        MapInstance? map =
            GetMap(newPlayer.MapId);

        if (map == null)
            return;

        foreach (WorldPlayer player in map.Players)
        {
            if (player.CharacterId ==
                newPlayer.CharacterId)
            {
                continue;
            }

            SendPlayerSpawn(
                player,
                newPlayer
            );

            SendPlayerSpawn(
                newPlayer,
                player
            );
        }
    }

    private static void SendPlayerSpawn(
        WorldPlayer receiver,
        WorldPlayer spawnedPlayer)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerSpawn
        );

        writer.Put(
            spawnedPlayer.CharacterId
        );

        writer.Put(
            spawnedPlayer.Name
        );

        writer.Put(
            spawnedPlayer.X
        );

        writer.Put(
            spawnedPlayer.Y
        );

        writer.Put(
            spawnedPlayer.Z
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    public void BroadcastMovement(
        WorldPlayer movingPlayer)
    {
        MapInstance? map =
            GetMap(
                movingPlayer.MapId
            );

        if (map == null)
            return;

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerMovementSnapshot
        );

        writer.Put(
            movingPlayer.CharacterId
        );

        writer.Put(
            movingPlayer.X
        );

        writer.Put(
            movingPlayer.Y
        );

        writer.Put(
            movingPlayer.Z
        );

        foreach (WorldPlayer player in
            GetNearbyPlayers(
                map,
                movingPlayer
            ))
        {
            player.Session.Peer.Send(
                writer,
                DeliveryMethod.Sequenced
            );
        }
    }

    public void RemovePlayer(
    WorldPlayer leavingPlayer)
    {
        MapInstance? map =
            GetMap(
                leavingPlayer.MapId
            );

        if (map == null)
            return;

        map.RemovePlayer(
            leavingPlayer.CharacterId
        );

        foreach (WorldPlayer player in
            map.Players)
        {
            if (player.VisiblePlayers.Remove(
                    leavingPlayer.CharacterId))
            {
                SendPlayerDespawn(
                    player,
                    leavingPlayer.CharacterId
                );
            }
        }

        leavingPlayer.VisiblePlayers.Clear();

        Console.WriteLine(
            $"[World] Player left | CharacterId={leavingPlayer.CharacterId} | MapId={leavingPlayer.MapId}"
        );
    }

    private static SpatialCell GetCell(
        float x,
        float z)
    {
        int cellX =
            (int)MathF.Floor(
                x / CellSize
            );

        int cellZ =
            (int)MathF.Floor(
                z / CellSize
            );

        return new SpatialCell(
            cellX,
            cellZ
        );
    }

    private static IEnumerable<WorldPlayer>
        GetNearbyPlayers(
            MapInstance map,
            WorldPlayer sourcePlayer)
    {
        SpatialCell sourceCell =
            GetCell(
                sourcePlayer.X,
                sourcePlayer.Z
            );

        foreach (WorldPlayer player in map.Players)
        {
            if (player.CharacterId ==
                sourcePlayer.CharacterId)
            {
                continue;
            }

            SpatialCell playerCell =
                GetCell(
                    player.X,
                    player.Z
                );

            int deltaX =
                Math.Abs(
                    playerCell.X -
                    sourceCell.X
                );

            int deltaZ =
                Math.Abs(
                    playerCell.Z -
                    sourceCell.Z
                );

            if (deltaX <= 1 &&
                deltaZ <= 1)
            {
                yield return player;
            }
        }
    }

    public void UpdatePlayerVisibility(
        WorldPlayer sourcePlayer)
    {
        MapInstance? map =
            GetMap(
                sourcePlayer.MapId
            );

        if (map == null)
            return;

        HashSet<long> nearbyIds =
            new();

        foreach (WorldPlayer otherPlayer in
            GetNearbyPlayers(
                map,
                sourcePlayer
            ))
        {
            nearbyIds.Add(
                otherPlayer.CharacterId
            );

            if (!sourcePlayer.VisiblePlayers.Contains(
                    otherPlayer.CharacterId))
            {
                SendPlayerSpawn(
                    sourcePlayer,
                    otherPlayer
                );

                sourcePlayer.VisiblePlayers.Add(
                    otherPlayer.CharacterId
                );
            }
        }

        List<long> noLongerVisible =
            new();

        foreach (long characterId in
            sourcePlayer.VisiblePlayers)
        {
            if (!nearbyIds.Contains(
                    characterId))
            {
                noLongerVisible.Add(
                    characterId
                );
            }
        }

        foreach (long characterId in
            noLongerVisible)
        {
            SendPlayerDespawn(
                sourcePlayer,
                characterId
            );

            sourcePlayer.VisiblePlayers.Remove(
                characterId
            );
        }
    }

    private static void SendPlayerDespawn(
        WorldPlayer receiver,
        long characterId)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerDespawn
        );

        writer.Put(
            characterId
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    public void RefreshVisibility(
        WorldPlayer source)
    {
        MapInstance? map =
            GetMap(
                source.MapId
            );

        if (map == null)
        {
            return;
        }

        HashSet<long> nearbyIds =
            new();

        foreach (WorldPlayer nearby in
                 map.GetNearbyPlayers(source))
        {
            if (nearby.CharacterId ==
                source.CharacterId)
            {
                continue;
            }

            nearbyIds.Add(
                nearby.CharacterId
            );

            // Source empieza a ver al otro.
            if (source.VisiblePlayers.Add(
                    nearby.CharacterId))
            {
                SendPlayerSpawn(
                    source,
                    nearby
                );
            }

            // El otro empieza a ver a Source.
            if (nearby.VisiblePlayers.Add(
                    source.CharacterId))
            {
                SendPlayerSpawn(
                    nearby,
                    source
                );
            }
        }

        long[] previouslyVisible =
            source.VisiblePlayers.ToArray();

        foreach (long characterId in
                 previouslyVisible)
        {
            if (nearbyIds.Contains(
                    characterId))
            {
                continue;
            }

            if (!map.TryGetPlayer(
                    characterId,
                    out WorldPlayer? other) ||
                other == null)
            {
                source.VisiblePlayers.Remove(
                    characterId
                );

                continue;
            }

            source.VisiblePlayers.Remove(
                characterId
            );

            SendPlayerDespawn(
                source,
                characterId
            );

            if (other.VisiblePlayers.Remove(
                    source.CharacterId))
            {
                SendPlayerDespawn(
                    other,
                    source.CharacterId
                );
            }
        }
    }

    public void EnqueueMove(
        MoveCommand command)
    {
        _movementCommands.Enqueue(
            command
        );
    }

    private void ProcessMovementCommands()
    {
        while (_movementCommands.TryDequeue(
                   out MoveCommand? command))
        {
            PlayerSession session =
                command.Session;

            WorldPlayer? player =
                session.WorldPlayer;

            if (player == null)
            {
                continue;
            }

            if (player.IsDead)
            {
                player.MovementInputX = 0f;
                player.MovementInputZ = 0f;

                continue;
            }

            float inputX =
                Math.Clamp(
                    command.InputX,
                    -1f,
                    1f
                );

            float inputZ =
                Math.Clamp(
                    command.InputZ,
                    -1f,
                    1f
                );

            float magnitudeSquared =
                inputX * inputX +
                inputZ * inputZ;

            if (magnitudeSquared > 1f)
            {
                float magnitude =
                    MathF.Sqrt(
                        magnitudeSquared
                    );

                inputX /= magnitude;
                inputZ /= magnitude;
            }

            player.MovementInputX =
                inputX;

            player.MovementInputZ =
                inputZ;
        }
    }

    private void SimulatePlayerMovement(
        float deltaTime)
    {
        foreach (MapInstance map in _maps.Values)
        {
            foreach (WorldPlayer player in map.Players)
            {
                if (player.AttackCooldownRemaining > 0f)
                {
                    player.AttackCooldownRemaining -= deltaTime;

                    if (player.AttackCooldownRemaining < 0f)
                    {
                        player.AttackCooldownRemaining = 0f;
                    }
                }

                if (player.IsDead)
                {
                    player.MovementInputX = 0f;
                    player.MovementInputZ = 0f;

                    continue;
                }

                float inputX =
                    player.MovementInputX;

                float inputZ =
                    player.MovementInputZ;

                if (inputX == 0f &&
                    inputZ == 0f)
                {
                    continue;
                }

                float movementDistance =
                    PlayerMovementSpeed *
                    deltaTime;

                player.X +=
                    inputX *
                    movementDistance;

                player.Z +=
                    inputZ *
                    movementDistance;

                map.UpdatePlayerCell(
                    player
                );

                RefreshVisibility(
                    player
                );
            }
        }
    }

    public void SendWorldSnapshots()
    {
        foreach (MapInstance map in
                 _maps.Values)
        {
            foreach (WorldPlayer receiver in
                     map.Players)
            {
                SendWorldSnapshot(
                    map,
                    receiver
                );

                foreach (WorldMonster monster in
                         map.Monsters)
                {
                    SendMonsterSnapshot(
                        receiver,
                        monster
                    );
                }
            }
        }
    }

    private static void SendWorldSnapshot(
        MapInstance map,
        WorldPlayer receiver)
    {
        var visiblePlayers =
            map.Players
                .Where(
                    player =>
                        player.CharacterId ==
                        receiver.CharacterId ||
                        receiver.VisiblePlayers.Contains(
                            player.CharacterId
                        )
                )
                .ToList();

        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.WorldSnapshot
        );

        writer.Put(
            visiblePlayers.Count
        );

        foreach (WorldPlayer player in
            visiblePlayers)
        {
            writer.Put(
                player.CharacterId
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

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.Sequenced
        );
    }

    private static void SendMoveResponse(
        PlayerSession session,
        MoveResult result,
        WorldPlayer player)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.MoveResponse
        );

        writer.Put(
            (byte)result
        );

        writer.Put(
            player.CharacterId
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

        session.Peer.Send(
            writer,
            DeliveryMethod.Sequenced
        );
    }

    public WorldMonster SpawnMonster(
        int mapId,
        int monsterId,
        string name,
        int level,
        float x,
        float y,
        float z,
        int maxHp)
    {
        MapInstance map =
            GetOrCreateMap(
                mapId
            );

        long entityId =
            ++_nextMonsterEntityId;

        var monster =
            new WorldMonster(
                entityId,
                monsterId,
                name,
                level,
                mapId,
                x,
                y,
                z,
                maxHp
            );

        map.AddMonster(
            monster
        );

        Console.WriteLine(
            $"[Monster] Spawned | EntityId={entityId} | MonsterId={monsterId} | Name={name} | Map={mapId} | Position=({x}, {y}, {z})"
        );

        return monster;
    }

    private static void SendMonsterSpawn(
        WorldPlayer receiver,
        WorldMonster monster)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.MonsterSpawn
        );

        writer.Put(
            monster.EntityId
        );

        writer.Put(
            monster.MonsterId
        );

        writer.Put(
            monster.Name
        );

        writer.Put(
            monster.Level
        );

        writer.Put(
            monster.X
        );

        writer.Put(
            monster.Y
        );

        writer.Put(
            monster.Z
        );

        writer.Put(
            monster.CurrentHp
        );

        writer.Put(
            monster.MaxHp
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    public void SendInitialMonsters(
        WorldPlayer player)
    {
        MapInstance? map =
            GetMap(
                player.MapId
            );

        if (map == null)
        {
            return;
        }

        foreach (WorldMonster monster in
                 map.Monsters)
        {
            SendMonsterSpawn(
                player,
                monster
            );
        }
    }

    private MapInstance GetOrCreateMap(
        int mapId)
    {
        if (_maps.TryGetValue(
                mapId,
                out MapInstance? map))
        {
            return map;
        }

        map =
            new MapInstance(
                mapId
            );

        _maps.Add(
            mapId,
            map
        );

        Console.WriteLine(
            $"[World] Created MapInstance | MapId={mapId}"
        );

        return map;
    }

    private void SimulateMonsters(
        float deltaTime)
    {
        foreach (MapInstance map in
                 _maps.Values)
        {
            foreach (WorldMonster monster in
                     map.Monsters)
            {
                if (monster.AttackCooldownRemaining > 0f)
                {
                    monster.AttackCooldownRemaining -=
                        deltaTime;

                    if (monster.AttackCooldownRemaining < 0f)
                    {
                        monster.AttackCooldownRemaining =
                            0f;
                    }
                }

                WorldPlayer? target =
                    FindMonsterTarget(
                        map,
                        monster
                    );

                if (target == null)
                {
                    monster.TargetCharacterId =
                        null;

                    continue;
                }

                monster.TargetCharacterId =
                    target.CharacterId;

                float deltaX =
                    target.X -
                    monster.X;

                float deltaZ =
                    target.Z -
                    monster.Z;

                float distanceSquared =
                    deltaX * deltaX +
                    deltaZ * deltaZ;

                float attackRangeSquared =
                    monster.AttackRange *
                    monster.AttackRange;

                // ==================================
                // EL MONSTRUO YA ESTÁ EN RANGO
                // ==================================
                if (distanceSquared <=
                    attackRangeSquared)
                {
                    TryMonsterAttack(
                        map,
                        monster,
                        target
                    );

                    continue;
                }

                // ==================================
                // CHASE
                // ==================================

                float distance =
                    MathF.Sqrt(
                        distanceSquared
                    );

                if (distance <= 0.001f)
                {
                    continue;
                }

                float directionX =
                    deltaX /
                    distance;

                float directionZ =
                    deltaZ /
                    distance;

                float movementDistance =
                    monster.MovementSpeed *
                    deltaTime;

                float remainingDistance =
                    distance -
                    monster.AttackRange;

                if (movementDistance >
                    remainingDistance)
                {
                    movementDistance =
                        remainingDistance;
                }

                monster.X +=
                    directionX *
                    movementDistance;

                monster.Z +=
                    directionZ *
                    movementDistance;
            }
        }
    }

    private void TryMonsterAttack(
        MapInstance map,
        WorldMonster monster,
        WorldPlayer target)
    {
        if (target.IsDead)
        {
            monster.TargetCharacterId =
                null;

            return;
        }

        if (monster.AttackCooldownRemaining >
            0f)
        {
            return;
        }

        int damage =
            monster.AttackDamage;

        target.CurrentHp =
            Math.Max(
                0,
                target.CurrentHp - damage
            );

        monster.AttackCooldownRemaining =
            monster.AttackCooldown;

        Console.WriteLine(
            $"[Combat] {monster.Name} ({monster.EntityId}) attacked {target.Name} ({target.CharacterId}) | Damage={damage} | HP={target.CurrentHp}/{target.MaxHp}"
        );

        BroadcastPlayerVitals(
            map,
            target
        );

        if (target.IsDead)
        {
            target.MovementInputX =
                0f;

            target.MovementInputZ =
                0f;

            Console.WriteLine(
                $"[Combat] Player died | CharacterId={target.CharacterId} | Name={target.Name}"
            );

            BroadcastPlayerDeath(
                map,
                target
            );

            monster.TargetCharacterId =
                null;
        }
    }

    public bool RespawnPlayer(
        WorldPlayer player)
    {
        if (!player.IsDead)
        {
            return false;
        }

        MapInstance? map =
            GetMap(
                player.MapId
            );

        if (map == null)
        {
            return false;
        }

        player.CurrentHp =
            player.MaxHp;

        player.X =
            player.RespawnX;

        player.Y =
            player.RespawnY;

        player.Z =
            player.RespawnZ;

        player.MovementInputX =
            0f;

        player.MovementInputZ =
            0f;

        map.UpdatePlayerCell(
            player
        );

        RefreshVisibility(
            player
        );

        BroadcastPlayerVitals(
            map,
            player
        );

        Console.WriteLine(
            $"[World] Player respawned | CharacterId={player.CharacterId} | Position=({player.X}, {player.Y}, {player.Z}) | HP={player.CurrentHp}/{player.MaxHp}"
        );

        return true;
    }

    private static WorldPlayer? FindMonsterTarget(
        MapInstance map,
        WorldMonster monster)
    {
        WorldPlayer? closestPlayer =
            null;

        float closestDistanceSquared =
            monster.DetectionRadius *
            monster.DetectionRadius;

        foreach (WorldPlayer player in
                 map.Players)

        {
            if (player.IsDead)
            {
                continue;
            }

            float deltaX =
                player.X -
                monster.X;

            float deltaZ =
                player.Z -
                monster.Z;

            float distanceSquared =
                deltaX * deltaX +
                deltaZ * deltaZ;

            if (distanceSquared >
                closestDistanceSquared)
            {
                continue;
            }

            closestDistanceSquared =
                distanceSquared;

            closestPlayer =
                player;
        }

        return closestPlayer;
    }

    private static void SendPlayerVitals(
        WorldPlayer receiver,
        WorldPlayer target)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerVitalsSnapshot
        );

        writer.Put(
            target.CharacterId
        );

        writer.Put(
            target.CurrentHp
        );

        writer.Put(
            target.MaxHp
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    private static void BroadcastPlayerVitals(
        MapInstance map,
        WorldPlayer target)
    {
        foreach (WorldPlayer receiver in
                 map.Players)
        {
            bool isOwner =
                receiver.CharacterId ==
                target.CharacterId;

            bool canSeeTarget =
                receiver.VisiblePlayers.Contains(
                    target.CharacterId
                );

            if (!isOwner &&
                !canSeeTarget)
            {
                continue;
            }

            SendPlayerVitals(
                receiver,
                target
            );
        }
    }

    public void SendInitialPlayerVitals(
        WorldPlayer player)
    {
        SendPlayerVitals(
            player,
            player
        );
    }

    private static void SendMonsterSnapshot(
        WorldPlayer receiver,
        WorldMonster monster)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.MonsterSnapshot
        );

        writer.Put(
            monster.EntityId
        );

        writer.Put(
            monster.X
        );

        writer.Put(
            monster.Y
        );

        writer.Put(
            monster.Z
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.Sequenced
        );
    }

    private static void SendPlayerDeath(
        WorldPlayer receiver,
        WorldPlayer deadPlayer)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerDeath
        );

        writer.Put(
            deadPlayer.CharacterId
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    private static void BroadcastPlayerDeath(
        MapInstance map,
        WorldPlayer deadPlayer)
    {
        foreach (WorldPlayer receiver in
                 map.Players)
        {
            bool isOwner =
                receiver.CharacterId ==
                deadPlayer.CharacterId;

            bool canSee =
                receiver.VisiblePlayers.Contains(
                    deadPlayer.CharacterId
                );

            if (!isOwner &&
                !canSee)
            {
                continue;
            }

            SendPlayerDeath(
                receiver,
                deadPlayer
            );
        }
    }

    public bool TryPlayerAttackMonster(
        WorldPlayer player,
        long monsterEntityId)
    {
        if (player.IsDead)
        {
            return false;
        }

        if (player.AttackCooldownRemaining > 0f)
        {
            return false;
        }

        MapInstance? map =
            GetMap(player.MapId);

        if (map == null)
        {
            return false;
        }

        if (!map.TryGetMonster(
                monsterEntityId,
                out WorldMonster? monster) ||
            monster == null)
        {
            return false;
        }

        if (monster.CurrentHp <= 0)
        {
            return false;
        }

        float deltaX =
            monster.X - player.X;

        float deltaZ =
            monster.Z - player.Z;

        float distanceSquared =
            deltaX * deltaX +
            deltaZ * deltaZ;

        float attackRangeSquared =
            player.AttackRange *
            player.AttackRange;

        if (distanceSquared >
            attackRangeSquared)
        {
            Console.WriteLine(
                $"[Combat] Attack rejected: out of range | Player={player.CharacterId} | Monster={monster.EntityId}"
            );

            return false;
        }

        int damage =
            player.AttackDamage;

        monster.CurrentHp =
            Math.Max(
                0,
                monster.CurrentHp - damage
            );

        player.AttackCooldownRemaining =
            player.AttackCooldown;

        Console.WriteLine(
            $"[Combat] {player.Name} attacked {monster.Name} ({monster.EntityId}) | Damage={damage} | MonsterHP={monster.CurrentHp}/{monster.MaxHp}"
        );

        BroadcastMonsterVitals(
            map,
            monster
        );

        if (monster.CurrentHp <= 0)
        {
            KillMonster(
                map,
                monster,
                player
            );
        }

        return true;
    }

    private static void SendMonsterVitals(
        WorldPlayer receiver,
        WorldMonster monster)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.MonsterVitalsSnapshot
        );

        writer.Put(
            monster.EntityId
        );

        writer.Put(
            monster.CurrentHp
        );

        writer.Put(
            monster.MaxHp
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    private static void BroadcastMonsterVitals(
        MapInstance map,
        WorldMonster monster)
    {
        foreach (WorldPlayer receiver in
                 map.Players)
        {
            SendMonsterVitals(
                receiver,
                monster
            );
        }
    }

    private static void SendMonsterDeath(
        WorldPlayer receiver,
        WorldMonster monster)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.MonsterDeath
        );

        writer.Put(
            monster.EntityId
        );

        receiver.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    private void KillMonster(
        MapInstance map,
        WorldMonster monster,
        WorldPlayer killer)
    {
        Console.WriteLine(
            $"[Combat] Monster killed | EntityId={monster.EntityId} | Monster={monster.Name} | Killer={killer.Name}"
        );

        AwardMonsterExperience(
            killer,
            monster
        );

        foreach (WorldPlayer receiver in
                 map.Players)
        {
            SendMonsterDeath(
                receiver,
                monster
            );
        }

        map.RemoveMonster(
            monster.EntityId
        );

        _pendingMonsterRespawns.Add(
            new PendingMonsterRespawn
            {
                MapId = monster.MapId,
                MonsterId = monster.MonsterId,
                Name = monster.Name,
                Level = monster.Level,

                X = monster.SpawnX,
                Y = monster.SpawnY,
                Z = monster.SpawnZ,

                MaxHp = monster.MaxHp,

                RemainingSeconds = 5f
            }
        );

        Console.WriteLine(
            $"[Monster] Respawn scheduled | Monster={monster.Name} | Delay=5s"
        );
    }

    private sealed class PendingMonsterRespawn
    {
        public int MapId { get; init; }

        public int MonsterId { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Level { get; init; }

        public float X { get; init; }

        public float Y { get; init; }

        public float Z { get; init; }

        public int MaxHp { get; init; }

        public float RemainingSeconds { get; set; }
    }

    private void ProcessMonsterRespawns(
        float deltaTime)
    {
        for (int i =
                 _pendingMonsterRespawns.Count - 1;
             i >= 0;
             i--)
        {
            PendingMonsterRespawn pending =
                _pendingMonsterRespawns[i];

            pending.RemainingSeconds -=
                deltaTime;

            if (pending.RemainingSeconds >
                0f)
            {
                continue;
            }

            WorldMonster monster =
                SpawnMonster(
                    pending.MapId,
                    pending.MonsterId,
                    pending.Name,
                    pending.Level,
                    pending.X,
                    pending.Y,
                    pending.Z,
                    pending.MaxHp
                );

            MapInstance? map =
                GetMap(
                    pending.MapId
                );

            if (map != null)
            {
                BroadcastMonsterSpawn(
                    map,
                    monster
                );
            }

            _pendingMonsterRespawns.RemoveAt(
                i
            );

            Console.WriteLine(
                $"[Monster] Respawned | EntityId={monster.EntityId} | Monster={monster.Name}"
            );
        }
    }

    private static void BroadcastMonsterSpawn(
        MapInstance map,
        WorldMonster monster)
    {
        foreach (WorldPlayer player in
                 map.Players)
        {
            SendMonsterSpawn(
                player,
                monster
            );
        }
    }

    private void AwardMonsterExperience(
        WorldPlayer player,
        WorldMonster monster)
    {
        int experienceReward =
            Math.Max(
                1,
                monster.Level * 25
            );

        player.Experience +=
            experienceReward;

        Console.WriteLine(
            $"[XP] {player.Name} gained {experienceReward} XP | Level={player.Level} | XP={player.Experience}"
        );

        bool leveledUp =
            false;

        while (true)
        {
            long requiredExperience =
                WorldPlayer.GetExperienceRequiredForLevel(
                    player.Level
                );

            if (player.Experience <
                requiredExperience)
            {
                break;
            }

            player.Experience -=
                requiredExperience;

            player.Level++;

            leveledUp =
                true;

            Console.WriteLine(
                $"[XP] LEVEL UP | Character={player.Name} | NewLevel={player.Level}"
            );
        }

        SendPlayerExperience(
            player
        );

        if (leveledUp)
        {
            SendPlayerLevelUp(
                player
            );
        }

        _characterService.UpdateProgression(
            player.CharacterId,
            player.Level,
            player.Experience
        );
    }

    private static void SendPlayerExperience(
        WorldPlayer player)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerExperienceSnapshot
        );

        writer.Put(
            player.Level
        );

        writer.Put(
            player.Experience
        );

        writer.Put(
            WorldPlayer.GetExperienceRequiredForLevel(
                player.Level
            )
        );

        player.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

    private static void SendPlayerLevelUp(
        WorldPlayer player)
    {
        var writer =
            new NetDataWriter();

        writer.Put(
            (ushort)PacketId.PlayerLevelUp
        );

        writer.Put(
            player.Level
        );

        player.Session.Peer.Send(
            writer,
            DeliveryMethod.ReliableOrdered
        );
    }

public void SendInitialPlayerExperience(
    WorldPlayer player)
{
    SendPlayerExperience(
        player
    );
}
}