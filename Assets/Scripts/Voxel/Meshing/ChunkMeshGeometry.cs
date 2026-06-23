using System.Collections.Generic;
using UnityEngine;

internal sealed class ChunkMeshGeometry
{
    public List<Vector3> Vertices { get; private set; }
    public List<int> Triangles { get; private set; }
    public List<Vector3> Normals { get; private set; }
    public List<Vector2> Uvs { get; private set; }
    public List<Vector4> TileRects { get; private set; }
    public List<Vector2> TileCounts { get; private set; }

    public bool IsEmpty => Vertices == null || Vertices.Count == 0 || Triangles == null || Triangles.Count == 0;

    public static ChunkMeshGeometry FromScratch(ChunkMeshScratch scratch)
    {
        return new ChunkMeshGeometry
        {
            Vertices = new List<Vector3>(scratch.Vertices),
            Triangles = new List<int>(scratch.Triangles),
            Normals = new List<Vector3>(scratch.Normals),
            Uvs = new List<Vector2>(scratch.Uvs),
            TileRects = new List<Vector4>(scratch.TileRects),
            TileCounts = new List<Vector2>(scratch.TileCounts)
        };
    }
}
