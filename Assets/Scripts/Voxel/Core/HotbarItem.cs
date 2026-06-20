using UnityEngine;

public enum ItemKind : byte
{
    None = 0,
    Block = 1,
    GrassBundle = 2,
    Stick = 3,
    Flint = 4,
    Chisel = 5,
    Clay = 6,
    RawClayBowl = 7,
    ClayBowl = 8
}

public readonly struct HotbarItem
{
    public ItemKind Kind { get; }
    public VoxelBlockType BlockType { get; }
    public int Count { get; }

    public HotbarItem(ItemKind kind, VoxelBlockType blockType = VoxelBlockType.Air, int count = 1)
    {
        Kind = kind;
        BlockType = blockType;
        Count = kind == ItemKind.None ? 0 : Mathf.Max(1, count);
    }

    public bool IsEmpty => Kind == ItemKind.None;

    public bool IsStackable => Kind != ItemKind.None && Kind != ItemKind.Chisel;

    public int MaxStack => IsStackable ? InventoryConstants.MaxStackSize : 1;

    public bool CanStackWith(HotbarItem other)
    {
        return !IsEmpty
               && !other.IsEmpty
               && IsStackable
               && Kind == other.Kind
               && BlockType == other.BlockType;
    }

    public bool IsPlaceableBlock =>
        Kind == ItemKind.Block &&
        BlockType != VoxelBlockType.Air &&
        BlockType != VoxelBlockType.Campfire;

    public bool IsAssemblyComponent =>
        Kind is ItemKind.GrassBundle or ItemKind.Stick or ItemKind.Flint;

    public bool IsGroundPlaceable => GroundItemPlacementProfiles.IsGroundPlaceable(Kind);

    public HotbarItem WithCount(int count)
    {
        return new HotbarItem(Kind, BlockType, count);
    }

    public static HotbarItem FromBlock(VoxelBlockType blockType, int count = 1) => new(ItemKind.Block, blockType, count);

    public static HotbarItem GrassBundle() => new(ItemKind.GrassBundle);

    public static HotbarItem Stick(int count = 1) => new(ItemKind.Stick, count: count);

    public static HotbarItem Flint(int count = 1) => new(ItemKind.Flint, count: count);

    public static HotbarItem Chisel() => new(ItemKind.Chisel);

    public static HotbarItem Clay(int count = 1) => new(ItemKind.Clay, count: count);

    public static HotbarItem RawClayBowl(int count = 1) => new(ItemKind.RawClayBowl, count: count);

    public static HotbarItem ClayBowl(int count = 1) => new(ItemKind.ClayBowl, count: count);

    public bool IsChisel => Kind == ItemKind.Chisel;

    public bool IsClay => Kind == ItemKind.Clay;

    public string GetDisplayName()
    {
        var name = Kind switch
        {
            ItemKind.Block => BlockType.ToString(),
            ItemKind.GrassBundle => "Grass",
            ItemKind.Stick => "Stick",
            ItemKind.Flint => "Flint",
            ItemKind.Chisel => "Chisel",
            ItemKind.Clay => "Clay",
            ItemKind.RawClayBowl => "Raw Clay Bowl",
            ItemKind.ClayBowl => "Clay Bowl",
            _ => string.Empty
        };

        if (Count > 1 && !string.IsNullOrEmpty(name))
        {
            return $"{name} x{Count}";
        }

        return name;
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
