using UnityEngine;

/// <summary>
/// Authoritative world simulation surface. Only the server mutates state through here.
/// </summary>
public interface IGameServerWorld
{
    int WorldHeight { get; }

    int MinWorldY { get; }

    int ChunkSize { get; }

    Vector3 GetSpawnPosition(PlayerConnectionId connectionId);

    /// <summary>
    /// Preloads/generates chunks around a position before a player spawns.
    /// </summary>
    void PrimeAreaAround(Vector3 worldPosition);

    WorldCommandResult ExecuteCommand(PlayerConnectionId connectionId, WorldCommand command);

    bool TryBuildChunkSnapshot(Vector3Int chunkCoord, out ChunkBlocksSnapshotMessage snapshot);
}
