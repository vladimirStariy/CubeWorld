using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkMeshScratch
{
    public readonly List<Vector3> Vertices = new(2048);
    public readonly List<int> Triangles = new(4096);
    public readonly List<Vector3> Normals = new(2048);
    public readonly List<Vector2> Uvs = new(2048);
    public readonly List<Vector4> TileRects = new(2048);
    public readonly List<Vector2> TileCounts = new(2048);

    private int[,] greedyMask;

    public int[,] GetGreedyMask(int chunkSize)
    {
        if (greedyMask == null || greedyMask.GetLength(0) != chunkSize)
        {
            greedyMask = new int[chunkSize, chunkSize];
        }

        return greedyMask;
    }

    public void Clear()
    {
        Vertices.Clear();
        Triangles.Clear();
        Normals.Clear();
        Uvs.Clear();
        TileRects.Clear();
        TileCounts.Clear();
    }
}
