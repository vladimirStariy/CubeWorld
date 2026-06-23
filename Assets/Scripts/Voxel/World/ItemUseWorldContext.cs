using UnityEngine;

internal sealed class ItemUseWorldContext : IItemUseContext
{
    private readonly WorldSimulation world;

    public ItemUseWorldContext(WorldSimulation world)
    {
        this.world = world;
    }

    public bool TryPlaceCampfireFoundation(Vector3Int hitBlock, Vector3 faceNormal, out string message) =>
        world.TryPlaceCampfireFoundation(hitBlock, faceNormal, out message);

    public bool TryAddCampfireStick(Vector3Int hitBlock, Vector3 faceNormal, out string message) =>
        world.TryAddCampfireStick(hitBlock, faceNormal, out message);

    public bool TryLightCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message) =>
        world.TryLightCampfireAssembly(hitBlock, faceNormal, out message);
}
