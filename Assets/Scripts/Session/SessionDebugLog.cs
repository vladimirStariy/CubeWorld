using UnityEngine;

public static class SessionDebugLog
{
    public static void WriteIntegratedSessionReady(LocalIntegratedGameSession session, BlockWorldClient client)
    {
        if (session == null)
        {
            return;
        }

        var simulation = session.Connection?.Simulation;
        var spawn = session.GetSpawnPosition();

        GameConsoleLog.Info("[Server] Local integrated server started.");
        if (simulation != null)
        {
            GameConsoleLog.Info(
                $"[Server] World height={simulation.WorldHeight} (XZ infinite), " +
                $"chunk={simulation.ChunkSize}, loaded={simulation.LoadedChunkCount}.");
        }

        GameConsoleLog.Info($"[Server] Spawn {FormatVector(spawn)}.");

        if (client != null)
        {
            GameConsoleLog.Info("[Client] Connected to local server (in-process).");
            GameConsoleLog.Info($"[Client] Network channel '{GameNetworkConstants.WorldChannel}' ready.");
            GameConsoleLog.Info("[Client] Chunk presenter, input, and UI active.");
            GameConsoleLog.Info("[Client] Chunk streaming is driven by the client.");
            GameConsoleLog.Info("[Client] Commands route Client→Server via network bridge.");
        }
        else
        {
            GameConsoleLog.Info("[Client] BlockWorldClient is not configured.");
        }
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.#}, {value.y:0.#}, {value.z:0.#})";
    }
}
