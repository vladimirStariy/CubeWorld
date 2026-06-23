using UnityEngine;

public sealed class LocalIntegratedServerConnection : ILocalIntegratedServerConnection
{
    private readonly BlockWorldServer server;
    private readonly LocalIntegratedNetworkBridge network;

    public LocalIntegratedServerConnection(BlockWorldServer server, LocalIntegratedNetworkBridge networkBridge)
    {
        this.server = server;
        network = networkBridge;
    }

    public IWorldAuthority Authority => server;

    public IWorldPresentationQueries Presentation => server;

    public IGameClientNetworkSession Network => network;

    public IWorldSimulation Simulation => server.Simulation;

    public PlayerInventoryState PlayerInventory => server.PlayerInventory;

    public ContentCatalog ContentCatalog => server.ContentCatalog;

    public Texture2D BlockAtlasTexture => server.BlockAtlasTexture;

    public ChunkStreamingSettings ChunkStreaming => server.ChunkStreaming;

    public Vector3 GetSpawnPosition() => server.GetSpawnPosition();

    public WorldCommandResult ExecuteCommand(WorldCommand command) => network.SendCommandFromClient(command);

    public void PrimeSpawnArea(Vector3 spawnPosition) => server.PrimeSimulation(spawnPosition);

    public Material BuildClientBlockMaterial(out Texture2D atlasTexture) =>
        server.BuildClientBlockMaterial(out atlasTexture);

    public Material BuildClientFluidMaterial(out Texture2D atlasTexture) =>
        server.BuildClientFluidMaterial(out atlasTexture);
}
