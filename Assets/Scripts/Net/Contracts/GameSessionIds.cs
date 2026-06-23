/// <summary>
/// Stable identifiers for multiplayer sessions. Singleplayer uses <see cref="Local"/> only.
/// </summary>
public readonly struct PlayerConnectionId
{
    public int Value { get; }

    public PlayerConnectionId(int value)
    {
        Value = value;
    }

    public static PlayerConnectionId Local => new(0);

    public bool IsValid => Value >= 0;

    public override string ToString() => Value.ToString();
}

public readonly struct GameSessionId
{
    public int Value { get; }

    public GameSessionId(int value)
    {
        Value = value;
    }

    public static GameSessionId Singleplayer => new(0);

    public override string ToString() => Value.ToString();
}
