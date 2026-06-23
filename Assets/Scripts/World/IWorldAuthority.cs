using System.Collections.Generic;
using UnityEngine;

public interface IWorldAuthority
{
    IWorldSimulation Simulation { get; }

    PlayerInventoryState PlayerInventory { get; }

    ContentCatalog ContentCatalog { get; }

    WorldCommandResult ExecuteCommand(WorldCommand command);

    bool TrySetBlock(Vector3Int position, VoxelBlockType blockType);
    bool TryQueryBlock(Vector3Int position, out BlockQueryResult result);
    bool TryGetBiomeAt(Vector3Int worldPosition, out BiomeDefinition biome, out ClimateSample climate);

    bool HasChiseledBlockAt(Vector3Int position);
    bool TryBeginChiselBlock(Vector3Int blockPosition);
    bool TryChiselRemoveVoxel(Vector3Int blockPosition, Vector3 localPoint);
    bool TryChiselAddVoxel(Vector3Int blockPosition, Vector3 localPoint);

    bool TryGetCampfireState(Vector3Int position, out CampfireState state);
    bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message);
    bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message);
    bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message);
    bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state);
    bool TryHasCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal);
    void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer);

    bool TryPlaceClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksiteKey key, out string message);
    bool TryFindClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksite worksite);
    bool TryStartClayForming(ClayWorksiteKey key, string recipeId, out string message);
    void RemoveClayWorksite(ClayWorksiteKey key);
    ClayFormingEditResult TryClayFormingAdd(ClayWorksiteKey key, int u, int v);
    ClayFormingEditResult TryClayFormingRemove(ClayWorksiteKey key, int u, int v);
    void SetClayFormingToolMode(ClayWorksiteKey key, ClayFormingToolMode toolMode);
    void CopyClayWorksiteSnapshots(List<ClayWorksiteSnapshot> buffer);

    bool TryPlaceGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, HotbarItem item, out string message);
    bool TryProbeGroundPickup(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, int requestedAmount, out HotbarItem probeItem);
    bool TryPickupGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, int requestedAmount, out HotbarItem pickedItem, out string message);
    int ResolveGroundPickupAmount(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, bool shiftHeld);
    void CopyGroundItemSnapshots(List<GroundItemSurfaceSnapshot> buffer);
}
