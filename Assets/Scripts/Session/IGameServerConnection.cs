using UnityEngine;

/// <summary>
/// Client-side link to the authoritative game server.
/// Singleplayer uses <see cref="ILocalIntegratedServerConnection"/> in-process;
/// multiplayer will use a remote transport with the same command surface.
/// </summary>
public interface IGameServerConnection
{
    IWorldAuthority Authority { get; }

    IWorldPresentationQueries Presentation { get; }

    PlayerInventoryState PlayerInventory { get; }

    ContentCatalog ContentCatalog { get; }

    Texture2D BlockAtlasTexture { get; }

    ChunkStreamingSettings ChunkStreaming { get; }

    Vector3 GetSpawnPosition();

    WorldCommandResult ExecuteCommand(WorldCommand command);
}
