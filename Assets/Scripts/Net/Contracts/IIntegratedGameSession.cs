/// <summary>
/// Factory for singleplayer: server + client in one process, sharing no mutable shortcuts in public API.
/// </summary>
public interface IIntegratedGameSessionFactory
{
    IntegratedGameSessionPair CreateInProcess(IntegratedGameSessionOptions options);
}

public sealed class IntegratedGameSessionOptions
{
    public GameServerStartOptions Server = new();

    public GameClientConnectOptions Client = new();
}

public readonly struct IntegratedGameSessionPair
{
    public IGameServerHost Server { get; }

    public IGameClientHost Client { get; }

    public IntegratedGameSessionPair(IGameServerHost server, IGameClientHost client)
    {
        Server = server;
        Client = client;
    }
}
