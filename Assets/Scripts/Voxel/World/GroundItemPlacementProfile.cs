public readonly struct GroundItemPlacementProfile
{
    public GroundPlacementLayout Layout { get; }
    public int MaxStackPerSlot { get; }
    public int ShiftPickupAmount { get; }
    public ContentId StackingShapeId { get; }
    public int CuboidsPerModel { get; }
    public int ItemsPerModel { get; }
    public int TransferQuantity { get; }
    public float CbScaleYByLayer { get; }

    public GroundItemPlacementProfile(
        GroundPlacementLayout layout,
        int maxStackPerSlot,
        int shiftPickupAmount = 1,
        ContentId stackingShapeId = default,
        int cuboidsPerModel = 1,
        int itemsPerModel = 0,
        int transferQuantity = 0,
        float cbScaleYByLayer = 0f)
    {
        Layout = layout;
        MaxStackPerSlot = maxStackPerSlot;
        ShiftPickupAmount = shiftPickupAmount < 1 ? 1 : shiftPickupAmount;
        StackingShapeId = stackingShapeId;
        CuboidsPerModel = cuboidsPerModel < 1 ? 1 : cuboidsPerModel;
        TransferQuantity = transferQuantity < 1 ? 1 : transferQuantity;
        ItemsPerModel = itemsPerModel > 0 ? itemsPerModel : TransferQuantity;
        CbScaleYByLayer = cbScaleYByLayer;
    }

    public bool HasStackingShape => !string.IsNullOrEmpty(StackingShapeId.Name);

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
    public static bool TryGet(ItemKind kind, out GroundItemPlacementProfile profile)
    {
        if (ItemRegistry.Active != null && ItemRegistry.Active.TryGetGroundProfile(kind, out profile))
        {
            return true;
        }

        profile = default;
        return false;
    }

    public static bool IsGroundPlaceable(ItemKind kind)
    {
        return ItemRegistry.Active != null && ItemRegistry.Active.IsGroundPlaceable(kind);
    }
}
