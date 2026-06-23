using UnityEngine;
using UnityEngine.Rendering;

public sealed class ChunkRenderEntry
{
    public Vector3Int Coord { get; }
    public Vector3 WorldOrigin { get; }
    public Matrix4x4 DrawMatrix { get; }
    public Mesh Mesh { get; private set; }
    public bool HasVisibleGeometry { get; private set; }

    public ChunkRenderEntry(Vector3Int coord, int chunkSize)
    {
        Coord = coord;
        WorldOrigin = new Vector3(
            coord.x * chunkSize,
            coord.y * chunkSize,
            coord.z * chunkSize);
        DrawMatrix = Matrix4x4.TRS(WorldOrigin, Quaternion.identity, Vector3.one);
    }

    public void Clear()
    {
        if (Mesh != null)
        {
            Mesh.Clear();
        }

        HasVisibleGeometry = false;
    }

    public void Dispose()
    {
        if (Mesh != null)
        {
            Object.Destroy(Mesh);
            Mesh = null;
        }

        HasVisibleGeometry = false;
    }

    public void ApplyMesh(
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles,
        System.Collections.Generic.List<Vector3> normals,
        System.Collections.Generic.List<Vector2> uvs,
        System.Collections.Generic.List<Vector4> tileRects,
        System.Collections.Generic.List<Vector2> tileCounts)
    {
        if (vertices.Count == 0 || triangles.Count == 0)
        {
            Clear();
            return;
        }

        if (Mesh == null)
        {
            Mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            Mesh.MarkDynamic();
        }
        else
        {
            Mesh.Clear();
        }

        Mesh.SetVertices(vertices, 0, vertices.Count, MeshUpdateFlags.DontRecalculateBounds);
        Mesh.SetTriangles(triangles, 0, false);
        Mesh.SetNormals(normals, 0, normals.Count, MeshUpdateFlags.DontRecalculateBounds);
        Mesh.SetUVs(0, uvs, 0, uvs.Count, MeshUpdateFlags.DontRecalculateBounds);
        Mesh.SetUVs(1, tileRects, 0, tileRects.Count, MeshUpdateFlags.DontRecalculateBounds);
        Mesh.SetUVs(2, tileCounts, 0, tileCounts.Count, MeshUpdateFlags.DontRecalculateBounds);
        Mesh.RecalculateBounds();
        HasVisibleGeometry = true;
    }
}
