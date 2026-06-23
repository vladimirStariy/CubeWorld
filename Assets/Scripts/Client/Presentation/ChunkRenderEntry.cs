using UnityEngine;
using UnityEngine.Rendering;

public sealed class ChunkRenderEntry
{
    public Vector3Int Coord { get; }
    public Vector3 WorldOrigin { get; }
    public Matrix4x4 DrawMatrix { get; }
    public Mesh Mesh => mesh;
    public Mesh FluidMesh => fluidMesh;
    public bool HasVisibleGeometry { get; private set; }
    public bool HasVisibleFluidGeometry { get; private set; }

    private Mesh mesh;
    private Mesh fluidMesh;

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
        if (mesh != null)
        {
            mesh.Clear();
        }

        if (fluidMesh != null)
        {
            fluidMesh.Clear();
        }

        HasVisibleGeometry = false;
        HasVisibleFluidGeometry = false;
    }

    public void Dispose()
    {
        if (mesh != null)
        {
            Object.Destroy(mesh);
            mesh = null;
        }

        if (fluidMesh != null)
        {
            Object.Destroy(fluidMesh);
            fluidMesh = null;
        }

        HasVisibleGeometry = false;
        HasVisibleFluidGeometry = false;
    }

    public void ClearSolidMesh()
    {
        if (mesh != null)
        {
            mesh.Clear();
        }

        HasVisibleGeometry = false;
    }

    public void ClearFluidMesh()
    {
        if (fluidMesh != null)
        {
            fluidMesh.Clear();
        }

        HasVisibleFluidGeometry = false;
    }

    public void ApplyMesh(
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles,
        System.Collections.Generic.List<Vector3> normals,
        System.Collections.Generic.List<Vector2> uvs,
        System.Collections.Generic.List<Vector4> tileRects,
        System.Collections.Generic.List<Vector2> tileCounts)
    {
        HasVisibleGeometry = ApplyToMesh(
            ref mesh,
            vertices,
            triangles,
            normals,
            uvs,
            tileRects,
            tileCounts);
    }

    public void ApplyFluidMesh(
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles,
        System.Collections.Generic.List<Vector3> normals,
        System.Collections.Generic.List<Vector2> uvs,
        System.Collections.Generic.List<Vector4> tileRects,
        System.Collections.Generic.List<Vector2> tileCounts)
    {
        HasVisibleFluidGeometry = ApplyToMesh(
            ref fluidMesh,
            vertices,
            triangles,
            normals,
            uvs,
            tileRects,
            tileCounts);
    }

    private static bool ApplyToMesh(
        ref Mesh mesh,
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles,
        System.Collections.Generic.List<Vector3> normals,
        System.Collections.Generic.List<Vector2> uvs,
        System.Collections.Generic.List<Vector4> tileRects,
        System.Collections.Generic.List<Vector2> tileCounts)
    {
        if (vertices.Count == 0 || triangles.Count == 0)
        {
            if (mesh != null)
            {
                mesh.Clear();
            }

            return false;
        }

        if (mesh == null)
        {
            mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
        }
        else
        {
            mesh.Clear();
        }

        mesh.SetVertices(vertices, 0, vertices.Count, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetTriangles(triangles, 0, false);
        mesh.SetNormals(normals, 0, normals.Count, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetUVs(0, uvs, 0, uvs.Count, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetUVs(1, tileRects, 0, tileRects.Count, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetUVs(2, tileCounts, 0, tileCounts.Count, MeshUpdateFlags.DontRecalculateBounds);
        mesh.RecalculateBounds();
        return true;
    }
}
