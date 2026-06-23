using UnityEngine;

/// <summary>
/// Pushes authoritative state deltas to connected clients.
/// </summary>
public interface IGameServerReplication
{
    void BindTransport(IServerToClientsChannel channel);

    void NotifyBlockChange(BlockChangeMessage message);

    void NotifyChunkInvalidate(ChunkCoordMessage message);

    void NotifyChunkUnload(ChunkCoordMessage message);

    void NotifyClayWorksiteChanged(ClayWorksiteChangedMessage message);

    void SendChunkSnapshot(PlayerConnectionId connectionId, ChunkBlocksSnapshotMessage snapshot);

    void SendCommandResult(PlayerConnectionId connectionId, WorldCommandResult result);
}

public interface IGameServerContent
{
    ContentCatalog Catalog { get; }

    bool TryGetBlockAtlas(out Texture2D atlasTexture);
}
