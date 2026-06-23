using UnityEngine;

/// <summary>
/// In-process server connection for singleplayer (integrated server + local client).
/// Exposes simulation for chunk meshing; remote clients will receive chunk snapshots instead.
/// </summary>
public interface ILocalIntegratedServerConnection : IGameServerConnection
{
    IGameClientNetworkSession Network { get; }

    IWorldSimulation Simulation { get; }

    void PrimeSpawnArea(Vector3 spawnPosition);

    Material BuildClientBlockMaterial(out Texture2D atlasTexture);

    Material BuildClientFluidMaterial(out Texture2D atlasTexture);
}
