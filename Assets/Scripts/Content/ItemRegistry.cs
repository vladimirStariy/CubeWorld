using System;
using System.Collections.Generic;

public sealed class ItemRegistry
{
    public static ItemRegistry Active { get; set; }

    private readonly Dictionary<ContentId, ItemDefinition> byId = new();
    private readonly Dictionary<(ItemKind kind, VoxelBlockType blockType), ItemDefinition> byRuntime = new();
    private readonly Dictionary<string, ItemDefinition> byAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ItemDefinition> creativeEntries = new();

    public IReadOnlyList<ItemDefinition> CreativeEntries => creativeEntries;

    public void Register(ItemDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        byId[definition.Id] = definition;
        byRuntime[(definition.RuntimeKind, definition.RuntimeBlockType)] = definition;

        foreach (var alias in definition.CommandAliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                byAlias[alias] = definition;
            }
        }

        var nameAlias = definition.Id.Name;
        if (!string.IsNullOrWhiteSpace(nameAlias))
        {
            byAlias[nameAlias] = definition;
        }

        if (definition.ShowInCreative && !creativeEntries.Contains(definition))
        {
            creativeEntries.Add(definition);
        }
    }

    public bool TryGet(ContentId id, out ItemDefinition definition) => byId.TryGetValue(id, out definition);

    public bool TryGet(ItemKind kind, VoxelBlockType blockType, out ItemDefinition definition) =>
        byRuntime.TryGetValue((kind, blockType), out definition);

    public bool TryGet(HotbarItem item, out ItemDefinition definition) =>
        TryGet(item.Kind, item.BlockType, out definition);

    public bool TryGetByAlias(string alias, out ItemDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        return byAlias.TryGetValue(alias.Trim(), out definition);
    }

    public bool HasCapability(HotbarItem item, ItemCapabilities capability)
    {
        if (item.IsEmpty)
        {
            return false;
        }

        return TryGet(item, out var definition) && definition.Capabilities.Has(capability);
    }

    public bool TryGetGroundProfile(ItemKind kind, out GroundItemPlacementProfile profile)
    {
        profile = default;
        if (!TryGet(kind, VoxelBlockType.Air, out var definition) || !definition.GroundProfile.HasValue)
        {
            return false;
        }

        profile = definition.GroundProfile.Value;
        return true;
    }

    public bool IsGroundPlaceable(ItemKind kind) =>
        TryGet(kind, VoxelBlockType.Air, out var definition) && definition.GroundProfile.HasValue;

    public HotbarItem CreateStack(ItemKind kind, int count)
    {
        if (TryGet(kind, VoxelBlockType.Air, out var definition))
        {
            return definition.CreateStack(count);
        }

        return kind switch
        {
            ItemKind.Stick => HotbarItem.Stick(count),
            ItemKind.Flint => HotbarItem.Flint(count),
            ItemKind.Clay => HotbarItem.Clay(count),
            ItemKind.RawClayBowl => HotbarItem.RawClayBowl(count),
            ItemKind.ClayBowl => HotbarItem.ClayBowl(count),
            _ => new HotbarItem(kind, count: count)
        };
    }
}
