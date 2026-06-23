using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

internal sealed class BackgroundChunkMeshBuilder
{
    public readonly struct CompletedMesh
    {
        public readonly Vector3Int Coord;
        public readonly int BuildVersion;
        public readonly ChunkMeshGeometry Geometry;
        public readonly ChunkMeshGeometry FluidGeometry;

        public CompletedMesh(
            Vector3Int coord,
            int buildVersion,
            ChunkMeshGeometry geometry,
            ChunkMeshGeometry fluidGeometry)
        {
            Coord = coord;
            BuildVersion = buildVersion;
            Geometry = geometry;
            FluidGeometry = fluidGeometry;
        }
    }

    private readonly object gate = new();
    private readonly Queue<CompletedMesh> completed = new();
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

    public bool TryStart(Vector3Int chunkCoord, int buildVersion, ChunkMeshBuildSnapshot snapshot)
    {
        if (snapshot == null)
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

        ThreadPool.QueueUserWorkItem(_ => RunBuild(chunkCoord, buildVersion, snapshot));
        return true;
    }

    public bool TryDequeueCompleted(out CompletedMesh result)
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

    private void RunBuild(Vector3Int chunkCoord, int buildVersion, ChunkMeshBuildSnapshot snapshot)
    {
        try
        {
            var view = new DetachedChunkMeshView(snapshot);
            var blocks = new ChunkBlockData(snapshot.ChunkSize, snapshot.Coord);
            blocks.CopyBlocksFrom(snapshot.Blocks);
            blocks.CopyFluidsFrom(snapshot.Fluids);

            var solidScratch = new ChunkMeshScratch();
            ChunkMeshBuilder.BuildChunkMesh(view, blocks, solidScratch);
            var geometry = ChunkMeshGeometry.FromScratch(solidScratch);

            var fluidScratch = new ChunkMeshScratch();
            FluidMeshBuilder.BuildFluidMesh(view, view, blocks, fluidScratch);
            var fluidGeometry = ChunkMeshGeometry.FromScratch(fluidScratch);

            lock (gate)
            {
                completed.Enqueue(new CompletedMesh(chunkCoord, buildVersion, geometry, fluidGeometry));
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
