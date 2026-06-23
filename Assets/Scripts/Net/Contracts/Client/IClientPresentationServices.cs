using UnityEngine;

/// <summary>
/// Rendering and UI helpers that are client-only but driven by server content.
/// </summary>
public interface IClientPresentationServices
{
    IClientWorldQueries Queries { get; }

    IClientContentView Content { get; }

    ChunkStreamingSettings ChunkStreaming { get; }

    Material BuildBlockMaterial(out Texture2D atlasTexture);
}

public interface IClientContentView
{
    ContentCatalog Catalog { get; }

    Texture2D BlockAtlasTexture { get; }
}

public interface IClientPlayerState
{
    PlayerInventoryState Inventory { get; }

    Vector3 SpawnPosition { get; }
}
