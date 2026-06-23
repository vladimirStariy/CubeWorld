using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public sealed class WorldSimulation : IWorldSimulation
{
    public event System.Action<Vector3Int> ChunkPresentationChanged;
    public event System.Action<Vector3Int> ChunkUnloaded;

    private readonly Dictionary<Vector3Int, ChunkBlockData> chunks = new();
    private readonly Dictionary<Vector3Int, VoxelBlockType[]> unloadedChunkCache = new();
    private readonly Dictionary<Vector3Int, FluidCell[]> unloadedFluidCache = new();
    private readonly HashSet<Vector3Int> dirtyChunks = new();
    private readonly HashSet<Vector3Int> presentationDirtySet = new();
    private readonly HashSet<Vector3Int> modifiedChunkCoords = new();
    private readonly HashSet<Vector3Int> generatedChunks = new();
    private readonly Queue<Vector3Int> pendingGenerations = new();
    private readonly HashSet<Vector3Int> pendingGenerationSet = new();
    private readonly Queue<Vector3Int> pendingStreamLoads = new();
    private readonly HashSet<Vector3Int> pendingStreamLoadSet = new();
    private readonly List<Vector3Int> chunkUnloadBuffer = new();
    private readonly List<Vector3Int> chunkCoordSortBuffer = new();
    private readonly BackgroundChunkGenerator backgroundGenerator = new();
    private readonly FluidSpreadSimulator fluidSpreadSimulator = new();
    private readonly Dictionary<long, int> terrainSurfaceYCache = new();
    private Vector3 lastStreamingPlayerWorldPosition;
    private readonly Dictionary<Vector3Int, ChiseledBlockData> chiseledBlocks = new();
    private readonly Dictionary<Vector3Int, CampfireBlockEntity> campfires = new();
    private readonly Dictionary<Vector3Int, CampfireAssembly> campfireAssemblies = new();
    private ItemUseRegistry itemUseRegistry;
    private IChunkWorldGenerator chunkGenerator;
    private WorldSettings worldSettings;
    private ItemRegistry itemRegistry;
    private BiomeRegistry biomeRegistry;
    private ChunkStreamingSettings streamingSettings;
    private Vector3Int lastStreamingCenter = new(int.MinValue, 0, int.MinValue);
    private int lastMinChunkY = int.MinValue;
    private int lastMaxChunkY = int.MinValue;
    private readonly int worldHeight;
    private readonly int baseLayerY;

    public int ChunkSize { get; }
    public int ChiselResolution { get; }
    public int MaxChunkY { get; }
    public int LoadedChunkCount => chunks.Count;
    public bool HasPendingStreaming =>
        pendingStreamLoads.Count > 0
        || pendingGenerations.Count > 0
        || backgroundGenerator.InFlightCount > 0
        || backgroundGenerator.CompletedCount > 0;
    public int WorldHeight => worldHeight;
    public int MinWorldY => baseLayerY;

    public WorldSimulation(
        int worldHeight,
        int baseLayerY,
        int chunkSize,
        int chiselResolution)
    {
        this.worldHeight = worldHeight;
        this.baseLayerY = baseLayerY;
        ChunkSize = chunkSize;
        ChiselResolution = chiselResolution;
        MaxChunkY = Mathf.Max(0, (worldHeight - 1) / chunkSize);
    }

    public void ConfigureStreaming(ChunkStreamingSettings settings)
    {
        streamingSettings = settings;
    }

    public void UpdateChunkStreaming(Vector3 playerWorldPosition)
    {
        if (streamingSettings == null)
        {
            return;
        }

        lastStreamingPlayerWorldPosition = playerWorldPosition;
        var worldPosition = Vector3Int.FloorToInt(playerWorldPosition);
        var center = WorldToChunk(worldPosition);
        var horizontalCenter = new Vector3Int(center.x, 0, center.z);
        GetActiveVerticalChunkRange(playerWorldPosition.y, out var minChunkY, out var maxChunkY);

        if (horizontalCenter == lastStreamingCenter &&
            minChunkY == lastMinChunkY &&
            maxChunkY == lastMaxChunkY)
        {
            return;
        }

        lastStreamingCenter = horizontalCenter;
        lastMinChunkY = minChunkY;
        lastMaxChunkY = maxChunkY;
        var viewDistance = streamingSettings.ViewDistanceChunks;
        var unloadDistance = streamingSettings.UnloadDistanceChunks;

        for (int dz = -viewDistance; dz <= viewDistance; dz++)
        {
            for (int dx = -viewDistance; dx <= viewDistance; dx++)
            {
                if (HorizontalChunkDistance(horizontalCenter, new Vector3Int(horizontalCenter.x + dx, 0, horizontalCenter.z + dz)) > viewDistance)
                {
                    continue;
                }

                for (int cy = minChunkY; cy <= maxChunkY; cy++)
                {
                    EnqueueStreamLoad(new Vector3Int(horizontalCenter.x + dx, cy, horizontalCenter.z + dz));
                }
            }
        }

        chunkUnloadBuffer.Clear();
        foreach (var pair in chunks)
        {
            var coord = pair.Key;
            if (HorizontalChunkDistance(horizontalCenter, new Vector3Int(coord.x, 0, coord.z)) > unloadDistance ||
                coord.y < minChunkY ||
                coord.y > maxChunkY)
            {
                chunkUnloadBuffer.Add(coord);
            }
        }

        for (int i = 0; i < chunkUnloadBuffer.Count; i++)
        {
            UnloadChunk(chunkUnloadBuffer[i]);
        }
    }

    public void ProcessChunkLoadRequests(int maxRequests)
    {
        var horizontalCenter = GetHorizontalChunkCenter(lastStreamingPlayerWorldPosition);

        for (int i = 0; i < maxRequests && pendingStreamLoadSet.Count > 0; i++)
        {
            if (!TryDequeueNearestStreamLoad(horizontalCenter, out var chunkCoord))
            {
                break;
            }

            RequestLoadChunk(chunkCoord);
        }
    }

    public void ProcessGenerationBudget(Vector3 playerWorldPosition, float maxMilliseconds)
    {
        if (streamingSettings == null)
        {
            return;
        }

        var applyStopwatch = System.Diagnostics.Stopwatch.StartNew();
        ApplyCompletedGenerations(16);
        RuntimeFrameProfiler.Record("stream.generate.apply", applyStopwatch.Elapsed.TotalMilliseconds);

        var horizontalCenter = GetHorizontalChunkCenter(playerWorldPosition);
        var syncRadius = streamingSettings.SyncGenerationRadiusChunks;
        FlushCriticalGenerations(horizontalCenter, syncRadius, maxPerFrame: 8);

        if (maxMilliseconds <= 0f)
        {
            FlushPresentationDirtyBatch();
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int maxInFlight = 3;

        var maxStarts = streamingSettings.MaxGenerationsPerFrame;
        var started = 0;
        while (pendingGenerationSet.Count > 0
               && started < maxStarts
               && backgroundGenerator.InFlightCount < maxInFlight
               && stopwatch.Elapsed.TotalMilliseconds < maxMilliseconds)
        {
            if (!TrySelectNearestPendingGeneration(horizontalCenter, out var chunkCoord))
            {
                break;
            }

            if (!chunks.TryGetValue(chunkCoord, out var chunk))
            {
                continue;
            }

            if (generatedChunks.Contains(chunkCoord))
            {
                continue;
            }

            if (backgroundGenerator.IsInFlight(chunkCoord))
            {
                continue;
            }

            var horizontalDistance = HorizontalChunkDistance(
                horizontalCenter,
                new Vector3Int(chunkCoord.x, 0, chunkCoord.z));

            if (horizontalDistance <= syncRadius)
            {
                GenerateChunkDataSync(chunk);
                QueueChunkPresentationChanged(chunk.Coord);
                started++;
                continue;
            }

            if (!TryScheduleBackgroundGeneration(chunkCoord))
            {
                GenerateChunkDataSync(chunk);
                QueueChunkPresentationChanged(chunk.Coord);
            }

            started++;
        }

        FlushPresentationDirtyBatch();
    }

    public void ProcessSimulationQueues()
    {
        if (streamingSettings == null)
        {
            return;
        }

        ProcessSimulationQueues(
            streamingSettings.MaxGenerationsPerFrame,
            streamingSettings.MaxMeshBuildsPerFrame);
    }

    public void ProcessSimulationQueues(int maxGenerations, int _)
    {
        ApplyCompletedGenerations(maxGenerations);

        for (int i = 0; i < maxGenerations && pendingGenerations.Count > 0; i++)
        {
            var chunkCoord = pendingGenerations.Dequeue();
            pendingGenerationSet.Remove(chunkCoord);
            if (!chunks.TryGetValue(chunkCoord, out var chunk))
            {
                continue;
            }

            if (generatedChunks.Contains(chunkCoord))
            {
                continue;
            }

            GenerateChunkDataSync(chunk);
            QueueChunkPresentationChanged(chunk.Coord);
        }

        FlushPresentationDirtyBatch();
    }

    public bool TryGetChunkBlocks(Vector3Int chunkCoord, out ChunkBlockData chunkBlocks)
    {
        return chunks.TryGetValue(chunkCoord, out chunkBlocks);
    }

    private void NotifyChunkPresentationChanged(Vector3Int chunkCoord)
    {
        QueueChunkPresentationChanged(chunkCoord);
        FlushPresentationDirtyBatch();
    }

    private void QueueChunkPresentationChanged(Vector3Int chunkCoord)
    {
        if (chunks.ContainsKey(chunkCoord) && generatedChunks.Contains(chunkCoord))
        {
            presentationDirtySet.Add(chunkCoord);
        }

        EnqueueAdjacentPresentationUpdates(chunkCoord);
    }

    private void FlushPresentationDirtyBatch()
    {
        if (presentationDirtySet.Count == 0)
        {
            return;
        }

        foreach (var chunkCoord in presentationDirtySet)
        {
            ChunkPresentationChanged?.Invoke(chunkCoord);
        }

        presentationDirtySet.Clear();
    }

    private void EnqueueAdjacentPresentationUpdates(Vector3Int chunkCoord)
    {
        NotifyPresentationDirty(chunkCoord + Vector3Int.left);
        NotifyPresentationDirty(chunkCoord + Vector3Int.right);
        NotifyPresentationDirty(chunkCoord + Vector3Int.down);
        NotifyPresentationDirty(chunkCoord + Vector3Int.up);
        NotifyPresentationDirty(chunkCoord + new Vector3Int(0, 0, -1));
        NotifyPresentationDirty(chunkCoord + new Vector3Int(0, 0, 1));
    }

    private void NotifyPresentationDirty(Vector3Int chunkCoord)
    {
        if (!chunks.ContainsKey(chunkCoord) || !generatedChunks.Contains(chunkCoord))
        {
            return;
        }

        presentationDirtySet.Add(chunkCoord);
    }

    private void GetActiveVerticalChunkRange(float playerWorldY, out int minChunkY, out int maxChunkY)
    {
        if (MaxChunkY <= 7)
        {
            minChunkY = 0;
            maxChunkY = MaxChunkY;
            return;
        }

        var playerChunkY = FloorDiv(Mathf.FloorToInt(playerWorldY), ChunkSize);
        var below = streamingSettings != null ? streamingSettings.VerticalViewDistanceBelowChunks : 4;
        var above = streamingSettings != null ? streamingSettings.VerticalViewDistanceAboveChunks : 3;
        minChunkY = Mathf.Max(0, playerChunkY - below);
        maxChunkY = Mathf.Min(MaxChunkY, playerChunkY + above);
    }

    public void InitializeWorldGeneration(
        WorldGeneratorRegistry registry,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes)
    {
        worldSettings = settings;
        itemRegistry = items;
        biomeRegistry = biomes;
        generatedChunks.Clear();

        chunkGenerator = null;
        if (registry != null && settings != null)
        {
            if (!registry.TryGet(settings.GeneratorId, out chunkGenerator))
            {
                registry.TryGetDefault(out chunkGenerator);
            }
        }

        pendingGenerations.Clear();
        pendingGenerationSet.Clear();
        pendingStreamLoads.Clear();
        pendingStreamLoadSet.Clear();
        backgroundGenerator.Clear();
        unloadedFluidCache.Clear();
        lastStreamingCenter = new Vector3Int(int.MinValue, 0, int.MinValue);
        lastMinChunkY = int.MinValue;
        lastMaxChunkY = int.MinValue;
    }

    public void PrimeSpawnArea(Vector3 spawnPosition)
    {
        fluidSpreadSimulator.ClearQueues();
        terrainSurfaceYCache.Clear();
        lastStreamingCenter = new Vector3Int(int.MinValue, 0, int.MinValue);
        lastMinChunkY = int.MinValue;
        lastMaxChunkY = int.MinValue;
        UpdateChunkStreaming(spawnPosition);
        PrimeCriticalSpawnChunks(spawnPosition, syncRadiusChunks: 2);
    }

    public bool TryGetBiomeAt(Vector3Int worldPosition, out BiomeDefinition biome, out ClimateSample climate)
    {
        biome = null;
        climate = default;

        if (worldSettings == null || biomeRegistry == null || !IsInWorld(worldPosition))
        {
            return false;
        }

        climate = LatitudeClimateModel.Sample(worldPosition.x, worldPosition.z, worldSettings);
        biome = biomeRegistry.Resolve(climate);
        return biome != null;
    }

    private void PrimeCriticalSpawnChunks(Vector3 spawnPosition, int syncRadiusChunks)
    {
        var worldPos = Vector3Int.FloorToInt(spawnPosition);
        var centerChunk = WorldToChunk(worldPos);
        GetActiveVerticalChunkRange(spawnPosition.y, out var minChunkY, out var maxChunkY);

        for (int dz = -syncRadiusChunks; dz <= syncRadiusChunks; dz++)
        {
            for (int dx = -syncRadiusChunks; dx <= syncRadiusChunks; dx++)
            {
                for (int cy = minChunkY; cy <= maxChunkY; cy++)
                {
                    var chunkCoord = new Vector3Int(centerChunk.x + dx, cy, centerChunk.z + dz);
                    if (!IsChunkCoordInWorld(chunkCoord))
                    {
                        continue;
                    }

                    RequestLoadChunk(chunkCoord);
                    FlushChunkUntilGenerated(chunkCoord);
                }
            }
        }
    }

    public void RebuildAllChunkPresentation()
    {
        foreach (var pair in chunks)
        {
            QueueChunkPresentationChanged(pair.Key);
        }

        dirtyChunks.Clear();
        FlushPresentationDirtyBatch();
    }

    public bool TrySetBlock(Vector3Int position, VoxelBlockType blockType)
    {
        if (!IsInWorld(position))
        {
            return false;
        }

        var hadChiseledBlock = chiseledBlocks.ContainsKey(position);
        chiseledBlocks.Remove(position);

        var chunkCoord = WorldToChunk(position);
        var local = WorldToLocal(position);
        if (!EnsureChunkLoaded(chunkCoord))
        {
            return false;
        }

        var chunk = chunks[chunkCoord];
        var oldType = chunk.GetBlock(local);

        if (oldType == blockType && !hadChiseledBlock)
        {
            return false;
        }

        if (oldType != blockType)
        {
            chunk.SetBlock(local, blockType);
            if (blockType != VoxelBlockType.Air)
            {
                chunk.SetFluid(local, FluidCell.Empty);
            }

            modifiedChunkCoords.Add(chunkCoord);
            UpdateFunctionalBlockEntities(position, oldType, blockType);
            RemoveCampfireAssembliesAffectedByBlockChange(position, oldType, blockType);
            WorldSimulationEvents.RaiseBlockChanged(position, oldType, blockType);
            WakeFluidSpreadAround(position);
            InvalidateTerrainSurfaceCache(position.x, position.z);
        }

        MarkChunkDirty(chunkCoord);
        MarkNeighborChunksDirtyIfBorder(local, chunkCoord);
        FlushPresentationDirty();
        return true;
    }

    public bool TryGetCampfireState(Vector3Int position, out CampfireState state)
    {
        if (campfires.TryGetValue(position, out var campfire))
        {
            state = campfire.Snapshot();
            return true;
        }

        state = default;
        return false;
    }

    public bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state)
    {
        if (TryResolveCampfireAssembly(clickedBlock, faceNormal, out _, out var assembly))
        {
            state = assembly.Snapshot();
            return true;
        }

        state = default;
        return false;
    }

    public void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer)
    {
        buffer.Clear();
        foreach (var pair in campfireAssemblies)
        {
            buffer.Add(new CampfireAssemblySnapshot(pair.Key, pair.Value.Snapshot()));
        }
    }

    public void SetItemUseRegistry(ItemUseRegistry registry)
    {
        itemUseRegistry = registry;
    }

    public bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message)
    {
        var context = new ItemUseWorldContext(this);
        var items = ItemRegistry.Active;
        if (itemUseRegistry != null
            && items != null
            && itemUseRegistry.TryUse(hitBlock, faceNormal, item, context, items, out message))
        {
            return true;
        }

        message = "Cannot use this item here.";
        return false;
    }

    public bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out var anchor, out _))
        {
            message = "No campfire assembly here.";
            return false;
        }

        campfireAssemblies.Remove(anchor);
        message = "Removed campfire assembly.";
        return true;
    }

    public bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message)
    {
        if (!campfires.TryGetValue(position, out var campfire))
        {
            message = "No campfire at target.";
            state = default;
            return false;
        }

        var changed = campfire.TryInteract(interaction, out message);
        state = campfire.Snapshot();
        return changed;
    }

    public void TickFunctionalBlocks(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (campfires.Count > 0)
        {
            foreach (var campfire in campfires.Values)
            {
                campfire.Tick(deltaTime);
            }
        }

        var fluidStopwatch = Stopwatch.StartNew();
        fluidSpreadSimulator.Tick(this, deltaTime);
        RuntimeFrameProfiler.Record("sim.fluids", fluidStopwatch.Elapsed.TotalMilliseconds);
        if (dirtyChunks.Count > 0)
        {
            FlushPresentationDirty();
        }
    }

    public FluidSimulationDiagnostics GetFluidSimulationDiagnostics()
    {
        return fluidSpreadSimulator.GetDiagnostics();
    }

    public bool TryBeginChiselBlock(Vector3Int blockPosition)
    {
        if (!IsInWorld(blockPosition) || chiseledBlocks.ContainsKey(blockPosition))
        {
            return false;
        }

        if (GetBlock(blockPosition) == VoxelBlockType.Campfire || !IsBlockOccupied(blockPosition))
        {
            return false;
        }

        var blockType = GetBlock(blockPosition);
        var chiseled = new ChiseledBlockData(ChiselResolution, blockPosition, blockType);
        chiseled.FillSolid();
        chiseledBlocks[blockPosition] = chiseled;
        SetBlockFast(blockPosition, VoxelBlockType.Air);

        MarkChunkDirty(WorldToChunk(blockPosition));
        MarkNeighborChunksDirtyIfBorder(WorldToLocal(blockPosition), WorldToChunk(blockPosition));
        FlushPresentationDirty();
        return true;
    }

    public bool TryChiselRemoveVoxel(Vector3Int blockPosition, Vector3 localPoint)
    {
        return TryChiselSetCell(blockPosition, localPoint, solid: false);
    }

    public bool TryChiselAddVoxel(Vector3Int blockPosition, Vector3 localPoint)
    {
        return TryChiselSetCell(blockPosition, localPoint, solid: true);
    }

    private bool TryChiselSetCell(Vector3Int blockPosition, Vector3 localPoint, bool solid)
    {
        if (!IsInWorld(blockPosition))
        {
            return false;
        }

        if (GetBlock(blockPosition) == VoxelBlockType.Campfire)
        {
            return false;
        }

        if (!chiseledBlocks.TryGetValue(blockPosition, out var chiseled))
        {
            return false;
        }

        var cell = ChiseledBlockData.LocalPointToCell(localPoint, ChiselResolution);
        if (!chiseled.SetCell(cell.x, cell.y, cell.z, solid))
        {
            return false;
        }

        if (!chiseled.HasAnySolid())
        {
            chiseledBlocks.Remove(blockPosition);
        }

        MarkChunkDirty(WorldToChunk(blockPosition));
        MarkNeighborChunksDirtyIfBorder(WorldToLocal(blockPosition), WorldToChunk(blockPosition));
        FlushPresentationDirty();
        return true;
    }

    public bool TryQueryBlock(Vector3Int position, out BlockQueryResult result)
    {
        result = default;
        if (!IsInWorld(position))
        {
            return false;
        }

        if (chiseledBlocks.TryGetValue(position, out var chiseled))
        {
            var solidCells = chiseled.CountSolidCells();
            if (solidCells == 0)
            {
                return false;
            }

            result = new BlockQueryResult(chiseled.BlockType, true, solidCells, chiseled.Resolution);
            return true;
        }

        var chunkType = GetBlock(position);
        if (chunkType == VoxelBlockType.Air)
        {
            return false;
        }

        result = new BlockQueryResult(chunkType, false, 0, 0);
        return true;
    }

    public bool IsInWorld(Vector3Int position)
    {
        return position.y >= baseLayerY && position.y < worldHeight;
    }

    public bool IsBlockOccupied(Vector3Int position)
    {
        if (chiseledBlocks.TryGetValue(position, out var chiseled))
        {
            return chiseled.HasAnySolid();
        }

        return GetBlock(position) != VoxelBlockType.Air;
    }

    public VoxelBlockType GetBlock(Vector3Int position)
    {
        if (!IsInWorld(position))
        {
            return VoxelBlockType.Air;
        }

        var chunkCoord = WorldToChunk(position);
        if (!chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return VoxelBlockType.Air;
        }

        return chunk.GetBlock(WorldToLocal(position));
    }

    public FluidCell GetFluid(Vector3Int position)
    {
        if (!IsInWorld(position))
        {
            return FluidCell.Empty;
        }

        var chunkCoord = WorldToChunk(position);
        if (!chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return FluidCell.Empty;
        }

        return chunk.GetFluid(WorldToLocal(position));
    }

    public bool IsFullCubeBlock(Vector3Int position)
    {
        if (chiseledBlocks.ContainsKey(position))
        {
            return false;
        }

        return VoxelBlockShapes.IsFullCube(GetBlock(position));
    }

    public bool HasChiseledBlock(Vector3Int position)
    {
        return chiseledBlocks.ContainsKey(position);
    }

    public bool TryGetChiseledBlock(Vector3Int position, out ChiseledBlockData block)
    {
        return chiseledBlocks.TryGetValue(position, out block);
    }

    public bool IsFaceOccludedByNeighbor(Vector3Int neighborPosition, int currentFaceIndex)
    {
        if (!IsInWorld(neighborPosition))
        {
            return true;
        }

        if (!IsChunkLoadedAt(neighborPosition))
        {
            return true;
        }

        var neighborType = GetBlock(neighborPosition);
        if (neighborType != VoxelBlockType.Air)
        {
            return BlockShapeOcclusion.IsNeighborOccludingFace(neighborType, currentFaceIndex);
        }

        if (!chiseledBlocks.TryGetValue(neighborPosition, out var chiseled))
        {
            return false;
        }

        var neighborFace = VoxelConstants.OppositeFace[currentFaceIndex];
        return chiseled.IsSideFullySolid(neighborFace);
    }

    public bool IsMicroFaceOccludedByNeighbor(Vector3Int neighborBlockPos, int currentFace, int x, int y, int z, int resolution)
    {
        if (!IsInWorld(neighborBlockPos))
        {
            return true;
        }

        if (!IsChunkLoadedAt(neighborBlockPos))
        {
            return true;
        }

        var neighborType = GetBlock(neighborBlockPos);
        if (neighborType != VoxelBlockType.Air)
        {
            if (!BlockShapeOcclusion.IsNeighborOccludingFace(neighborType, currentFace))
            {
                return false;
            }

            return true;
        }

        if (!chiseledBlocks.TryGetValue(neighborBlockPos, out var chiseledNeighbor))
        {
            return false;
        }

        return currentFace switch
        {
            0 => chiseledNeighbor.GetCell(0, y, z),
            1 => chiseledNeighbor.GetCell(resolution - 1, y, z),
            2 => chiseledNeighbor.GetCell(x, 0, z),
            3 => chiseledNeighbor.GetCell(x, resolution - 1, z),
            4 => chiseledNeighbor.GetCell(x, y, 0),
            5 => chiseledNeighbor.GetCell(x, y, resolution - 1),
            _ => false
        };
    }

    public Vector3Int LocalToWorld(Vector3Int chunkCoord, Vector3Int local)
    {
        return new Vector3Int(
            chunkCoord.x * ChunkSize + local.x,
            chunkCoord.y * ChunkSize + local.y,
            chunkCoord.z * ChunkSize + local.z);
    }

    private void SetBlockFast(Vector3Int position, VoxelBlockType blockType)
    {
        var chunkCoord = WorldToChunk(position);
        if (!EnsureChunkLoaded(chunkCoord))
        {
            return;
        }

        var local = WorldToLocal(position);
        chunks[chunkCoord].SetBlock(local, blockType);
        modifiedChunkCoords.Add(chunkCoord);
        MarkChunkDirty(chunkCoord);
    }

    private bool EnsureChunkLoaded(Vector3Int chunkCoord)
    {
        if (chunks.ContainsKey(chunkCoord))
        {
            return true;
        }

        if (!IsChunkCoordInWorld(chunkCoord))
        {
            return false;
        }

        RequestLoadChunk(chunkCoord);
        FlushChunkUntilGenerated(chunkCoord);
        return chunks.ContainsKey(chunkCoord);
    }

    private bool IsChunkGenerated(Vector3Int chunkCoord)
    {
        return chunks.ContainsKey(chunkCoord) && generatedChunks.Contains(chunkCoord);
    }

    private void FlushChunkUntilGenerated(Vector3Int chunkCoord)
    {
        for (int i = 0; i < 128 && !IsChunkGenerated(chunkCoord); i++)
        {
            ApplyCompletedGenerations(16);
            if (IsChunkGenerated(chunkCoord))
            {
                break;
            }

            if (backgroundGenerator.IsInFlight(chunkCoord))
            {
                continue;
            }

            if (!chunks.TryGetValue(chunkCoord, out var chunk))
            {
                break;
            }

            RemovePendingGeneration(chunkCoord);
            GenerateChunkDataSync(chunk);
            QueueChunkPresentationChanged(chunk.Coord);
            break;
        }

        FlushPresentationDirtyBatch();
    }

    private void RequestLoadChunk(Vector3Int chunkCoord)
    {
        if (chunks.ContainsKey(chunkCoord) || !IsChunkCoordInWorld(chunkCoord))
        {
            return;
        }

        var chunk = new ChunkBlockData(ChunkSize, chunkCoord);
        chunks.Add(chunkCoord, chunk);

        if (unloadedChunkCache.TryGetValue(chunkCoord, out var cachedBlocks))
        {
            chunk.CopyBlocksFrom(cachedBlocks);
            if (unloadedFluidCache.TryGetValue(chunkCoord, out var cachedFluids))
            {
                chunk.CopyFluidsFrom(cachedFluids);
            }

            unloadedChunkCache.Remove(chunkCoord);
            unloadedFluidCache.Remove(chunkCoord);
            generatedChunks.Add(chunkCoord);
            WakeFluidsAfterGeneration(chunkCoord);
            NotifyChunkPresentationChanged(chunkCoord);
            return;
        }

        EnqueueGeneration(chunkCoord);
    }

    private void UnloadChunk(Vector3Int chunkCoord)
    {
        if (!chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return;
        }

        if (modifiedChunkCoords.Contains(chunkCoord))
        {
            unloadedChunkCache[chunkCoord] = chunk.CopyBlocksToArray();
            unloadedFluidCache[chunkCoord] = chunk.CopyFluidsToArray();
        }

        chunks.Remove(chunkCoord);
        generatedChunks.Remove(chunkCoord);
        dirtyChunks.Remove(chunkCoord);
        ChunkUnloaded?.Invoke(chunkCoord);
    }

    private void EnqueueStreamLoad(Vector3Int chunkCoord)
    {
        if (!IsChunkCoordInWorld(chunkCoord) || chunks.ContainsKey(chunkCoord) || pendingStreamLoadSet.Contains(chunkCoord))
        {
            return;
        }

        pendingStreamLoadSet.Add(chunkCoord);
        pendingStreamLoads.Enqueue(chunkCoord);
    }

    private void EnqueueGeneration(Vector3Int chunkCoord)
    {
        if (pendingGenerationSet.Add(chunkCoord))
        {
            pendingGenerations.Enqueue(chunkCoord);
        }
    }

    private bool IsChunkLoadedAt(Vector3Int worldPosition)
    {
        var chunkCoord = WorldToChunk(worldPosition);
        return chunks.ContainsKey(chunkCoord) && generatedChunks.Contains(chunkCoord);
    }

    private void GenerateChunkDataSync(ChunkBlockData chunk)
    {
        if (chunkGenerator == null || worldSettings == null || biomeRegistry == null)
        {
            generatedChunks.Add(chunk.Coord);
            return;
        }

        generatedChunks.Add(chunk.Coord);
        var context = new VoxelChunkGenerationContext(
            this,
            chunk,
            worldSettings,
            itemRegistry,
            biomeRegistry);
        chunkGenerator.GenerateChunk(chunk.Coord, context);
        WakeFluidsAfterGeneration(chunk.Coord);
    }

    private bool TryScheduleBackgroundGeneration(Vector3Int chunkCoord)
    {
        if (chunkGenerator == null || worldSettings == null || biomeRegistry == null)
        {
            generatedChunks.Add(chunkCoord);
            return false;
        }

        return backgroundGenerator.TryStart(
            chunkCoord,
            ChunkSize,
            chunkGenerator,
            worldSettings,
            itemRegistry,
            biomeRegistry);
    }

    private void ApplyCompletedGenerations(int maxApply)
    {
        for (int i = 0; i < maxApply && backgroundGenerator.TryDequeueCompleted(out var result); i++)
        {
            if (!chunks.TryGetValue(result.Coord, out var chunk))
            {
                continue;
            }

            if (generatedChunks.Contains(result.Coord))
            {
                continue;
            }

            chunk.CopyBlocksFrom(result.Blocks);
            if (result.Fluids != null)
            {
                chunk.CopyFluidsFrom(result.Fluids);
            }

            generatedChunks.Add(result.Coord);
            WakeFluidsAfterGeneration(result.Coord);
            QueueChunkPresentationChanged(result.Coord);
        }
    }

    private void RemovePendingGeneration(Vector3Int chunkCoord)
    {
        if (!pendingGenerationSet.Remove(chunkCoord))
        {
            return;
        }

        var buffer = new Queue<Vector3Int>(pendingGenerations.Count);
        while (pendingGenerations.Count > 0)
        {
            var pending = pendingGenerations.Dequeue();
            if (pending != chunkCoord)
            {
                buffer.Enqueue(pending);
            }
        }

        while (buffer.Count > 0)
        {
            pendingGenerations.Enqueue(buffer.Dequeue());
        }
    }

    private Vector3Int GetHorizontalChunkCenter(Vector3 playerWorldPosition)
    {
        var worldPosition = Vector3Int.FloorToInt(playerWorldPosition);
        var center = WorldToChunk(worldPosition);
        return new Vector3Int(center.x, 0, center.z);
    }

    private bool TryDequeueNearestStreamLoad(Vector3Int horizontalCenter, out Vector3Int nearest)
    {
        nearest = default;
        if (pendingStreamLoadSet.Count == 0)
        {
            return false;
        }

        var bestDistance = int.MaxValue;
        foreach (var coord in pendingStreamLoadSet)
        {
            var distance = HorizontalChunkDistance(horizontalCenter, new Vector3Int(coord.x, 0, coord.z));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = coord;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            return false;
        }

        RemovePendingStreamLoad(nearest);
        return true;
    }

    private void RemovePendingStreamLoad(Vector3Int chunkCoord)
    {
        if (!pendingStreamLoadSet.Remove(chunkCoord))
        {
            return;
        }

        var buffer = new Queue<Vector3Int>(pendingStreamLoads.Count);
        while (pendingStreamLoads.Count > 0)
        {
            var pending = pendingStreamLoads.Dequeue();
            if (pending != chunkCoord)
            {
                buffer.Enqueue(pending);
            }
        }

        while (buffer.Count > 0)
        {
            pendingStreamLoads.Enqueue(buffer.Dequeue());
        }
    }

    private bool TrySelectNearestPendingGeneration(Vector3Int horizontalCenter, out Vector3Int nearest)
    {
        nearest = default;
        if (pendingGenerationSet.Count == 0)
        {
            return false;
        }

        var bestDistance = int.MaxValue;
        chunkCoordSortBuffer.Clear();
        foreach (var coord in pendingGenerationSet)
        {
            chunkCoordSortBuffer.Add(coord);
        }

        for (int i = 0; i < chunkCoordSortBuffer.Count; i++)
        {
            var coord = chunkCoordSortBuffer[i];
            if (!chunks.ContainsKey(coord) || generatedChunks.Contains(coord))
            {
                RemovePendingGeneration(coord);
                continue;
            }

            var distance = HorizontalChunkDistance(horizontalCenter, new Vector3Int(coord.x, 0, coord.z));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = coord;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            return false;
        }

        RemovePendingGeneration(nearest);
        return true;
    }

    private void FlushCriticalGenerations(Vector3Int horizontalCenter, int syncRadius, int maxPerFrame)
    {
        if (syncRadius <= 0 || maxPerFrame <= 0)
        {
            return;
        }

        chunkCoordSortBuffer.Clear();
        foreach (var pair in chunks)
        {
            var coord = pair.Key;
            if (generatedChunks.Contains(coord) || backgroundGenerator.IsInFlight(coord))
            {
                continue;
            }

            var distance = HorizontalChunkDistance(horizontalCenter, new Vector3Int(coord.x, 0, coord.z));
            if (distance > syncRadius)
            {
                continue;
            }

            chunkCoordSortBuffer.Add(coord);
        }

        if (chunkCoordSortBuffer.Count == 0)
        {
            return;
        }

        chunkCoordSortBuffer.Sort((a, b) =>
            HorizontalChunkDistance(horizontalCenter, new Vector3Int(a.x, 0, a.z))
                .CompareTo(HorizontalChunkDistance(horizontalCenter, new Vector3Int(b.x, 0, b.z))));

        var processed = 0;
        for (int i = 0; i < chunkCoordSortBuffer.Count && processed < maxPerFrame; i++)
        {
            var coord = chunkCoordSortBuffer[i];
            if (!chunks.TryGetValue(coord, out var chunk) || generatedChunks.Contains(coord))
            {
                continue;
            }

            RemovePendingGeneration(coord);
            GenerateChunkDataSync(chunk);
            QueueChunkPresentationChanged(coord);
            processed++;
        }
    }

    private bool IsChunkCoordInWorld(Vector3Int chunkCoord)
    {
        return chunkCoord.y >= 0 && chunkCoord.y <= MaxChunkY;
    }

    private static int HorizontalChunkDistance(Vector3Int center, Vector3Int target)
    {
        return Mathf.Max(Mathf.Abs(center.x - target.x), Mathf.Abs(center.z - target.z));
    }

    internal void SetBlockForGeneration(Vector3Int worldPosition, VoxelBlockType blockType, ChunkBlockData targetChunk)
    {
        if (!IsInWorld(worldPosition) || targetChunk == null)
        {
            return;
        }

        if (WorldToChunk(worldPosition) != targetChunk.Coord)
        {
            return;
        }

        targetChunk.SetBlock(WorldToLocal(worldPosition), blockType);
    }

    internal void SetFluidForGeneration(Vector3Int worldPosition, FluidCell fluid, ChunkBlockData targetChunk)
    {
        if (!IsInWorld(worldPosition) || targetChunk == null || fluid.IsEmpty)
        {
            return;
        }

        if (WorldToChunk(worldPosition) != targetChunk.Coord)
        {
            return;
        }

        var local = WorldToLocal(worldPosition);
        if (targetChunk.GetBlock(local) != VoxelBlockType.Air)
        {
            return;
        }

        targetChunk.SetFluid(local, fluid);
    }

    internal bool IsFluidSimulationReady(Vector3Int position)
    {
        if (!IsInWorld(position))
        {
            return false;
        }

        var chunkCoord = WorldToChunk(position);
        return chunks.ContainsKey(chunkCoord) && generatedChunks.Contains(chunkCoord);
    }

    internal bool IsFluidChunkResident(Vector3Int position)
    {
        if (!IsInWorld(position))
        {
            return false;
        }

        return chunks.ContainsKey(WorldToChunk(position));
    }

    internal int GetTerrainSurfaceY(int worldX, int worldZ)
    {
        var key = PackColumnKey(worldX, worldZ);
        if (terrainSurfaceYCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var surfaceY = -1;
        for (int y = WorldHeight - 1; y >= MinWorldY; y--)
        {
            var position = new Vector3Int(worldX, y, worldZ);
            if (!IsInWorld(position))
            {
                continue;
            }

            if (IsFullCubeBlock(position))
            {
                surfaceY = y;
                break;
            }
        }

        terrainSurfaceYCache[key] = surfaceY;
        return surfaceY;
    }

    internal int GetFluidGroundSupportY(Vector3Int fluidPosition)
    {
        var minY = Mathf.Max(MinWorldY, fluidPosition.y - 24);
        for (int y = fluidPosition.y - 1; y >= minY; y--)
        {
            var position = new Vector3Int(fluidPosition.x, y, fluidPosition.z);
            if (!IsInWorld(position))
            {
                break;
            }

            if (IsFullCubeBlock(position))
            {
                return y;
            }
        }

        return -1;
    }

    private static long PackColumnKey(int worldX, int worldZ)
    {
        return ((long)worldX << 32) | (uint)worldZ;
    }

    private void InvalidateTerrainSurfaceCache(int worldX, int worldZ)
    {
        terrainSurfaceYCache.Remove(PackColumnKey(worldX, worldZ));
    }

    internal bool TrySetFluidForSimulation(Vector3Int position, FluidCell fluid)
    {
        if (!IsFluidSimulationReady(position) || fluid.IsEmpty)
        {
            return false;
        }

        var chunkCoord = WorldToChunk(position);
        var chunk = chunks[chunkCoord];
        var local = WorldToLocal(position);
        if (chunk.GetBlock(local) != VoxelBlockType.Air)
        {
            return false;
        }

        var current = chunk.GetFluid(local);
        if (current.Type == fluid.Type
            && current.Level == fluid.Level
            && current.IsSource == fluid.IsSource)
        {
            return false;
        }

        chunk.SetFluid(local, fluid);
        modifiedChunkCoords.Add(chunkCoord);
        MarkChunkDirty(chunkCoord);
        MarkNeighborChunksDirtyIfBorder(local, chunkCoord);
        return true;
    }

    internal void EnqueueFluidSpreadWake(Vector3Int position)
    {
        fluidSpreadSimulator.Enqueue(position);
    }

    internal void EnqueueFluidSpreadFrontier(Vector3Int position)
    {
        fluidSpreadSimulator.EnqueueFrontier(position);
    }

    private void WakeFluidSpreadAround(Vector3Int position)
    {
        fluidSpreadSimulator.EnqueueFrontier(position);
        fluidSpreadSimulator.EnqueueFrontier(position + Vector3Int.up);
        fluidSpreadSimulator.EnqueueFrontier(position + Vector3Int.down);
        fluidSpreadSimulator.EnqueueFrontier(position + Vector3Int.left);
        fluidSpreadSimulator.EnqueueFrontier(position + Vector3Int.right);
        fluidSpreadSimulator.EnqueueFrontier(position + new Vector3Int(0, 0, 1));
        fluidSpreadSimulator.EnqueueFrontier(position + new Vector3Int(0, 0, -1));
    }

    private void WakeFluidsAfterGeneration(Vector3Int chunkCoord)
    {
        // Ocean sources are placed at generation time; only simulate fluids after block changes.
    }

    private void MarkChunkDirty(Vector3Int chunkCoord)
    {
        dirtyChunks.Add(chunkCoord);
    }

    private void MarkNeighborChunksDirtyIfBorder(Vector3Int localPos, Vector3Int chunkCoord)
    {
        if (localPos.x == 0) MarkChunkDirty(chunkCoord + Vector3Int.left);
        if (localPos.x == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector3Int.right);
        if (localPos.y == 0) MarkChunkDirty(chunkCoord + Vector3Int.down);
        if (localPos.y == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector3Int.up);
        if (localPos.z == 0) MarkChunkDirty(chunkCoord + new Vector3Int(0, 0, -1));
        if (localPos.z == ChunkSize - 1) MarkChunkDirty(chunkCoord + new Vector3Int(0, 0, 1));
    }

    private void FlushPresentationDirty()
    {
        foreach (var chunkCoord in dirtyChunks)
        {
            NotifyPresentationDirty(chunkCoord);
        }

        dirtyChunks.Clear();
        FlushPresentationDirtyBatch();
    }

    internal bool TryPlaceCampfireFoundation(Vector3Int foundationBlock, Vector3 faceNormal, out string message)
    {
        if (!WorldItemInteraction.IsTopFaceHit(faceNormal))
        {
            message = "Place grass on top of a block.";
            return false;
        }

        if (!IsBlockOccupied(foundationBlock))
        {
            message = "Need a solid block for the campfire.";
            return false;
        }

        var anchor = WorldItemInteraction.GetAssemblyAnchor(foundationBlock);
        if (!IsInWorld(anchor))
        {
            message = "Not enough space above.";
            return false;
        }

        if (GetBlock(anchor) != VoxelBlockType.Air || campfireAssemblies.ContainsKey(anchor))
        {
            message = "Space above is blocked.";
            return false;
        }

        campfireAssemblies[anchor] = new CampfireAssembly(anchor, foundationBlock);
        message = "Campfire foundation placed. Add sticks.";
        return true;
    }

    internal bool TryAddCampfireStick(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out _, out var stickAssembly))
        {
            message = "No campfire foundation here.";
            return false;
        }

        return stickAssembly.TryAddStick(out message);
    }

    internal bool TryLightCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out var lightAnchor, out var lightAssembly))
        {
            message = "No campfire foundation here.";
            return false;
        }

        if (!lightAssembly.CanLight())
        {
            message = $"Need {CampfireAssembly.RequiredSticks} sticks before lighting.";
            return false;
        }

        return TryLightCampfireAssemblyAtAnchor(lightAnchor, out message);
    }

    private bool TryLightCampfireAssemblyAtAnchor(Vector3Int anchorPosition, out string message)
    {
        if (!campfireAssemblies.TryGetValue(anchorPosition, out var assembly))
        {
            message = "No campfire here.";
            return false;
        }

        if (!assembly.CanLight())
        {
            message = $"Need {CampfireAssembly.RequiredSticks} sticks before lighting.";
            return false;
        }

        campfireAssemblies.Remove(anchorPosition);

        if (GetBlock(anchorPosition) != VoxelBlockType.Air)
        {
            message = "Space is blocked.";
            return false;
        }

        if (!TrySetBlock(anchorPosition, VoxelBlockType.Campfire))
        {
            message = "Could not place campfire.";
            return false;
        }

        if (campfires.TryGetValue(anchorPosition, out var campfire))
        {
            campfire.StartLit(8f);
        }

        message = "Campfire lit!";
        return true;
    }

    internal bool TryResolveCampfireAssembly(Vector3Int clickedBlock, Vector3 faceNormal, out Vector3Int anchor, out CampfireAssembly assembly)
    {
        assembly = null;
        if (WorldItemInteraction.TryResolveAssemblyAnchor(
                clickedBlock,
                faceNormal,
                campfireAssemblies.ContainsKey,
                out anchor) &&
            campfireAssemblies.TryGetValue(anchor, out assembly))
        {
            return true;
        }

        anchor = default;
        return false;
    }

    private void RemoveCampfireAssembliesAffectedByBlockChange(Vector3Int position, VoxelBlockType oldType, VoxelBlockType newType)
    {
        campfireAssemblies.Remove(position);

        if (oldType != VoxelBlockType.Air && newType == VoxelBlockType.Air)
        {
            campfireAssemblies.Remove(position + Vector3Int.up);
        }
    }

    private void UpdateFunctionalBlockEntities(Vector3Int position, VoxelBlockType oldType, VoxelBlockType newType)
    {
        if (oldType == VoxelBlockType.Campfire && newType != VoxelBlockType.Campfire)
        {
            campfires.Remove(position);
        }

        if (newType == VoxelBlockType.Campfire && oldType != VoxelBlockType.Campfire)
        {
            campfires[position] = new CampfireBlockEntity();
        }
    }

    private Vector3Int WorldToChunk(Vector3Int world)
    {
        return new Vector3Int(
            FloorDiv(world.x, ChunkSize),
            FloorDiv(world.y, ChunkSize),
            FloorDiv(world.z, ChunkSize));
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }

        return (value - divisor + 1) / divisor;
    }

    private Vector3Int WorldToLocal(Vector3Int world)
    {
        return new Vector3Int(
            VoxelConstants.Mod(world.x, ChunkSize),
            VoxelConstants.Mod(world.y, ChunkSize),
            VoxelConstants.Mod(world.z, ChunkSize));
    }
}
