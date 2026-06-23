using UnityEngine;

/// <summary>
/// Process-level entry for the game client (rendering, input, local prediction later).
/// </summary>
public interface IGameClientHost
{
    IGameClientSession Session { get; }

    bool IsConnected { get; }

    void Connect(GameClientConnectOptions options);

    void Disconnect();

    /// <summary>
    /// Per-frame client work: apply replication queue, streaming, input flush.
    /// </summary>
    void Tick(float deltaTime);
}

public sealed class GameClientConnectOptions
{
    public PlayerConnectionId LocalPlayerId = PlayerConnectionId.Local;

    public GameSessionId SessionId = GameSessionId.Singleplayer;

    /// <summary>
    /// Null/empty for integrated in-process session.
    /// </summary>
    public string ServerAddress;

    public int ServerPort;
}

/// <summary>
/// Everything gameplay UI and presentation code should talk to on the client.
/// </summary>
public interface IGameClientSession
{
    PlayerConnectionId LocalPlayerId { get; }

    IWorldCommandSender Commands { get; }

    IWorldReplicationReceiver Replication { get; }

    IClientWorldStore World { get; }

    IClientPresentationServices Presentation { get; }

    IClientPlayerState Player { get; }
}
