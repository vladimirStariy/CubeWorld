using UnityEngine;

public sealed class CampfireItemUseProvider : IItemUseProvider
{
    public bool CanUse(HotbarItem item, ItemRegistry items) =>
        items.TryGet(item, out var definition) && definition.Capabilities.IsAssemblyComponent();

    public bool TryUse(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        HotbarItem item,
        ItemRegistry items,
        IItemUseContext context,
        out string message)
    {
        message = null;
        if (items == null || !items.TryGet(item, out var definition))
        {
            return false;
        }

        if (definition.Capabilities.Has(ItemCapabilities.CampfireFoundation))
        {
            return context.TryPlaceCampfireFoundation(hitBlock, faceNormal, out message);
        }

        if (definition.Capabilities.Has(ItemCapabilities.CampfireAssemblyStick))
        {
            return context.TryAddCampfireStick(hitBlock, faceNormal, out message);
        }

        if (definition.Capabilities.Has(ItemCapabilities.CampfireIgniter))
        {
            return context.TryLightCampfireAssembly(hitBlock, faceNormal, out message);
        }

        return false;
    }
}
