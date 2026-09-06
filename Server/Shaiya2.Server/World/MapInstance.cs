namespace Shaiya2.Server.World;

public sealed class MapInstance
{
    private const float CellSize =
        20f;

    private readonly Dictionary<long, WorldPlayer>
        _players = new();

    private readonly Dictionary<SpatialCell, HashSet<long>>
        _playersByCell = new();

    private readonly Dictionary<long, SpatialCell>
        _playerCells = new();

    public int MapId { get; }

    public IReadOnlyCollection<WorldPlayer> Players =>
        _players.Values;

    private readonly Dictionary<long, WorldMonster>
        _monsters = new();

    public IReadOnlyCollection<WorldMonster> Monsters =>
        _monsters.Values;

    public MapInstance(
        int mapId)
    {
        MapId =
            mapId;
    }

    public bool AddPlayer(
        WorldPlayer player)
    {
        if (!_players.TryAdd(
                player.CharacterId,
                player))
        {
            return false;
        }

        SpatialCell cell =
            GetCell(
                player.X,
                player.Z
            );

        AddToCell(
            player.CharacterId,
            cell
        );

        _playerCells[
            player.CharacterId
        ] =
            cell;

        return true;
    }

    public bool RemovePlayer(
        long characterId)
    {
        if (!_players.Remove(
                characterId))
        {
            return false;
        }

        if (_playerCells.TryGetValue(
                characterId,
                out SpatialCell cell))
        {
            RemoveFromCell(
                characterId,
                cell
            );

            _playerCells.Remove(
                characterId
            );
        }

        return true;
    }

    public bool TryGetPlayer(
        long characterId,
        out WorldPlayer? player)
    {
        return _players.TryGetValue(
            characterId,
            out player
        );
    }

    public void UpdatePlayerCell(
        WorldPlayer player)
    {
        SpatialCell newCell =
            GetCell(
                player.X,
                player.Z
            );

        if (_playerCells.TryGetValue(
                player.CharacterId,
                out SpatialCell oldCell))
        {
            if (oldCell == newCell)
            {
                return;
            }

            RemoveFromCell(
                player.CharacterId,
                oldCell
            );
        }

        AddToCell(
            player.CharacterId,
            newCell
        );

        _playerCells[
            player.CharacterId
        ] =
            newCell;
    }

    public IEnumerable<WorldPlayer> GetNearbyPlayers(
        WorldPlayer source)
    {
        SpatialCell center =
            GetCell(
                source.X,
                source.Z
            );

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                SpatialCell cell =
                    new(
                        center.X + x,
                        center.Z + z
                    );

                if (!_playersByCell.TryGetValue(
                        cell,
                        out HashSet<long>? ids))
                {
                    continue;
                }

                foreach (long characterId in ids)
                {
                    if (_players.TryGetValue(
                            characterId,
                            out WorldPlayer? player))
                    {
                        yield return player;
                    }
                }
            }
        }
    }

    private static SpatialCell GetCell(
        float x,
        float z)
    {
        return new SpatialCell(
            (int)MathF.Floor(
                x / CellSize
            ),
            (int)MathF.Floor(
                z / CellSize
            )
        );
    }

    private void AddToCell(
        long characterId,
        SpatialCell cell)
    {
        if (!_playersByCell.TryGetValue(
                cell,
                out HashSet<long>? players))
        {
            players =
                new HashSet<long>();

            _playersByCell[
                cell
            ] =
                players;
        }

        players.Add(
            characterId
        );
    }

    private void RemoveFromCell(
        long characterId,
        SpatialCell cell)
    {
        if (!_playersByCell.TryGetValue(
                cell,
                out HashSet<long>? players))
        {
            return;
        }

        players.Remove(
            characterId
        );

        if (players.Count == 0)
        {
            _playersByCell.Remove(
                cell
            );
        }
    }

    public bool AddMonster(
        WorldMonster monster)
    {
        return _monsters.TryAdd(
            monster.EntityId,
            monster
        );
    }

    public bool RemoveMonster(
        long entityId)
    {
        return _monsters.Remove(
            entityId
        );
    }

    public bool TryGetMonster(
        long entityId,
        out WorldMonster? monster)
    {
        return _monsters.TryGetValue(
            entityId,
            out monster
        );
    }
}