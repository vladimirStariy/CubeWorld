using System;

/// <summary>
/// Client-side view of server→client replication (Vintage Story network channel on the client).
/// </summary>
public interface IGameClientNetworkSession
{
    event Action<BlockChangeMessage> BlockChanged;

    event Action<ChunkCoordMessage> ChunkInvalidated;

    event Action<ChunkCoordMessage> ChunkUnloaded;

    event Action<ClayWorksiteChangedMessage> ClayWorksiteChanged;
}
