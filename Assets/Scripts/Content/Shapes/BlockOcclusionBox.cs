public readonly struct BlockOcclusionBox
{
    public BlockOcclusionBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    public int MinX { get; }
    public int MinY { get; }
    public int MinZ { get; }
    public int MaxX { get; }
    public int MaxY { get; }
    public int MaxZ { get; }
}
