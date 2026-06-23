using System;

/// <summary>
/// Typed game channel built on top of <see cref="INetworkPacketTransport"/>.
/// One channel per logical world replication stream.
/// </summary>
public interface IClientToServerChannel
{
    /// <summary>
    /// Sends player intent to the authoritative server and waits for the result.
    /// Remote implementations may defer the result to a later <see cref="IServerToClientChannel.CommandResultReceived"/> event.
    /// </summary>
    WorldCommandResult SendCommand(WorldCommand command);
}

public interface IServerToClientChannel
{
    event Action<WorldCommandResult> CommandResultReceived;

    event Action<BlockChangeMessage> BlockChanged;

    event Action<ChunkCoordMessage> ChunkInvalidated;

    event Action<ChunkCoordMessage> ChunkUnloaded;

    event Action<ChunkBlocksSnapshotMessage> ChunkSnapshotReceived;

    event Action<ClayWorksiteChangedMessage> ClayWorksiteChanged;
}

public interface IServerToClientsChannel
{
    void SendCommandResult(PlayerConnectionId connectionId, WorldCommandResult result);

    void BroadcastBlockChange(BlockChangeMessage message);

    void BroadcastChunkInvalidate(ChunkCoordMessage message);

    void BroadcastChunkUnload(ChunkCoordMessage message);

    void SendChunkSnapshot(PlayerConnectionId connectionId, ChunkBlocksSnapshotMessage snapshot);

    void BroadcastClayWorksiteChanged(ClayWorksiteChangedMessage message);
}
