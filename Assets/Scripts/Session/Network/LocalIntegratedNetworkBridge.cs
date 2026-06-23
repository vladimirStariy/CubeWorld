using System;
using UnityEngine;

/// <summary>
/// In-process client↔server transport for integrated singleplayer.
/// Remote multiplayer will replace this with a TCP/UDP channel using the same message DTOs.
/// </summary>
public sealed class LocalIntegratedNetworkBridge : IGameClientNetworkSession, IDisposable
{
    private BlockWorldServer server;
    private bool bound;

    public event Action<BlockChangeMessage> BlockChanged;
    public event Action<ChunkCoordMessage> ChunkInvalidated;
    public event Action<ChunkCoordMessage> ChunkUnloaded;
    public event Action<ClayWorksiteChangedMessage> ClayWorksiteChanged;

    public void BindServer(BlockWorldServer worldServer)
    {
        if (bound || worldServer == null)
        {
            return;
        }

        server = worldServer;
        WorldSimulationEvents.BlockChanged += HandleBlockChanged;
        ClayFormingEvents.WorksiteChanged += HandleClayWorksiteChanged;

        var simulation = server.Simulation;
        if (simulation != null)
        {
            simulation.ChunkPresentationChanged += HandleChunkInvalidated;
            simulation.ChunkUnloaded += HandleChunkUnloaded;
        }

        bound = true;
        GameConsoleLog.Info($"[Network] Channel '{GameNetworkConstants.WorldChannel}' bound (local in-process).");
    }

    public WorldCommandResult SendCommandFromClient(WorldCommand command)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("Server is not ready.");
        }

        if (!WorldCommandNetworking.IsHighFrequency(command))
        {
            GameConsoleLog.Info($"[Client→Server] {WorldCommandNetworking.Describe(command)}");
        }

        var result = server.ExecuteCommand(command);
        var resultMessage = WorldCommandNetworking.ToMessage(result);

        if (!string.IsNullOrEmpty(resultMessage.Message))
        {
            var prefix = resultMessage.Success ? "[Server→Client]" : "[Server→Client] FAIL";
            GameConsoleLog.Info($"{prefix} {resultMessage.Message}");
        }

        if (result.HasClayEditResult && result.ClayEditResult.RecipeCompleted)
        {
            GameConsoleLog.Info("[Server→Client] Clay recipe completed.");
        }

        return result;
    }

    public void Dispose()
    {
        if (!bound)
        {
            return;
        }

        WorldSimulationEvents.BlockChanged -= HandleBlockChanged;
        ClayFormingEvents.WorksiteChanged -= HandleClayWorksiteChanged;

        if (server?.Simulation != null)
        {
            server.Simulation.ChunkPresentationChanged -= HandleChunkInvalidated;
            server.Simulation.ChunkUnloaded -= HandleChunkUnloaded;
        }

        bound = false;
        server = null;
    }

    private void HandleBlockChanged(BlockChangeEvent change)
    {
        var message = BlockChangeMessage.From(change);
        GameConsoleLog.Info(
            $"[Server→Client] BlockChange ({message.X}, {message.Y}, {message.Z}): " +
            $"{(VoxelBlockType)message.OldType} → {(VoxelBlockType)message.NewType}");
        BlockChanged?.Invoke(message);
    }

    private void HandleClayWorksiteChanged(ClayWorksiteKey key)
    {
        ClayWorksiteChanged?.Invoke(ClayWorksiteChangedMessage.From(key));
    }

    private void HandleChunkInvalidated(Vector3Int chunkCoord)
    {
        var message = ChunkCoordMessage.From(chunkCoord);
        ChunkInvalidated?.Invoke(message);
    }

    private void HandleChunkUnloaded(Vector3Int chunkCoord)
    {
        var message = ChunkCoordMessage.From(chunkCoord);
        GameConsoleLog.Info($"[Server→Client] ChunkUnload ({message.X}, {message.Y}, {message.Z})");
        ChunkUnloaded?.Invoke(message);
    }
}
