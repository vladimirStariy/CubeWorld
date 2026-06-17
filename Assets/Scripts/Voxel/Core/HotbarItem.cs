public enum ItemKind : byte
{
    None = 0,
    Block = 1,
    GrassBundle = 2,
    Stick = 3,
    Flint = 4
}

public readonly struct HotbarItem
{
    public ItemKind Kind { get; }
    public VoxelBlockType BlockType { get; }

    public HotbarItem(ItemKind kind, VoxelBlockType blockType = VoxelBlockType.Air)
    {
        Kind = kind;
        BlockType = blockType;
    }

    public bool IsEmpty => Kind == ItemKind.None;

    public bool IsPlaceableBlock =>
        Kind == ItemKind.Block &&
        BlockType != VoxelBlockType.Air &&
        BlockType != VoxelBlockType.Campfire;

    public bool IsAssemblyComponent =>
        Kind is ItemKind.GrassBundle or ItemKind.Stick or ItemKind.Flint;

    public static HotbarItem FromBlock(VoxelBlockType blockType) => new(ItemKind.Block, blockType);

    public static HotbarItem GrassBundle() => new(ItemKind.GrassBundle);

    public static HotbarItem Stick() => new(ItemKind.Stick);

    public static HotbarItem Flint() => new(ItemKind.Flint);

    public string GetDisplayName()
    {
        return Kind switch
        {
            ItemKind.Block => BlockType.ToString(),
            ItemKind.GrassBundle => "Grass",
            ItemKind.Stick => "Stick",
            ItemKind.Flint => "Flint",
            _ => string.Empty
        };
    }
}

public readonly struct CreativeEntry
{
    public readonly HotbarItem Item;
    public readonly string Label;

    public CreativeEntry(HotbarItem item, string label = null)
    {
        Item = item;
        Label = label ?? item.GetDisplayName();
    }
}
