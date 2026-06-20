using UnityEngine;

public sealed class ItemDefinition
{
    public ContentId Id { get; }
    public string DisplayName { get; }
    public ItemKind RuntimeKind { get; }
    public VoxelBlockType RuntimeBlockType { get; }
    public ItemCapabilities Capabilities { get; }
    public GroundItemPlacementProfile? GroundProfile { get; }
    public bool ShowInCreative { get; }
    public string[] CommandAliases { get; }

    public ItemDefinition(
        ContentId id,
        string displayName,
        ItemKind runtimeKind,
        ItemCapabilities capabilities,
        VoxelBlockType runtimeBlockType = VoxelBlockType.Air,
        GroundItemPlacementProfile? groundProfile = null,
        bool showInCreative = true,
        params string[] commandAliases)
    {
        Id = id;
        DisplayName = displayName;
        RuntimeKind = runtimeKind;
        RuntimeBlockType = runtimeBlockType;
        Capabilities = capabilities;
        GroundProfile = groundProfile;
        ShowInCreative = showInCreative;
        CommandAliases = commandAliases ?? System.Array.Empty<string>();
    }

    public bool IsStackable => RuntimeKind != ItemKind.None && !Capabilities.Has(ItemCapabilities.ChiselTool);

    public int MaxStack => IsStackable ? InventoryConstants.MaxStackSize : 1;

    public HotbarItem CreateStack(int count = 1)
    {
        if (RuntimeKind == ItemKind.Block)
        {
            return HotbarItem.FromBlock(RuntimeBlockType, count);
        }

        return RuntimeKind switch
        {
            ItemKind.GrassBundle => HotbarItem.GrassBundle(),
            ItemKind.Stick => HotbarItem.Stick(count),
            ItemKind.Flint => HotbarItem.Flint(count),
            ItemKind.Chisel => HotbarItem.Chisel(),
            ItemKind.Clay => HotbarItem.Clay(count),
            ItemKind.RawClayBowl => HotbarItem.RawClayBowl(count),
            ItemKind.ClayBowl => HotbarItem.ClayBowl(count),
            _ => new HotbarItem(RuntimeKind, RuntimeBlockType, count)
        };
    }
}
