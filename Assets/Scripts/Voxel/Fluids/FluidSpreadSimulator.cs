using System.Collections.Generic;
using UnityEngine;

internal sealed class FluidSpreadSimulator
{
    private const float SpreadIntervalSeconds = 0.22f;
    private const int MaxUpdatesPerSpreadTick = 96;

    private static readonly Vector3Int[] HorizontalOffsets =
    {
        Vector3Int.left,
        Vector3Int.right,
        new(0, 0, 1),
        new(0, 0, -1)
    };

    private readonly Queue<Vector3Int> pendingUpdates = new();
    private readonly HashSet<Vector3Int> pendingSet = new();
    private readonly Queue<Vector3Int> frontierUpdates = new();
    private readonly HashSet<Vector3Int> frontierSet = new();
    private float tickAccumulator;

    private long spreadTickCount;
    private long totalCellsProcessed;
    private long totalFluidChanges;
    private int lastTickProcessed;
    private int lastTickChanges;

    public FluidSimulationDiagnostics GetDiagnostics()
    {
        return new FluidSimulationDiagnostics
        {
            SpreadTickCount = spreadTickCount,
            TotalCellsProcessed = totalCellsProcessed,
            TotalFluidChanges = totalFluidChanges,
            PendingQueueCount = pendingUpdates.Count,
            FrontierQueueCount = frontierUpdates.Count,
            LastTickProcessed = lastTickProcessed,
            LastTickChanges = lastTickChanges
        };
    }

    public void ClearQueues()
    {
        pendingUpdates.Clear();
        pendingSet.Clear();
        frontierUpdates.Clear();
        frontierSet.Clear();
    }

    public void Enqueue(Vector3Int worldPosition)
    {
        if (pendingSet.Add(worldPosition))
        {
            pendingUpdates.Enqueue(worldPosition);
        }
    }

    public void EnqueueFrontier(Vector3Int worldPosition)
    {
        if (frontierSet.Add(worldPosition))
        {
            frontierUpdates.Enqueue(worldPosition);
        }
    }

    public void EnqueueActiveFluidsInChunk(WorldSimulation world, ChunkBlockData chunk, int chunkSize)
    {
        if (world == null || chunk == null)
        {
            return;
        }

        var origin = ChunkOrigin(chunk.Coord, chunkSize);
        for (int localZ = 0; localZ < chunkSize; localZ++)
        {
            for (int localY = 0; localY < chunkSize; localY++)
            {
                for (int localX = 0; localX < chunkSize; localX++)
                {
                    var local = new Vector3Int(localX, localY, localZ);
                    var worldPosition = origin + local;
                    var fluid = chunk.GetFluid(local);
                    if (fluid.IsEmpty || fluid.IsSource)
                    {
                        continue;
                    }

                    if (CanPotentiallySpread(world, worldPosition))
                    {
                        Enqueue(worldPosition);
                    }
                }
            }
        }
    }

    public void EnqueueActiveFluidsOnChunkFace(
        WorldSimulation world,
        ChunkBlockData chunk,
        int chunkSize,
        ChunkFace face)
    {
        if (world == null || chunk == null)
        {
            return;
        }

        var origin = ChunkOrigin(chunk.Coord, chunkSize);
        var max = chunkSize - 1;
        for (int a = 0; a < chunkSize; a++)
        {
            for (int b = 0; b < chunkSize; b++)
            {
                var local = face switch
                {
                    ChunkFace.NegativeX => new Vector3Int(0, a, b),
                    ChunkFace.PositiveX => new Vector3Int(max, a, b),
                    ChunkFace.NegativeY => new Vector3Int(a, 0, b),
                    ChunkFace.PositiveY => new Vector3Int(a, max, b),
                    ChunkFace.NegativeZ => new Vector3Int(a, b, 0),
                    _ => new Vector3Int(a, b, max)
                };

                var worldPosition = origin + local;
                var fluid = chunk.GetFluid(local);
                if (fluid.IsEmpty || fluid.IsSource)
                {
                    continue;
                }

                if (CanPotentiallySpread(world, worldPosition))
                {
                    EnqueueFrontier(worldPosition);
                }
            }
        }
    }

    public void Tick(WorldSimulation world, float deltaTime)
    {
        if (deltaTime <= 0f || world == null)
        {
            return;
        }

        tickAccumulator += deltaTime;
        if (tickAccumulator < SpreadIntervalSeconds)
        {
            return;
        }

        tickAccumulator -= SpreadIntervalSeconds;
        spreadTickCount++;

        var processed = 0;
        var changes = 0;
        while (processed < MaxUpdatesPerSpreadTick)
        {
            if (!TryDequeueNext(out var position, out var fromFrontier))
            {
                break;
            }

            if (!fromFrontier)
            {
                var queuedFluid = world.GetFluid(position);
                if (!queuedFluid.IsEmpty && queuedFluid.IsSource)
                {
                    continue;
                }
            }

            if (!world.IsFluidSimulationReady(position))
            {
                if (world.IsFluidChunkResident(position))
                {
                    Enqueue(position);
                }

                continue;
            }

            processed++;
            totalCellsProcessed++;
            if (TrySpreadFrom(world, position))
            {
                changes++;
                totalFluidChanges++;
                EnqueueFrontier(position);
            }
        }

        lastTickProcessed = processed;
        lastTickChanges = changes;
    }

    private bool TryDequeueNext(out Vector3Int position, out bool fromFrontier)
    {
        if (frontierUpdates.Count > 0)
        {
            position = frontierUpdates.Dequeue();
            frontierSet.Remove(position);
            fromFrontier = true;
            return true;
        }

        if (pendingUpdates.Count > 0)
        {
            position = pendingUpdates.Dequeue();
            pendingSet.Remove(position);
            fromFrontier = false;
            return true;
        }

        position = default;
        fromFrontier = false;
        return false;
    }

    private static bool TrySpreadFrom(WorldSimulation world, Vector3Int position)
    {
        var fluid = world.GetFluid(position);
        if (fluid.IsEmpty)
        {
            return false;
        }

        var below = position + Vector3Int.down;
        if (world.IsInWorld(below) && CanFlowDown(world, below))
        {
            return TryPlaceFallingFluid(world, below, fluid);
        }

        if (!IsOnFlatSpreadSurface(world, position))
        {
            return false;
        }

        var changed = false;
        foreach (var offset in HorizontalOffsets)
        {
            var neighbor = position + offset;
            if (!world.IsInWorld(neighbor))
            {
                continue;
            }

            changed |= TryFlowHorizontal(world, position, neighbor, fluid);

            var belowNeighbor = neighbor + Vector3Int.down;
            if (world.IsInWorld(belowNeighbor) && CanFlowDown(world, belowNeighbor))
            {
                changed |= TryPlaceFallingFluid(world, belowNeighbor, fluid);
            }
        }

        return changed;
    }

    private static bool CanFlowDown(WorldSimulation world, Vector3Int below)
    {
        if (world.GetBlock(below) != VoxelBlockType.Air)
        {
            return false;
        }

        var belowFluid = world.GetFluid(below);
        return belowFluid.IsEmpty || belowFluid.Level < FluidConstants.MaxLevel;
    }

    private static bool TryPlaceFallingFluid(WorldSimulation world, Vector3Int target, FluidCell sourceFluid)
    {
        if (world.GetBlock(target) != VoxelBlockType.Air)
        {
            return false;
        }

        var targetFluid = world.GetFluid(target);
        var flowLevel = FluidConstants.MaxLevel;
        if (!targetFluid.IsEmpty && targetFluid.Level >= flowLevel)
        {
            return false;
        }

        if (!world.TrySetFluidForSimulation(target, new FluidCell
        {
            Type = sourceFluid.Type,
            Level = flowLevel,
            IsSource = false
        }))
        {
            return false;
        }

        world.EnqueueFluidSpreadFrontier(target);
        return true;
    }

    private static bool TryFlowHorizontal(
        WorldSimulation world,
        Vector3Int source,
        Vector3Int target,
        FluidCell sourceFluid)
    {
        if (!CanSpreadHorizontallyBetween(world, source, target))
        {
            return false;
        }

        if (!TryGetHorizontalFlowLevel(sourceFluid, out var flowLevel))
        {
            return false;
        }

        var targetFluid = world.GetFluid(target);
        if (!targetFluid.IsEmpty && targetFluid.Level >= flowLevel)
        {
            return false;
        }

        if (!world.TrySetFluidForSimulation(target, new FluidCell
        {
            Type = sourceFluid.Type,
            Level = flowLevel,
            IsSource = false
        }))
        {
            return false;
        }

        world.EnqueueFluidSpreadFrontier(target);
        return true;
    }

    private static bool CanSpreadHorizontallyBetween(
        WorldSimulation world,
        Vector3Int source,
        Vector3Int target)
    {
        if (source.y != target.y || world.GetBlock(target) != VoxelBlockType.Air)
        {
            return false;
        }

        var sourceBelow = source + Vector3Int.down;
        var targetBelow = target + Vector3Int.down;
        if (!world.IsInWorld(sourceBelow) || !world.IsInWorld(targetBelow))
        {
            return false;
        }

        if (CanFlowDown(world, sourceBelow) || CanFlowDown(world, targetBelow))
        {
            return false;
        }

        var sourceTerrain = world.GetTerrainSurfaceY(source.x, source.z);
        var targetTerrain = world.GetTerrainSurfaceY(target.x, target.z);
        if (sourceTerrain < 0 || sourceTerrain != targetTerrain)
        {
            return false;
        }

        var sourceGround = world.GetFluidGroundSupportY(source);
        var targetGround = world.GetFluidGroundSupportY(target);
        return sourceGround >= 0 && sourceGround == targetGround;
    }

    private static bool CanPotentiallySpread(WorldSimulation world, Vector3Int position)
    {
        var fluid = world.GetFluid(position);
        if (fluid.IsEmpty)
        {
            return false;
        }

        var below = position + Vector3Int.down;
        if (world.IsInWorld(below) && CanFlowDown(world, below))
        {
            return true;
        }

        if (!IsOnFlatSpreadSurface(world, position))
        {
            return false;
        }

        if (!TryGetHorizontalFlowLevel(fluid, out var flowLevel))
        {
            return false;
        }

        foreach (var offset in HorizontalOffsets)
        {
            var neighbor = position + offset;
            if (!world.IsInWorld(neighbor))
            {
                continue;
            }

            if (CanSpreadHorizontallyBetween(world, position, neighbor))
            {
                var neighborFluid = world.GetFluid(neighbor);
                if (neighborFluid.IsEmpty || neighborFluid.Level < flowLevel)
                {
                    return true;
                }
            }

            var belowNeighbor = neighbor + Vector3Int.down;
            if (world.IsInWorld(belowNeighbor) && CanFlowDown(world, belowNeighbor))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetHorizontalFlowLevel(FluidCell sourceFluid, out byte flowLevel)
    {
        if (sourceFluid.IsSource)
        {
            flowLevel = (byte)(FluidConstants.MaxLevel - 1);
            return true;
        }

        if (sourceFluid.Level <= 1)
        {
            flowLevel = 0;
            return false;
        }

        flowLevel = (byte)(sourceFluid.Level - 1);
        return true;
    }

    /// <summary>
    /// Water can spread sideways only from a resting surface, not while falling.
    /// </summary>
    private static bool IsOnFlatSpreadSurface(WorldSimulation world, Vector3Int fluidPosition)
    {
        var below = fluidPosition + Vector3Int.down;
        if (!world.IsInWorld(below))
        {
            return false;
        }

        return !CanFlowDown(world, below) && world.GetFluidGroundSupportY(fluidPosition) >= 0;
    }

    private static Vector3Int ChunkOrigin(Vector3Int chunkCoord, int chunkSize)
    {
        return new Vector3Int(
            chunkCoord.x * chunkSize,
            chunkCoord.y * chunkSize,
            chunkCoord.z * chunkSize);
    }
}

internal enum ChunkFace
{
    NegativeX,
    PositiveX,
    NegativeY,
    PositiveY,
    NegativeZ,
    PositiveZ
}
