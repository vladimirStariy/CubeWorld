public static class ClayFormingConstants
{
    public const int GridSize = 14;
    public const int InitialPadSize = 8;
    public const float VoxelSize = 1f / GridSize;

    public static int InitialPadOffset => (GridSize - InitialPadSize) / 2;
}
