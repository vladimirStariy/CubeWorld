using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class BlockSelectionOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float lineWidthPixels = 2f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material outlineMaterial;
    private Mesh selectionMesh;

    private readonly List<Vector3> vertices = new();
    private readonly List<Vector4> lineData = new();
    private readonly List<int> triangles = new();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        outlineMaterial = new Material(Shader.Find("CubeWorld/SelectionOutline"));
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_LineWidth", lineWidthPixels);

        meshRenderer.sharedMaterial = outlineMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        selectionMesh = new Mesh
        {
            name = "SelectionFaceOutline",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        meshFilter.sharedMesh = selectionMesh;
        Hide();
    }

    public void ShowFaceOutline(Vector3 blockPosition, IReadOnlyList<LineSegment> segments)
    {
        transform.position = blockPosition;
        BuildLineMesh(blockPosition, segments);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BuildLineMesh(Vector3 blockPosition, IReadOnlyList<LineSegment> segments)
    {
        vertices.Clear();
        lineData.Clear();
        triangles.Clear();

        for (int i = 0; i < segments.Count; i++)
        {
            var from = segments[i].From - blockPosition;
            var to = segments[i].To - blockPosition;
            AddLineQuad(from, to);
        }

        selectionMesh.Clear();
        selectionMesh.SetVertices(vertices);
        selectionMesh.SetUVs(0, lineData);
        selectionMesh.SetTriangles(triangles, 0);
        selectionMesh.RecalculateBounds();
    }

    private void AddLineQuad(Vector3 from, Vector3 to)
    {
        var baseIndex = vertices.Count;
        AddVertex(from, to, -1f);
        AddVertex(from, to, 1f);
        AddVertex(to, from, 1f);
        AddVertex(to, from, -1f);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);
    }

    private void AddVertex(Vector3 position, Vector3 other, float extrude)
    {
        vertices.Add(position);
        lineData.Add(new Vector4(other.x, other.y, other.z, extrude));
    }
}
