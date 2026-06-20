using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class BlockPreviewMeshBuilder
{
    private static readonly Dictionary<VoxelBlockType, Mesh> Meshes = new();

    public static Mesh GetMesh(VoxelBlockType blockType)
    {
        if (Meshes.TryGetValue(blockType, out var cached) && cached != null)
        {
            return cached;
        }

        cached = VoxelBlockShapes.IsBottomSlab(blockType)
            ? BuildBottomSlabMesh(blockType)
            : VoxelBlockShapes.IsCustomMeshBlock(blockType)
                ? BuildCampfireMesh()
            : BuildFullCubeMesh(blockType);
        Meshes[blockType] = cached;
        return cached;
    }

    private static Mesh BuildCampfireMesh()
    {
        var vertices = new List<Vector3>(96);
        var triangles = new List<int>(144);
        var normals = new List<Vector3>(96);
        var uvs = new List<Vector2>(96);
        var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(VoxelBlockType.Dirt, BlockFace.Top);

        AddBox(vertices, triangles, normals, uvs, -0.30f, 0.30f, -0.50f, -0.38f, -0.30f, 0.30f, textureSlot);
        AddBox(vertices, triangles, normals, uvs, -0.42f, 0.42f, -0.34f, -0.22f, -0.10f, 0.10f, textureSlot);
        AddBox(vertices, triangles, normals, uvs, -0.10f, 0.10f, -0.22f, -0.10f, -0.42f, 0.42f, textureSlot);
        AddBox(vertices, triangles, normals, uvs, -0.14f, 0.14f, -0.10f, 0.06f, -0.14f, 0.14f, textureSlot);

        return CreateMesh(vertices, triangles, normals, uvs);
    }

    private static Mesh BuildFullCubeMesh(VoxelBlockType blockType)
    {
        var vertices = new List<Vector3>(24);
        var triangles = new List<int>(36);
        var normals = new List<Vector3>(24);
        var uvs = new List<Vector2>(24);

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(blockType, face);
            AddFullCubeFace(face, Vector3Int.zero, textureSlot, vertices, triangles, normals, uvs);
        }

        return CreateMesh(vertices, triangles, normals, uvs);
    }

    private static Mesh BuildBottomSlabMesh(VoxelBlockType blockType)
    {
        var vertices = new List<Vector3>(24);
        var triangles = new List<int>(36);
        var normals = new List<Vector3>(24);
        var uvs = new List<Vector2>(24);

        var ymin = -0.5f;
        var ymax = 0f;
        var xmin = -0.5f;
        var xmax = 0.5f;
        var zmin = -0.5f;
        var zmax = 0.5f;

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(blockType, face);

            switch (face)
            {
                case 0:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                        textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                    break;
                case 1:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                        textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                    break;
                case 2:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
                        textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
                    break;
                case 3:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
                        textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
                    break;
                case 4:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                        textureSlot, 1f, 0f, 1f, 1f, 0f, 1f, 0f, 0f);
                    break;
                default:
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                        textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                    break;
            }
        }

        return CreateMesh(vertices, triangles, normals, uvs);
    }

    private static void AddFullCubeFace(
        int faceIndex,
        Vector3Int local,
        int textureSlot,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        var x = local.x;
        var y = local.y;
        var z = local.z;
        var xmin = x - 0.5f;
        var xmax = x + 0.5f;
        var ymin = y - 0.5f;
        var ymax = y + 0.5f;
        var zmin = z - 0.5f;
        var zmax = z + 0.5f;

        switch (faceIndex)
        {
            case 0:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                    textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                break;
            case 1:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                    textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                break;
            case 2:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
                    textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
                break;
            case 3:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
                    textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
                break;
            case 4:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                    textureSlot, 1f, 0f, 1f, 1f, 0f, 1f, 0f, 0f);
                break;
            default:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                    textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
                break;
        }
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        Vector3 normal,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        int textureSlot,
        float uv0u, float uv0v,
        float uv1u, float uv1v,
        float uv2u, float uv2v,
        float uv3u, float uv3v)
    {
        var baseIndex = vertices.Count;
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        uvs.Add(BlockTextureLibrary.GetAtlasUv(textureSlot, uv0u, uv0v));
        uvs.Add(BlockTextureLibrary.GetAtlasUv(textureSlot, uv1u, uv1v));
        uvs.Add(BlockTextureLibrary.GetAtlasUv(textureSlot, uv2u, uv2v));
        uvs.Add(BlockTextureLibrary.GetAtlasUv(textureSlot, uv3u, uv3v));
    }

    private static void AddBox(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        float xmin,
        float xmax,
        float ymin,
        float ymax,
        float zmin,
        float zmax,
        int textureSlot)
    {
        AddQuad(vertices, triangles, normals, uvs, Vector3.right,
            new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
            textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
        AddQuad(vertices, triangles, normals, uvs, Vector3.left,
            new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
            textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
        AddQuad(vertices, triangles, normals, uvs, Vector3.up,
            new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
            textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
        AddQuad(vertices, triangles, normals, uvs, Vector3.down,
            new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
            textureSlot, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f);
        AddQuad(vertices, triangles, normals, uvs, Vector3.forward,
            new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
            textureSlot, 1f, 0f, 1f, 1f, 0f, 1f, 0f, 0f);
        AddQuad(vertices, triangles, normals, uvs, Vector3.back,
            new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
            textureSlot, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f);
    }

    private static Mesh CreateMesh(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        var mesh = new Mesh { indexFormat = IndexFormat.UInt16 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        return mesh;
    }
}
