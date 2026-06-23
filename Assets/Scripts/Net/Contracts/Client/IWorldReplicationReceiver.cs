using System;

/// <summary>
/// Server → client facts. Feeds <see cref="IClientWorldStore"/>.
/// </summary>
public interface IWorldReplicationReceiver
{
    event Action<BlockChangeMessage> BlockChanged;

    event Action<ChunkCoordMessage> ChunkInvalidated;

    event Action<ChunkCoordMessage> ChunkUnloaded;

    event Action<ChunkBlocksSnapshotMessage> ChunkSnapshotReceived;

    event Action<ClayWorksiteChangedMessage> ClayWorksiteChanged;

    event Action<WorldCommandResult> CommandResultReceived;
}

public interface IWorldReplicationApplier
{
    void ApplyBlockChange(BlockChangeMessage message);

    void ApplyChunkSnapshot(ChunkBlocksSnapshotMessage snapshot);

    void InvalidateChunk(ChunkCoordMessage message);

    void UnloadChunk(ChunkCoordMessage message);
}
