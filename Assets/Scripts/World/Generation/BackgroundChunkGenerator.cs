using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

internal sealed class BackgroundChunkGenerator
{
    public readonly struct CompletedChunk
    {
        public readonly Vector3Int Coord;
        public readonly VoxelBlockType[] Blocks;

        public CompletedChunk(Vector3Int coord, VoxelBlockType[] blocks)
        {
            Coord = coord;
            Blocks = blocks;
        }
    }

    private readonly object gate = new();
    private readonly Queue<CompletedChunk> completed = new();
    private readonly HashSet<Vector3Int> inFlight = new();

    public int InFlightCount
    {
        get
        {
            lock (gate)
            {
                return inFlight.Count;
            }
        }
    }

    public int CompletedCount
    {
        get
        {
            lock (gate)
            {
                return completed.Count;
            }
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            completed.Clear();
            inFlight.Clear();
        }
    }

    public bool IsInFlight(Vector3Int chunkCoord)
    {
        lock (gate)
        {
            return inFlight.Contains(chunkCoord);
        }
    }

    public bool TryStart(
        Vector3Int chunkCoord,
        int chunkSize,
        IChunkWorldGenerator generator,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes)
    {
        if (generator == null || settings == null || biomes == null)
        {
            return false;
        }

        lock (gate)
        {
            if (inFlight.Contains(chunkCoord))
            {
                return false;
            }

            inFlight.Add(chunkCoord);
        }

        var blockCount = chunkSize * chunkSize * chunkSize;
        ThreadPool.QueueUserWorkItem(_ => RunGeneration(chunkCoord, chunkSize, blockCount, generator, settings, items, biomes));
        return true;
    }

    public bool TryDequeueCompleted(out CompletedChunk result)
    {
        lock (gate)
        {
            if (completed.Count == 0)
            {
                result = default;
                return false;
            }

            result = completed.Dequeue();
            return true;
        }
    }

    private void RunGeneration(
        Vector3Int chunkCoord,
        int chunkSize,
        int blockCount,
        IChunkWorldGenerator generator,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes)
    {
        var blocks = new VoxelBlockType[blockCount];
        try
        {
            var context = new DetachedChunkGenerationContext(
                chunkCoord,
                chunkSize,
                settings,
                items,
                biomes,
                blocks);
            generator.GenerateChunk(chunkCoord, context);

            lock (gate)
            {
                completed.Enqueue(new CompletedChunk(chunkCoord, blocks));
                inFlight.Remove(chunkCoord);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            lock (gate)
            {
                inFlight.Remove(chunkCoord);
            }
        }
    }
}
