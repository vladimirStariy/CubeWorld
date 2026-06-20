using UnityEngine;

public interface IItemUseContext
{
    bool TryPlaceCampfireFoundation(Vector3Int hitBlock, Vector3 faceNormal, out string message);

    bool TryAddCampfireStick(Vector3Int hitBlock, Vector3 faceNormal, out string message);

    bool TryLightCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message);
}

public interface IItemUseProvider
{
    bool CanUse(HotbarItem item, ItemRegistry items);

    bool TryUse(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        HotbarItem item,
        ItemRegistry items,
        IItemUseContext context,
        out string message);
}
