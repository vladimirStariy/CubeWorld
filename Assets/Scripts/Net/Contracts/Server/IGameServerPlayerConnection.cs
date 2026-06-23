/// <summary>
/// Server-side view of one connected client.
/// </summary>
public interface IGameServerPlayerConnection
{
    PlayerConnectionId ConnectionId { get; }

    IClientToServerChannel Inbound { get; }

    IServerToClientChannel Outbound { get; }
}

public interface IGameServerConnectionListener
{
    void OnPlayerConnected(IGameServerPlayerConnection connection);

    void OnPlayerDisconnected(PlayerConnectionId connectionId);
}
