using System.Collections.Generic;
using UnityEngine;

public sealed class BlockWorldServer : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private int worldWidth = 100;
    [SerializeField] private int worldDepth = 100;
    [SerializeField] private int worldHeight = 16;
    [SerializeField] private int baseLayerY = 0;
    [SerializeField] private int chunkSize = 16;

    [Header("Visuals")]
    [SerializeField] private Texture2D dirtTexture;
    [SerializeField] private Texture2D grassTexture;
    [SerializeField] private Texture2D grassSideTexture;
    [SerializeField] private Texture2D blockAtlasTexture;
    [SerializeField] private int chiselResolution = 16;

    private VoxelWorldStorage world;
    private Transform chunksRoot;
    private readonly ClayFormingStorage clayForming = new();
    private GroundItemStorage groundItems;

    public Texture2D BlockAtlasTexture { get; private set; }

    public int WorldWidth => world.WorldWidth;
    public int WorldDepth => world.WorldDepth;
    public int WorldHeight => world.WorldHeight;

    private void Awake()
    {
        chiselResolution = 16;

        chunksRoot = new GameObject("Chunks").transform;
        chunksRoot.SetParent(transform, false);

        world = new VoxelWorldStorage(
            chunksRoot,
            worldWidth,
            worldDepth,
            worldHeight,
            baseLayerY,
            chunkSize,
            chiselResolution);

        var material = BlockWorldMaterialSetup.CreateBlockMaterial(
            dirtTexture,
            grassTexture,
            grassSideTexture,
            blockAtlasTexture,
            out var atlas);
        BlockAtlasTexture = atlas;
        world.SetChunkMaterial(material);

        world.GenerateFlatWorld();
        world.RebuildAllChunks();
        groundItems = new GroundItemStorage(world.IsBlockOccupied);
    }

    private void Update()
    {
        world.TickFunctionalBlocks(Time.deltaTime);
    }

    public bool TrySetBlock(Vector3Int position, VoxelBlockType blockType)
    {
        var changed = world.TrySetBlock(position, blockType);
        if (changed && blockType == VoxelBlockType.Air)
        {
            groundItems.RemoveSurfacesOnBlock(position);
        }

        return changed;
    }

    public bool IsInWorld(Vector3Int position)
    {
        return world.IsInWorld(position);
    }

    public bool HasSolidBlockAt(Vector3Int position)
    {
        return world.IsBlockOccupied(position);
    }

    public bool TryQueryBlock(Vector3Int position, out BlockQueryResult result)
    {
        return world.TryQueryBlock(position, out result);
    }

    public bool TryGetOutlineSegments(Vector3Int blockPosition, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetOutlineSegments(world, blockPosition, segments);
    }

    public bool TryGetHitFaceOutline(Vector3Int blockPosition, Vector3 faceNormal, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetHitFaceOutline(world, blockPosition, faceNormal, segments);
    }

    public bool HasChiseledBlockAt(Vector3Int position)
    {
        return world.HasChiseledBlock(position);
    }

    public bool TryBeginChiselBlock(Vector3Int blockPosition)
    {
        return world.TryBeginChiselBlock(blockPosition);
    }

    public bool TryChiselRemoveVoxel(Vector3Int blockPosition, Vector3 localPoint)
    {
        return world.TryChiselRemoveVoxel(blockPosition, localPoint);
    }

    public bool TryChiselAddVoxel(Vector3Int blockPosition, Vector3 localPoint)
    {
        return world.TryChiselAddVoxel(blockPosition, localPoint);
    }

    public bool TryGetCampfireState(Vector3Int position, out CampfireState state)
    {
        return world.TryGetCampfireState(position, out state);
    }

    public bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message)
    {
        return world.TryUseItemOnTarget(hitBlock, faceNormal, item, out message);
    }

    public bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        return world.TryBreakCampfireAssembly(hitBlock, faceNormal, out message);
    }

    public bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state)
    {
        return world.TryGetCampfireAssemblyState(clickedBlock, faceNormal, out state);
    }

    public void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer)
    {
        world.CopyCampfireAssemblySnapshots(buffer);
    }

    public bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message)
    {
        return world.TryInteractCampfire(position, interaction, out state, out message);
    }

    public void CopyClayWorksiteSnapshots(List<ClayWorksiteSnapshot> buffer)
    {
        clayForming.CopySnapshots(buffer);
    }

    public bool TryPlaceClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksiteKey key, out string message)
    {
        key = default;
        if (!world.IsBlockOccupied(anchorBlock))
        {
            message = "Clay needs a solid surface.";
            return false;
        }

        return clayForming.TryPlaceWorksite(anchorBlock, faceNormal, out key, out message);
    }

    public bool TryFindClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksite worksite)
    {
        return clayForming.TryFindWorksite(anchorBlock, faceNormal, out worksite);
    }

    public bool TryStartClayForming(ClayWorksiteKey key, string recipeId, out string message)
    {
        return clayForming.TryStartForming(key, recipeId, out message);
    }

    public void RemoveClayWorksite(ClayWorksiteKey key)
    {
        clayForming.RemoveWorksite(key);
    }

    public ClayFormingEditResult TryClayFormingAdd(ClayWorksiteKey key, int u, int v)
    {
        var result = clayForming.TryAddClay(key, u, v);
        FinalizeClayRecipePlacement(result);
        return result;
    }

    public ClayFormingEditResult TryClayFormingRemove(ClayWorksiteKey key, int u, int v)
    {
        var result = clayForming.TryRemoveClay(key, u, v);
        FinalizeClayRecipePlacement(result);
        return result;
    }

    public bool TryPlaceGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, HotbarItem item, out string message)
    {
        return groundItems.TryPlaceItem(hitBlock, faceNormal, worldHitPoint, item, out message);
    }

    public bool TryProbeGroundPickup(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem probeItem)
    {
        return groundItems.TryProbeGroundPickup(hitBlock, faceNormal, worldHitPoint, requestedAmount, out probeItem);
    }

    public bool TryPickupGroundItem(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem pickedItem,
        out string message)
    {
        return groundItems.TryPickupItem(hitBlock, faceNormal, worldHitPoint, requestedAmount, out pickedItem, out message);
    }

    public bool TryHasCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal)
    {
        return world.TryGetCampfireAssemblyState(hitBlock, faceNormal, out _);
    }

    public int ResolveGroundPickupAmount(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        bool shiftHeld)
    {
        return groundItems.ResolvePickupAmount(hitBlock, faceNormal, worldHitPoint, shiftHeld);
    }

    public void CopyGroundItemSnapshots(List<GroundItemSurfaceSnapshot> buffer)
    {
        groundItems.CopySnapshots(buffer);
    }

    private void FinalizeClayRecipePlacement(ClayFormingEditResult result)
    {
        if (!result.RecipeCompleted || !result.HasCompletionWorksiteKey)
        {
            return;
        }

        var surfaceKey = GroundItemSurfaceKey.FromClayWorksite(result.CompletionWorksiteKey);
        groundItems.TryPlaceCompletedItem(surfaceKey, result.OutputItem, out _);
    }

    public void SetClayFormingToolMode(ClayWorksiteKey key, ClayFormingToolMode toolMode)
    {
        clayForming.SetToolMode(key, toolMode);
    }
}
