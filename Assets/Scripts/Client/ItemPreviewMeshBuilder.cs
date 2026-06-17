using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ItemPreviewMeshBuilder
{
    private static readonly (float Yaw, float Pitch)[] StickAngles =
    {
        (0f, 8f),
        (62f, -12f),
        (-58f, 6f)
    };

    private static readonly Dictionary<ItemKind, Mesh> Meshes = new();
    private static readonly Dictionary<int, Mesh> SingleStickMeshes = new();

    public static bool SupportsPreview(ItemKind kind)
    {
        return kind is ItemKind.GrassBundle or ItemKind.Stick or ItemKind.Flint;
    }

    public static Mesh GetMesh(ItemKind kind)
    {
        if (Meshes.TryGetValue(kind, out var cached) && cached != null)
        {
            return cached;
        }

        cached = kind switch
        {
            ItemKind.GrassBundle => BuildGrassBundleMesh(),
            ItemKind.Stick => BuildStickMesh(),
            ItemKind.Flint => BuildFlintMesh(),
            _ => null
        };

        if (cached != null)
        {
            Meshes[kind] = cached;
        }

        return cached;
    }

    public static Mesh GetSingleStickMesh(int stickIndex)
    {
        if (stickIndex < 0 || stickIndex >= StickAngles.Length)
        {
            return null;
        }

        if (SingleStickMeshes.TryGetValue(stickIndex, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        var angles = StickAngles[stickIndex];
        AddStick(vertices, triangles, normals, angles.Yaw, angles.Pitch);
        cached = CreateMesh(vertices, triangles, normals);
        SingleStickMeshes[stickIndex] = cached;
        return cached;
    }

    public static float GetMeshGroundOffset(Mesh mesh)
    {
        return mesh != null ? -mesh.bounds.min.y : 0f;
    }

    public static Color GetPreviewColor(ItemKind kind)
    {
        return kind switch
        {
            ItemKind.GrassBundle => new Color(0.38f, 0.72f, 0.28f),
            ItemKind.Stick => new Color(0.62f, 0.42f, 0.22f),
            ItemKind.Flint => new Color(0.55f, 0.58f, 0.62f),
            _ => Color.white
        };
    }

    private static Mesh BuildGrassBundleMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();

        AddBox(vertices, triangles, normals, new Vector3(0f, -0.34f, 0f), new Vector3(0.52f, 0.14f, 0.52f));
        AddBox(vertices, triangles, normals, new Vector3(0f, -0.24f, 0f), new Vector3(0.36f, 0.1f, 0.36f));

        AddBlade(vertices, triangles, normals, new Vector3(-0.08f, -0.16f, 0.06f), 0.34f, 18f);
        AddBlade(vertices, triangles, normals, new Vector3(0.1f, -0.16f, -0.04f), 0.3f, -24f);
        AddBlade(vertices, triangles, normals, new Vector3(0.02f, -0.16f, 0.12f), 0.28f, 52f);
        AddBlade(vertices, triangles, normals, new Vector3(-0.12f, -0.16f, -0.08f), 0.26f, -58f);

        return CreateMesh(vertices, triangles, normals);
    }

    private static Mesh BuildStickMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();

        for (int i = 0; i < StickAngles.Length; i++)
        {
            var angles = StickAngles[i];
            AddStick(vertices, triangles, normals, angles.Yaw, angles.Pitch);
        }

        return CreateMesh(vertices, triangles, normals);
    }

    private static Mesh BuildFlintMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();

        AddBox(vertices, triangles, normals, new Vector3(0f, -0.18f, 0f), new Vector3(0.28f, 0.18f, 0.34f), Quaternion.Euler(12f, 24f, -8f));
        AddBox(vertices, triangles, normals, new Vector3(0.06f, -0.28f, -0.04f), new Vector3(0.2f, 0.12f, 0.22f), Quaternion.Euler(-6f, -18f, 14f));

        return CreateMesh(vertices, triangles, normals);
    }

    private static void AddStick(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, float yawDegrees, float pitchDegrees)
    {
        var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        var halfLength = 0.28f;
        var thickness = 0.045f;
        var center = rotation * new Vector3(0f, 0f, 0f);

        AddBox(
            vertices,
            triangles,
            normals,
            center,
            new Vector3(thickness, thickness, halfLength * 2f),
            rotation);
    }

    private static void AddBlade(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 baseCenter,
        float height,
        float yawDegrees)
    {
        var rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        var right = rotation * Vector3.right * 0.04f;
        var up = Vector3.up * height;
        var forward = rotation * Vector3.forward * 0.02f;

        var bottomCenter = baseCenter;
        var topCenter = baseCenter + up * 0.5f + forward;

        AddQuad(
            vertices,
            triangles,
            normals,
            rotation * Vector3.right,
            bottomCenter - right,
            bottomCenter + right,
            topCenter + right,
            topCenter - right);
    }

    private static void AddBox(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 center,
        Vector3 size,
        Quaternion? rotation = null)
    {
        var rot = rotation ?? Quaternion.identity;
        var half = size * 0.5f;

        var corners = new Vector3[8];
        corners[0] = center + rot * new Vector3(-half.x, -half.y, -half.z);
        corners[1] = center + rot * new Vector3(half.x, -half.y, -half.z);
        corners[2] = center + rot * new Vector3(half.x, -half.y, half.z);
        corners[3] = center + rot * new Vector3(-half.x, -half.y, half.z);
        corners[4] = center + rot * new Vector3(-half.x, half.y, -half.z);
        corners[5] = center + rot * new Vector3(half.x, half.y, -half.z);
        corners[6] = center + rot * new Vector3(half.x, half.y, half.z);
        corners[7] = center + rot * new Vector3(-half.x, half.y, half.z);

        AddQuad(vertices, triangles, normals, rot * Vector3.up, corners[4], corners[5], corners[6], corners[7]);
        AddQuad(vertices, triangles, normals, rot * Vector3.down, corners[1], corners[0], corners[3], corners[2]);
        AddQuad(vertices, triangles, normals, rot * Vector3.forward, corners[5], corners[1], corners[2], corners[6]);
        AddQuad(vertices, triangles, normals, rot * Vector3.back, corners[0], corners[4], corners[7], corners[3]);
        AddQuad(vertices, triangles, normals, rot * Vector3.right, corners[5], corners[4], corners[0], corners[1]);
        AddQuad(vertices, triangles, normals, rot * Vector3.left, corners[7], corners[6], corners[2], corners[3]);
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 normal,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3)
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
    }

    private static Mesh CreateMesh(List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        var mesh = new Mesh { indexFormat = IndexFormat.UInt16 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();
        return mesh;
    }
}
