using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ClayFormingOutlineMesh
{
    private static readonly (int From, int To)[] CubeEdges =
    {
        (0, 1), (1, 2), (2, 3), (3, 0),
        (4, 5), (5, 6), (6, 7), (7, 4),
        (0, 4), (1, 5), (2, 6), (3, 7)
    };

    public static void AddCellCubeOutline(Vector3 center, float halfExtent, List<LineSegment> segments)
    {
        var corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfExtent, -halfExtent, -halfExtent);
        corners[1] = center + new Vector3(halfExtent, -halfExtent, -halfExtent);
        corners[2] = center + new Vector3(halfExtent, -halfExtent, halfExtent);
        corners[3] = center + new Vector3(-halfExtent, -halfExtent, halfExtent);
        corners[4] = center + new Vector3(-halfExtent, halfExtent, -halfExtent);
        corners[5] = center + new Vector3(halfExtent, halfExtent, -halfExtent);
        corners[6] = center + new Vector3(halfExtent, halfExtent, halfExtent);
        corners[7] = center + new Vector3(-halfExtent, halfExtent, halfExtent);

        for (int i = 0; i < CubeEdges.Length; i++)
        {
            var edge = CubeEdges[i];
            segments.Add(new LineSegment(corners[edge.From], corners[edge.To]));
        }
    }

    public static GameObject BuildBatchedOutline(
        Transform parent,
        string name,
        IReadOnlyList<LineSegment> segments,
        Color color,
        float lineWidthPixels)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);

        var meshFilter = root.AddComponent<MeshFilter>();
        var meshRenderer = root.AddComponent<MeshRenderer>();
        var mesh = new Mesh
        {
            name = name,
            indexFormat = IndexFormat.UInt32
        };

        BuildLineMesh(mesh, segments);

        var material = new Material(Shader.Find("CubeWorld/ClayFormingOutline"));
        material.SetColor("_OutlineColor", color);
        material.SetFloat("_LineWidth", lineWidthPixels);

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return root;
    }

    private static void BuildLineMesh(Mesh mesh, IReadOnlyList<LineSegment> segments)
    {
        var vertices = new List<Vector3>(segments.Count * 4);
        var lineData = new List<Vector4>(segments.Count * 4);
        var triangles = new List<int>(segments.Count * 6);

        for (int i = 0; i < segments.Count; i++)
        {
            var from = segments[i].From;
            var to = segments[i].To;
            var baseIndex = vertices.Count;
            AddLineVertex(vertices, lineData, from, to, -1f);
            AddLineVertex(vertices, lineData, from, to, 1f);
            AddLineVertex(vertices, lineData, to, from, 1f);
            AddLineVertex(vertices, lineData, to, from, -1f);

            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, lineData);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    private static void AddLineVertex(
        List<Vector3> vertices,
        List<Vector4> lineData,
        Vector3 position,
        Vector3 other,
        float extrude)
    {
        vertices.Add(position);
        lineData.Add(new Vector4(other.x, other.y, other.z, extrude));
    }
}
