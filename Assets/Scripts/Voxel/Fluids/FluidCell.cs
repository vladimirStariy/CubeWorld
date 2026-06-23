public struct FluidCell
{
    public FluidType Type;
    public byte Level;
    public bool IsSource;

    public bool IsEmpty => Type == FluidType.None || Level == 0;

    public static FluidCell Empty => default;

    public static FluidCell Source(FluidType type, byte level = FluidConstants.MaxLevel)
    {
        return new FluidCell
        {
            Type = type,
            Level = level,
            IsSource = true
        };
    }

    public float GetFillHeight()
    {
        if (IsEmpty)
        {
            return 0f;
        }

        return Level / (float)FluidConstants.MaxLevel;
    }
}

public static class FluidConstants
{
    public const byte MaxLevel = 7;
}
