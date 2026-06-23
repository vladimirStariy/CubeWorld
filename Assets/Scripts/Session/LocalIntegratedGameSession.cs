using UnityEngine;

/// <summary>
/// Singleplayer session: integrated authoritative server with a local client.
/// Same split as Minecraft / Vintage Story — server owns simulation, client owns input and rendering.
/// </summary>
public sealed class LocalIntegratedGameSession
{
    public BlockWorldServer Server { get; }

    public LocalIntegratedNetworkBridge Network { get; }

    public ILocalIntegratedServerConnection Connection { get; }

    public LocalIntegratedGameSession(BlockWorldServer server)
    {
        Server = server;
        Network = new LocalIntegratedNetworkBridge();
        Network.BindServer(server);
        Connection = new LocalIntegratedServerConnection(server, Network);
    }

    public static LocalIntegratedGameSession Create(Transform parent)
    {
        var server = VoxelGameObjectFactory.CreateWorldServer(parent);
        return new LocalIntegratedGameSession(server);
    }

    public void StartWorld()
    {
        Server.ConfigureContent();
    }

    public Vector3 GetSpawnPosition() => Connection.GetSpawnPosition();

    public void PrimeSpawnArea(Vector3 spawnPosition) => Connection.PrimeSpawnArea(spawnPosition);
}
