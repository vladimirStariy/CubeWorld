using UnityEngine;

public readonly struct ItemDisplayTransform
{
    public static readonly ItemDisplayTransform BlockGuiDefault = new(
        Vector3.zero,
        new Vector3(BlockItemGuiTransform.GuiRotationX, BlockItemGuiTransform.GuiRotationY, 0f),
        Vector3.zero,
        BlockItemGuiTransform.GuiScale);

    public ItemDisplayTransform(Vector3 translation, Vector3 rotation, Vector3 origin, float scale)
    {
        Translation = translation;
        Rotation = rotation;
        Origin = origin;
        Scale = scale <= 0f ? 1f : scale;
    }

    public Vector3 Translation { get; }
    public Vector3 Rotation { get; }
    public Vector3 Origin { get; }
    public float Scale { get; }

    public void ApplyToWrapper(Transform wrapper, Transform originPivot, Transform meshRoot, float spinDegrees)
    {
        wrapper.localPosition = Translation;

        var baseRotation = Quaternion.Euler(Rotation);
        if (!Mathf.Approximately(spinDegrees, 0f))
        {
            baseRotation *= Quaternion.AngleAxis(spinDegrees, Vector3.up);
        }

        wrapper.localRotation = baseRotation;

        originPivot.localPosition = Origin;
        originPivot.localRotation = Quaternion.identity;

        meshRoot.localPosition = Vector3.zero;
        meshRoot.localRotation = Quaternion.identity;
        meshRoot.localScale = Vector3.one * Scale;
    }
}

public static class ItemDisplayTransforms
{
    public static ItemDisplayTransform ResolveGui(HotbarItem item)
    {
        if (item.Kind == ItemKind.Block)
        {
            return ItemDisplayTransform.BlockGuiDefault;
        }

        if (ItemRegistry.Active != null
            && ItemRegistry.Active.TryGet(item, out var definition)
            && definition.GuiTransform.HasValue)
        {
            return definition.GuiTransform.Value;
        }

        return ItemDisplayTransform.BlockGuiDefault;
    }

    public static bool TryResolveHand(HotbarItem item, out ItemDisplayTransform transform)
    {
        transform = default;
        if (item.IsEmpty || item.Kind == ItemKind.Block)
        {
            return false;
        }

        if (ItemRegistry.Active != null
            && ItemRegistry.Active.TryGet(item, out var definition)
            && definition.FpHandTransform.HasValue)
        {
            transform = definition.FpHandTransform.Value;
            return true;
        }

        return false;
    }
}
