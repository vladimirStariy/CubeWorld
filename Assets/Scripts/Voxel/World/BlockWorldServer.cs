using System.Collections.Generic;
using UnityEngine;

public sealed class BlockWorldServer : MonoBehaviour, IWorldAuthority, IWorldPresentationQueries
{
    [Header("World")]
    [SerializeField] private int worldHeight = 16;
    [SerializeField] private int baseLayerY = 0;
    [SerializeField] private int chunkSize = 16;

    [Header("Streaming")]
    [SerializeField] private ChunkStreamingSettings chunkStreaming = new();

    [Header("Visuals")]
    [SerializeField] private int chiselResolution = 16;

    private WorldSimulation simulation;
    private readonly ClayFormingStorage clayForming = new();
    private readonly PlayerInventoryState playerInventory = new();
    private readonly ContentCatalog contentCatalog = new();
    private GroundItemStorage groundItems;
    private bool contentConfigured;
    private WorldSettings worldSettings;

    public IWorldSimulation Simulation => simulation;
    public PlayerInventoryState PlayerInventory => playerInventory;
    public ContentCatalog ContentCatalog => contentCatalog;
    public WorldSettings WorldSettings => worldSettings;
    public ChunkStreamingSettings ChunkStreaming => chunkStreaming;

    public Texture2D BlockAtlasTexture { get; private set; }

    public int WorldHeight => simulation.WorldHeight;

    private void Awake()
    {
        chiselResolution = 16;
        worldSettings = WorldGenerationLoader.LoadMergedSettings();
        WorldSettings.Active = worldSettings;

        worldHeight = worldSettings.Height;
        baseLayerY = worldSettings.BaseLayerY;

        simulation = new WorldSimulation(
            worldHeight,
            baseLayerY,
            chunkSize,
            chiselResolution);

        groundItems = new GroundItemStorage(simulation.IsBlockOccupied);
    }

    public Vector3 GetSpawnPosition()
    {
        return worldSettings != null
            ? worldSettings.GetSpawnPosition()
            : new Vector3(0.5f, baseLayerY + 2f, 0.5f);
    }

    private void Update()
    {
        simulation.TickFunctionalBlocks(Time.deltaTime);
    }

    public bool TrySetBlock(Vector3Int position, VoxelBlockType blockType)
    {
        var changed = simulation.TrySetBlock(position, blockType);
        if (changed && blockType == VoxelBlockType.Air)
        {
            groundItems.RemoveSurfacesOnBlock(position);
        }

        return changed;
    }

    public bool TryQueryBlock(Vector3Int position, out BlockQueryResult result)
    {
        return simulation.TryQueryBlock(position, out result);
    }

    public bool TryGetBiomeAt(Vector3Int worldPosition, out BiomeDefinition biome, out ClimateSample climate)
    {
        return simulation.TryGetBiomeAt(worldPosition, out biome, out climate);
    }

    public bool TryGetOutlineSegments(Vector3Int blockPosition, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetOutlineSegments(simulation, blockPosition, segments);
    }

    public bool TryGetHitFaceOutline(Vector3Int blockPosition, Vector3 faceNormal, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetHitFaceOutline(simulation, blockPosition, faceNormal, segments);
    }

    public bool TryBuildStickStackOutline(Vector3Int hitBlock, Vector3 faceNormal, List<LineSegment> segments)
    {
        return groundItems.TryBuildStickStackOutline(hitBlock, faceNormal, segments);
    }

    public bool TryBuildStickStackOutline(GroundItemSurfaceKey key, List<LineSegment> segments)
    {
        return groundItems.TryBuildStickStackOutline(key, segments);
    }

    public bool TryGetStickStackCount(Vector3Int hitBlock, Vector3 faceNormal, out int stickCount)
    {
        return groundItems.TryGetStickStackCount(hitBlock, faceNormal, out stickCount);
    }

    public bool TryGetStickStackCount(GroundItemSurfaceKey key, out int stickCount)
    {
        return groundItems.TryGetStickStackCount(key, out stickCount);
    }

    public bool HasChiseledBlockAt(Vector3Int position) => simulation.HasChiseledBlock(position);

    public bool TryBeginChiselBlock(Vector3Int blockPosition) => simulation.TryBeginChiselBlock(blockPosition);

    public bool TryChiselRemoveVoxel(Vector3Int blockPosition, Vector3 localPoint) =>
        simulation.TryChiselRemoveVoxel(blockPosition, localPoint);

    public bool TryChiselAddVoxel(Vector3Int blockPosition, Vector3 localPoint) =>
        simulation.TryChiselAddVoxel(blockPosition, localPoint);

    public bool TryGetCampfireState(Vector3Int position, out CampfireState state) =>
        simulation.TryGetCampfireState(position, out state);

    public bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message) =>
        simulation.TryUseItemOnTarget(hitBlock, faceNormal, item, out message);

    public bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message) =>
        simulation.TryBreakCampfireAssembly(hitBlock, faceNormal, out message);

    public bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state) =>
        simulation.TryGetCampfireAssemblyState(clickedBlock, faceNormal, out state);

    public void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer) =>
        simulation.CopyCampfireAssemblySnapshots(buffer);

    public bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message) =>
        simulation.TryInteractCampfire(position, interaction, out state, out message);

    public void CopyClayWorksiteSnapshots(List<ClayWorksiteSnapshot> buffer) => clayForming.CopySnapshots(buffer);

    public bool TryPlaceClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksiteKey key, out string message)
    {
        key = default;
        if (!simulation.IsBlockOccupied(anchorBlock))
        {
            message = "Clay needs a solid surface.";
            return false;
        }

        return clayForming.TryPlaceWorksite(anchorBlock, faceNormal, out key, out message);
    }

    public bool TryFindClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksite worksite) =>
        clayForming.TryFindWorksite(anchorBlock, faceNormal, out worksite);

    public bool TryStartClayForming(ClayWorksiteKey key, string recipeId, out string message) =>
        clayForming.TryStartForming(key, recipeId, out message);

    public void RemoveClayWorksite(ClayWorksiteKey key) => clayForming.RemoveWorksite(key);

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

    public bool TryPlaceGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, HotbarItem item, out string message) =>
        groundItems.TryPlaceItem(hitBlock, faceNormal, worldHitPoint, item, out message);

    public bool TryProbeGroundPickup(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem probeItem) =>
        groundItems.TryProbeGroundPickup(hitBlock, faceNormal, worldHitPoint, requestedAmount, out probeItem);

    public bool TryPickupGroundItem(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem pickedItem,
        out string message) =>
        groundItems.TryPickupItem(hitBlock, faceNormal, worldHitPoint, requestedAmount, out pickedItem, out message);

    public bool TryHasCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal) =>
        simulation.TryGetCampfireAssemblyState(hitBlock, faceNormal, out _);

    public int ResolveGroundPickupAmount(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        bool shiftHeld) =>
        groundItems.ResolvePickupAmount(hitBlock, faceNormal, worldHitPoint, shiftHeld);

    public void CopyGroundItemSnapshots(List<GroundItemSurfaceSnapshot> buffer) => groundItems.CopySnapshots(buffer);

    public void SetClayFormingToolMode(ClayWorksiteKey key, ClayFormingToolMode toolMode) => clayForming.SetToolMode(key, toolMode);

    public WorldCommandResult ExecuteCommand(WorldCommand command) => WorldCommandExecutor.Execute(command, this);

    public void ConfigureContent()
    {
        if (contentConfigured)
        {
            return;
        }

        VanillaContentBootstrap.RegisterAll(contentCatalog);
        WorldGenerationLoader.LoadBiomesFromPacks(contentCatalog.Biomes);
        if (contentCatalog.Biomes.Biomes.Count == 0)
        {
            WorldGenerationLoader.RegisterFallbackBiomes(contentCatalog.Biomes);
        }

        WorldGenerationLoader.RegisterBuiltInGenerators(contentCatalog.WorldGenerators, worldSettings);
        contentCatalog.WorldSettings = worldSettings;
        simulation.SetItemUseRegistry(contentCatalog.ItemUse);
        clayForming.SetRecipeRegistry(contentCatalog.ClayRecipes);
        InitializeSimulation();
        contentConfigured = true;
    }

    public Material BuildClientBlockMaterial(out Texture2D atlasTexture)
    {
        var material = BlockWorldMaterialSetup.CreateBlockMaterial(contentCatalog.BlockTextures, out var atlas);
        BlockAtlasTexture = atlas;
        atlasTexture = atlas;
        return material;
    }

    public void PrimeSimulation(Vector3 spawnPosition)
    {
        simulation.ConfigureStreaming(chunkStreaming);
        simulation.PrimeSpawnArea(spawnPosition);
    }

    private void InitializeSimulation()
    {
        simulation.InitializeWorldGeneration(
            contentCatalog.WorldGenerators,
            worldSettings,
            contentCatalog.Items,
            contentCatalog.Biomes);
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
}
