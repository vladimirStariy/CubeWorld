using UnityEngine;

/// <summary>
/// Process-level entry for an authoritative game server (integrated or dedicated).
/// </summary>
public interface IGameServerHost
{
    IGameServerSession Session { get; }

    bool IsRunning { get; }

    void Start(GameServerStartOptions options);

    void Stop();

    /// <summary>
    /// Simulation tick: functional blocks, chunk generation queues, etc.
    /// </summary>
    void Tick(float deltaTime);
}

public sealed class GameServerStartOptions
{
    public GameSessionId SessionId = GameSessionId.Singleplayer;

    public WorldSettings WorldSettings;

    public ChunkStreamingSettings ChunkStreaming = new();
}

/// <summary>
/// One running world instance on the server.
/// </summary>
public interface IGameServerSession
{
    GameSessionId SessionId { get; }

    IGameServerWorld World { get; }

    IGameServerReplication Replication { get; }

    IGameServerContent Content { get; }

    void RegisterPlayer(PlayerConnectionId connectionId);

    void UnregisterPlayer(PlayerConnectionId connectionId);
}
