public readonly struct BlockShapeElement
{
    public BlockShapeElement(string name, BlockOcclusionBox bounds)
    {
        Name = name;
        Bounds = bounds;
    }

    public string Name { get; }
    public BlockOcclusionBox Bounds { get; }
}
