using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public sealed class ChunkWorldPresenter
{
    private readonly IWorldSimulation simulation;
    private readonly ChunkTerrainDrawer terrainDrawer;
    private readonly Dictionary<Vector3Int, ChunkRenderEntry> renderChunks = new();
    private readonly Queue<Vector3Int> pendingMeshBuilds = new();
    private readonly HashSet<Vector3Int> pendingMeshBuildSet = new();
    private readonly ChunkMeshScratch meshScratch = new();
    private readonly List<Vector3Int> meshBuildSortBuffer = new();
    private readonly BackgroundChunkMeshBuilder backgroundMeshBuilder = new();
    private readonly Dictionary<Vector3Int, int> meshBuildVersions = new();

    private const int MaxBackgroundMeshesInFlight = 3;

    private ChunkStreamingSettings streamingSettings;
    private Material chunkMaterial;
    private Vector3 lastPlayerWorldPosition;
    private float estimatedBaseFrameMs = 8f;
    private float lastResolvedStreamingBudgetMs = 3f;

    public IReadOnlyDictionary<Vector3Int, ChunkRenderEntry> RenderChunks => renderChunks;

    public ChunkWorldPresenter(IWorldSimulation simulation, ChunkTerrainDrawer drawer)
    {
        this.simulation = simulation;
        terrainDrawer = drawer;
        simulation.ChunkPresentationChanged += HandleChunkPresentationChanged;
        simulation.ChunkUnloaded += HandleChunkUnloaded;
    }

    public void Configure(ChunkStreamingSettings settings, Material material)
    {
        streamingSettings = settings;
        chunkMaterial = material;
        terrainDrawer.Configure(chunkMaterial, renderChunks);
    }

    public void UpdateStreaming(Vector3 playerWorldPosition)
    {
        lastPlayerWorldPosition = playerWorldPosition;

        var settings = streamingSettings;
        var totalBudgetMs = ResolveStreamingBudgetMs(settings);
        lastResolvedStreamingBudgetMs = totalBudgetMs;
        var loadRequests = settings != null ? settings.MaxChunkLoadRequestsPerFrame : 8;
        var maxMeshBuilds = settings != null ? settings.MaxMeshBuildsPerFrame : 1;

        var generationBudget = totalBudgetMs * 0.4f;
        var frameStopwatch = Stopwatch.StartNew();

        using (RuntimeFrameProfiler.Begin("stream.layout"))
        {
            simulation.UpdateChunkStreaming(playerWorldPosition);
        }

        using (RuntimeFrameProfiler.Begin("stream.load"))
        {
            simulation.ProcessChunkLoadRequests(loadRequests);
        }

        using (RuntimeFrameProfiler.Begin("stream.generate"))
        {
            simulation.ProcessGenerationBudget(playerWorldPosition, generationBudget);
        }

        var syncRadius = settings != null ? settings.SyncGenerationRadiusChunks : 2;

        using (RuntimeFrameProfiler.Begin("stream.mesh.apply"))
        {
            ApplyCompletedMeshes(16);
        }

        using (RuntimeFrameProfiler.Begin("stream.mesh.critical"))
        {
            ProcessCriticalMeshBuilds(playerWorldPosition, syncRadius);
        }

        var meshBudget = Mathf.Max(0.5f, totalBudgetMs - (float)frameStopwatch.Elapsed.TotalMilliseconds);
        using (RuntimeFrameProfiler.Begin("stream.mesh"))
        {
            ScheduleBackgroundMeshes(maxMeshBuilds, meshBudget, syncRadius);
            ApplyCompletedMeshes(8);
        }
    }

    public void ProcessMeshQueues()
    {
        ProcessMeshQueues(streamingSettings);
    }

    public void ProcessMeshQueues(ChunkStreamingSettings settings)
    {
        var syncRadius = settings != null ? settings.SyncGenerationRadiusChunks : 2;
        ApplyCompletedMeshes(16);
        ScheduleBackgroundMeshes(
            settings != null ? settings.MaxMeshBuildsPerFrame : 2,
            settings != null ? settings.MaxStreamingMillisecondsPerFrame : 6f,
            syncRadius);
        ApplyCompletedMeshes(8);
    }

    public void FlushPendingMeshes(int maxIterations = 128)
    {
        for (int i = 0; i < maxIterations && HasStreamingWorkPending(); i++)
        {
            simulation.ProcessChunkLoadRequests(16);
            simulation.ProcessGenerationBudget(lastPlayerWorldPosition, 12f);
            ApplyCompletedMeshes(16);
            ProcessCriticalMeshBuilds(lastPlayerWorldPosition, streamingSettings != null ? streamingSettings.SyncGenerationRadiusChunks : 2);
            ScheduleBackgroundMeshes(maxSchedules: 4, maxMilliseconds: 12f, syncRadius: int.MaxValue);
            ApplyCompletedMeshes(16);
        }
    }

    private bool HasStreamingWorkPending()
    {
        return pendingMeshBuilds.Count > 0
            || simulation.HasPendingStreaming
            || backgroundMeshBuilder.InFlightCount > 0
            || backgroundMeshBuilder.CompletedCount > 0;
    }

    private void ProcessCriticalMeshBuilds(Vector3 playerWorldPosition, int syncRadius)
    {
        if (syncRadius <= 0 || pendingMeshBuilds.Count == 0)
        {
            return;
        }

        var horizontalCenter = WorldToChunkCenter(playerWorldPosition);
        horizontalCenter = new Vector3Int(horizontalCenter.x, 0, horizontalCenter.z);

        meshBuildSortBuffer.Clear();
        while (pendingMeshBuilds.Count > 0)
        {
            meshBuildSortBuffer.Add(pendingMeshBuilds.Dequeue());
        }

        pendingMeshBuildSet.Clear();

        for (int i = 0; i < meshBuildSortBuffer.Count; i++)
        {
            var coord = meshBuildSortBuffer[i];
            var distance = HorizontalChunkDistance(new Vector3Int(coord.x, 0, coord.z), horizontalCenter);
            if (distance <= syncRadius)
            {
                TryBuildChunkMeshSync(coord);
            }
            else
            {
                EnqueueMeshBuild(coord);
            }
        }
    }

    private void ScheduleBackgroundMeshes(int maxSchedules, float maxMilliseconds, int syncRadius)
    {
        if (pendingMeshBuilds.Count == 0)
        {
            return;
        }

        meshBuildSortBuffer.Clear();
        while (pendingMeshBuilds.Count > 0)
        {
            meshBuildSortBuffer.Add(pendingMeshBuilds.Dequeue());
        }

        pendingMeshBuildSet.Clear();

        var playerChunk = WorldToChunkCenter(lastPlayerWorldPosition);
        var horizontalPlayer = new Vector3Int(playerChunk.x, 0, playerChunk.z);
        meshBuildSortBuffer.Sort((a, b) => HorizontalChunkDistance(a, playerChunk).CompareTo(HorizontalChunkDistance(b, playerChunk)));

        var stopwatch = Stopwatch.StartNew();
        var scheduled = 0;
        var resumeIndex = 0;

        for (int i = 0; i < meshBuildSortBuffer.Count && scheduled < maxSchedules; i++)
        {
            if (maxMilliseconds > 0f && stopwatch.Elapsed.TotalMilliseconds >= maxMilliseconds)
            {
                resumeIndex = i;
                break;
            }

            var chunkCoord = meshBuildSortBuffer[i];
            var horizontalDistance = HorizontalChunkDistance(
                new Vector3Int(chunkCoord.x, 0, chunkCoord.z),
                horizontalPlayer);

            if (horizontalDistance <= syncRadius)
            {
                if (TryBuildChunkMeshSync(chunkCoord))
                {
                    scheduled++;
                }

                resumeIndex = i + 1;
                continue;
            }

            if (backgroundMeshBuilder.IsInFlight(chunkCoord))
            {
                EnqueueMeshBuild(chunkCoord);
                resumeIndex = i + 1;
                continue;
            }

            if (backgroundMeshBuilder.InFlightCount >= MaxBackgroundMeshesInFlight)
            {
                resumeIndex = i;
                break;
            }

            if (TryScheduleBackgroundMesh(chunkCoord))
            {
                scheduled++;
            }

            resumeIndex = i + 1;

            if (maxMilliseconds > 0f && stopwatch.Elapsed.TotalMilliseconds >= maxMilliseconds)
            {
                break;
            }
        }

        for (int i = resumeIndex; i < meshBuildSortBuffer.Count; i++)
        {
            EnqueueMeshBuild(meshBuildSortBuffer[i]);
        }
    }

    private void ApplyCompletedMeshes(int maxApply)
    {
        for (int i = 0; i < maxApply && backgroundMeshBuilder.TryDequeueCompleted(out var result); i++)
        {
            ApplyBackgroundMeshResult(result);
        }
    }

    private void ApplyBackgroundMeshResult(BackgroundChunkMeshBuilder.CompletedMesh result)
    {
        if (!meshBuildVersions.TryGetValue(result.Coord, out var currentVersion)
            || result.BuildVersion != currentVersion)
        {
            return;
        }

        if (!simulation.TryGetChunkBlocks(result.Coord, out _))
        {
            return;
        }

        if (result.Geometry == null || result.Geometry.IsEmpty)
        {
            RemoveRenderEntry(result.Coord);
            return;
        }

        var entry = GetOrCreateRenderEntry(result.Coord);
        entry.ApplyMesh(
            result.Geometry.Vertices,
            result.Geometry.Triangles,
            result.Geometry.Normals,
            result.Geometry.Uvs,
            result.Geometry.TileRects,
            result.Geometry.TileCounts);

        if (!entry.HasVisibleGeometry)
        {
            RemoveRenderEntry(result.Coord);
        }
    }

    private bool TryScheduleBackgroundMesh(Vector3Int chunkCoord)
    {
        if (!simulation.TryGetChunkBlocks(chunkCoord, out var blocks))
        {
            return false;
        }

        if (blocks.IsEmpty())
        {
            RemoveRenderEntry(chunkCoord);
            return true;
        }

        if (!meshBuildVersions.TryGetValue(chunkCoord, out var buildVersion))
        {
            return false;
        }

        var snapshot = ChunkMeshBuildSnapshot.Capture(simulation, blocks);
        return backgroundMeshBuilder.TryStart(chunkCoord, buildVersion, snapshot);
    }

    private bool TryBuildChunkMeshSync(Vector3Int chunkCoord)
    {
        if (!simulation.TryGetChunkBlocks(chunkCoord, out var blocks))
        {
            return false;
        }

        if (blocks.IsEmpty())
        {
            RemoveRenderEntry(chunkCoord);
            return true;
        }

        var entry = GetOrCreateRenderEntry(chunkCoord);
        ChunkMeshBuilder.BuildChunkMesh(simulation, blocks, entry, meshScratch);

        if (!entry.HasVisibleGeometry)
        {
            RemoveRenderEntry(chunkCoord);
        }

        return true;
    }

    private void HandleChunkPresentationChanged(Vector3Int chunkCoord)
    {
        if (!simulation.TryGetChunkBlocks(chunkCoord, out _))
        {
            return;
        }

        meshBuildVersions[chunkCoord] = meshBuildVersions.GetValueOrDefault(chunkCoord) + 1;
        EnqueueMeshBuild(chunkCoord);
    }

    private void HandleChunkUnloaded(Vector3Int chunkCoord)
    {
        pendingMeshBuildSet.Remove(chunkCoord);
        meshBuildVersions.Remove(chunkCoord);
        RemoveRenderEntry(chunkCoord);
    }

    private ChunkRenderEntry GetOrCreateRenderEntry(Vector3Int chunkCoord)
    {
        if (!renderChunks.TryGetValue(chunkCoord, out var entry))
        {
            entry = new ChunkRenderEntry(chunkCoord, simulation.ChunkSize);
            renderChunks[chunkCoord] = entry;
        }

        return entry;
    }

    private void RemoveRenderEntry(Vector3Int chunkCoord)
    {
        if (!renderChunks.TryGetValue(chunkCoord, out var entry))
        {
            return;
        }

        entry.Dispose();
        renderChunks.Remove(chunkCoord);
    }

    private void EnqueueMeshBuild(Vector3Int chunkCoord)
    {
        if (pendingMeshBuildSet.Add(chunkCoord))
        {
            pendingMeshBuilds.Enqueue(chunkCoord);
        }
    }

    private float ResolveStreamingBudgetMs(ChunkStreamingSettings settings)
    {
        if (settings == null)
        {
            return 3f;
        }

        if (!settings.UseAdaptiveFramePacing)
        {
            return settings.MaxStreamingMillisecondsPerFrame;
        }

        var lastStreamingMs = (float)RuntimeFrameProfiler.GetLastSectionMs("client.streaming");
        var lastFrameMs = (float)RuntimeFrameProfiler.LastFrameMs;
        if (lastFrameMs > 0.1f)
        {
            var observedBase = Mathf.Max(5f, lastFrameMs - lastStreamingMs);
            estimatedBaseFrameMs = Mathf.Lerp(estimatedBaseFrameMs, observedBase, 0.1f);
        }

        return Mathf.Clamp(
            settings.TargetFrameMilliseconds - estimatedBaseFrameMs,
            settings.MinStreamingMillisecondsPerFrame,
            settings.MaxStreamingMillisecondsPerFrame);
    }

    public float EstimatedBaseFrameMs => estimatedBaseFrameMs;

    public float LastStreamingBudgetMs => lastResolvedStreamingBudgetMs;

    private Vector3Int WorldToChunkCenter(Vector3 worldPosition)
    {
        var chunkSize = simulation.ChunkSize;
        var world = Vector3Int.FloorToInt(worldPosition);
        return new Vector3Int(
            FloorDiv(world.x, chunkSize),
            FloorDiv(world.y, chunkSize),
            FloorDiv(world.z, chunkSize));
    }

    private static int HorizontalChunkDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z));
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }

        return (value - divisor + 1) / divisor;
    }
}
