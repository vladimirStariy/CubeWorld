using System.Collections.Generic;

public readonly struct GroundItemPlacementProfile
{
    public GroundPlacementLayout Layout { get; }
    public int MaxStackPerSlot { get; }
    public int ShiftPickupAmount { get; }

    public GroundItemPlacementProfile(GroundPlacementLayout layout, int maxStackPerSlot, int shiftPickupAmount = 1)
    {
        Layout = layout;
        MaxStackPerSlot = maxStackPerSlot;
        ShiftPickupAmount = shiftPickupAmount < 1 ? 1 : shiftPickupAmount;
    }

    public int SlotCount => Layout switch
    {
        GroundPlacementLayout.Dual => 2,
        GroundPlacementLayout.Quad => 4,
        _ => 1
    };

    public bool RequiresTopFace => Layout is GroundPlacementLayout.Dual
        or GroundPlacementLayout.Quad
        or GroundPlacementLayout.Pile
        or GroundPlacementLayout.Stack;
}

public static class GroundItemPlacementProfiles
{
    private static readonly Dictionary<ItemKind, GroundItemPlacementProfile> Profiles = new()
    {
        [ItemKind.Stick] = new(GroundPlacementLayout.Stack, StickStackLayout.Capacity, StickStackLayout.ShiftPickupAmount),
        [ItemKind.Flint] = new(GroundPlacementLayout.Pile, 64),
        [ItemKind.Clay] = new(GroundPlacementLayout.Pile, 64),
        [ItemKind.RawClayBowl] = new(GroundPlacementLayout.Dual, 1),
        [ItemKind.ClayBowl] = new(GroundPlacementLayout.Single, 1)
    };

    public static bool TryGet(ItemKind kind, out GroundItemPlacementProfile profile)
    {
        return Profiles.TryGetValue(kind, out profile);
    }

    public static bool IsGroundPlaceable(ItemKind kind)
    {
        return Profiles.ContainsKey(kind);
    }
}
