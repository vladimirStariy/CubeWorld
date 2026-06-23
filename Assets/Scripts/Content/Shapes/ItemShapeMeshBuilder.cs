using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ItemShapeMeshBuilder
{
    private static readonly Dictionary<(ContentId shapeId, int partCount), Mesh> MeshCache = new();

    public static void ClearCache()
    {
        MeshCache.Clear();
        DisplayMeshCache.Clear();
        GroundStackMeshCache.Clear();
        GroundStackBaseMinYCache.Clear();
    }

    public static bool TryGetMesh(ContentId shapeId, int quantityElements, out Mesh mesh)
    {
        return TryGetRenderData(shapeId, quantityElements, ItemKind.None, out mesh, out _);
    }

    public static bool TryGetFullMesh(ContentId shapeId, out Mesh mesh)
    {
        mesh = null;
        if (shapeId.Name == null || ItemShapeRegistry.Active == null
            || !ItemShapeRegistry.Active.TryGet(shapeId, out var definition))
        {
            return false;
        }

        return TryGetRenderData(shapeId, definition.PartCount, ItemKind.None, out mesh, out _);
    }

    public static bool TryGetRenderData(
        ContentId shapeId,
        int quantityElements,
        ItemKind fallbackKind,
        out Mesh mesh,
        out Material material)
    {
        mesh = null;
        material = null;

        if (shapeId.Name == null || ItemShapeRegistry.Active == null
            || !ItemShapeRegistry.Active.TryGet(shapeId, out var definition))
        {
            return false;
        }

        mesh = GetOrCreateMesh(definition, quantityElements);
        if (mesh == null)
        {
            return false;
        }

        material = ItemShapeMaterials.GetForShape(definition, fallbackKind);
        return material != null;
    }

    public static Mesh GetDisplayMesh(Mesh sourceMesh)
    {
        if (sourceMesh == null)
        {
            return null;
        }

        if (DisplayMeshCache.TryGetValue(sourceMesh, out var centered) && centered != null)
        {
            return centered;
        }

        centered = CreateCenteredMesh(sourceMesh);
        DisplayMeshCache[sourceMesh] = centered;
        return centered;
    }

    private static readonly Dictionary<Mesh, Mesh> DisplayMeshCache = new();

    private static Mesh CreateCenteredMesh(Mesh source)
    {
        var centered = Object.Instantiate(source);
        centered.name = source.name + "_display";

        var vertices = centered.vertices;
        var offset = source.bounds.center;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= offset;
        }

        centered.vertices = vertices;
        centered.RecalculateBounds();
        return centered;
    }

    public static float GetGroundOffset(Mesh mesh)
    {
        return mesh != null ? -mesh.bounds.min.y : 0f;
    }

    public static Mesh GetGroundStackMesh(Mesh sourceMesh, ItemShapeDefinition definition = null)
    {
        if (sourceMesh == null)
        {
            return null;
        }

        var groundMinY = ResolveGroundStackMinY(sourceMesh, definition);
        var cacheKey = (sourceMesh, groundMinY);
        if (GroundStackMeshCache.TryGetValue(cacheKey, out var aligned) && aligned != null)
        {
            return aligned;
        }

        aligned = CreateGroundStackMesh(sourceMesh, groundMinY);
        GroundStackMeshCache[cacheKey] = aligned;
        return aligned;
    }

    public static float GetGroundStackHeight(Mesh sourceMesh, ItemShapeDefinition definition = null)
    {
        if (sourceMesh == null)
        {
            return 0f;
        }

        var aligned = GetGroundStackMesh(sourceMesh, definition);
        return aligned != null && aligned.bounds.size.y > 0f ? aligned.bounds.max.y : 0f;
    }

    public static float GetGroundStackHeight(ItemShapeDefinition definition, int quantityElements)
    {
        if (definition == null || quantityElements <= 0)
        {
            return 0f;
        }

        var bounds = GetPartialBounds(definition, quantityElements);
        if (bounds.size.y <= 0f)
        {
            return 0f;
        }

        var groundMinY = GetGroundStackBaseMinY(definition);
        return Mathf.Max(bounds.max.y - groundMinY, 0f);
    }

    private static readonly Dictionary<(Mesh mesh, float groundMinY), Mesh> GroundStackMeshCache = new();
    private static readonly Dictionary<ContentId, float> GroundStackBaseMinYCache = new();

    private static float GetGroundStackBaseMinY(ItemShapeDefinition definition)
    {
        if (definition == null)
        {
            return ItemShapeJsonValues.EditorSpaceToUnity(Vector3.zero).y;
        }

        if (GroundStackBaseMinYCache.TryGetValue(definition.Id, out var cached))
        {
            return cached;
        }

        // Ground pile shapes are authored on the editor Y=0 plane (block top face).
        cached = ItemShapeJsonValues.EditorSpaceToUnity(Vector3.zero).y;
        GroundStackBaseMinYCache[definition.Id] = cached;
        return cached;
    }

    private static float ResolveGroundStackMinY(Mesh sourceMesh, ItemShapeDefinition definition)
    {
        return definition != null ? GetGroundStackBaseMinY(definition) : sourceMesh.bounds.min.y;
    }

    private static Mesh CreateGroundStackMesh(Mesh source, float groundMinY)
    {
        var aligned = Object.Instantiate(source);
        aligned.name = source.name + "_ground";

        var offset = new Vector3(0f, groundMinY, 0f);
        var vertices = aligned.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= offset;
        }

        aligned.vertices = vertices;
        aligned.RecalculateBounds();
        return aligned;
    }

    public static Bounds GetPartialBounds(ItemShapeDefinition definition, int quantityElements)
    {
        if (definition == null || definition.PartCount == 0)
        {
            return default;
        }

        var count = Mathf.Clamp(quantityElements, 0, definition.PartCount);
        if (count <= 0)
        {
            return default;
        }

        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < count; i++)
        {
            var faces = definition.Parts[i].Faces;
            for (int f = 0; f < faces.Count; f++)
            {
                var corners = faces[f].Corners;
                for (int c = 0; c < corners.Length; c++)
                {
                    min = Vector3.Min(min, corners[c]);
                    max = Vector3.Max(max, corners[c]);
                }
            }
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    private static Mesh GetOrCreateMesh(ItemShapeDefinition definition, int quantityElements)
    {
        var clamped = Mathf.Clamp(quantityElements, 0, definition.PartCount);
        if (clamped <= 0)
        {
            return null;
        }

        var cacheKey = (definition.Id, clamped);
        if (MeshCache.TryGetValue(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        for (int i = 0; i < clamped; i++)
        {
            AddPart(vertices, triangles, normals, uvs, definition.Parts[i]);
        }

        var mesh = new Mesh
        {
            name = $"{definition.Id.Name}_{clamped}",
            indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        MeshCache[cacheKey] = mesh;
        return mesh;
    }

    private static void AddPart(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        ItemShapePart part)
    {
        for (int f = 0; f < part.Faces.Count; f++)
        {
            AddFace(vertices, triangles, normals, uvs, part.Faces[f]);
        }
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        ItemShapeFace face)
    {
        var normal = Vector3.Normalize(Vector3.Cross(face.Corners[1] - face.Corners[0], face.Corners[2] - face.Corners[0]));
        if (normal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var baseIndex = vertices.Count;
        for (int i = 0; i < 4; i++)
        {
            vertices.Add(face.Corners[i]);
            normals.Add(normal);
            uvs.Add(face.Uvs[i]);
        }

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 3);
        triangles.Add(baseIndex + 2);
    }
}
