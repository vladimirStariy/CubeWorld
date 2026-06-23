using System;

/// <summary>
/// Lowest layer: opaque packets on the wire. Serialization lives above this.
/// </summary>
public interface INetworkPacketTransport
{
    bool IsConnected { get; }

    void Send(GameNetworkMessageKind kind, ReadOnlySpan<byte> payload);

    event Action<GameNetworkMessageKind, ReadOnlyMemory<byte>> PacketReceived;

    event Action Disconnected;
}

public interface INetworkPacketTransportFactory
{
    INetworkPacketTransport CreateClient(string address, int port);

    INetworkPacketTransport CreateServer(int port);
}
