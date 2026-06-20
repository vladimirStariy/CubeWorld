using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ClayFormingWorldVisualizer : MonoBehaviour
{
    private const float OutlineWidthPixels = 2.5f;

    private static readonly Color LayerClayColor = new(0.42f, 0.72f, 0.94f, 1f);
    private static readonly Color AddOutlineColor = new(0.25f, 0.92f, 0.32f, 0.95f);
    private static readonly Color RemoveOutlineColor = new(0.98f, 0.58f, 0.12f, 0.95f);

    private Transform visualsRoot;
    private readonly List<GameObject> spawned = new();
    private readonly List<LineSegment> addOutlineSegments = new();
    private readonly List<LineSegment> removeOutlineSegments = new();
    private Material layerClayMaterial;
    private bool configured;

    private float CellHalfExtent => ClayFormingConstants.VoxelSize * 0.5f;

    public void Configure(Transform parent)
    {
        if (configured)
        {
            return;
        }

        visualsRoot = new GameObject("ClayFormingVisuals").transform;
        visualsRoot.SetParent(parent, false);
        layerClayMaterial = CreateSolidMaterial(LayerClayColor);
        configured = true;
    }

    public void SyncAll(IReadOnlyList<ClayWorksiteSnapshot> snapshots)
    {
        Clear();
        if (!configured || snapshots == null)
        {
            return;
        }

        addOutlineSegments.Clear();
        removeOutlineSegments.Clear();

        for (int i = 0; i < snapshots.Count; i++)
        {
            RenderWorksite(snapshots[i]);
        }

        FlushOutlineBatch("ClayAddOutlines", addOutlineSegments, AddOutlineColor);
        FlushOutlineBatch("ClayRemoveOutlines", removeOutlineSegments, RemoveOutlineColor);
    }

    private void RenderWorksite(ClayWorksiteSnapshot snapshot)
    {
        if (!snapshot.HasSession || snapshot.Session == null)
        {
            RenderSolidLayer(snapshot.Key.AnchorBlock, snapshot.Key.FaceNormal, 0, snapshot.BaseLayer);
            return;
        }

        var session = snapshot.Session;
        var completed = session.CompletedStages;
        for (int stage = 0; stage < completed.Count; stage++)
        {
            RenderSolidLayer(
                session.AnchorBlock,
                session.FaceNormal,
                session.CompletedWorldLayer(stage),
                completed[stage]);
        }

        RenderActiveStage(session);
    }

    private void RenderSolidLayer(Vector3Int anchor, Vector3Int faceNormal, int layer, bool[,] solid)
    {
        for (int v = 0; v < ClayFormingConstants.GridSize; v++)
        {
            for (int u = 0; u < ClayFormingConstants.GridSize; u++)
            {
                if (!solid[u, v])
                {
                    continue;
                }

                var center = ClayFormingCoordinates.GetVoxelCenterWorld(anchor, faceNormal, layer, u, v);
                SpawnSolidCube(center);
            }
        }
    }

    private void RenderActiveStage(ClayFormingSession session)
    {
        var layer = session.CurrentWorldLayer;
        for (int v = 0; v < ClayFormingConstants.GridSize; v++)
        {
            for (int u = 0; u < ClayFormingConstants.GridSize; u++)
            {
                var center = ClayFormingCoordinates.GetVoxelCenterWorld(
                    session.AnchorBlock,
                    session.FaceNormal,
                    layer,
                    u,
                    v);

                var state = session.GetCellState(u, v);
                if (state == ClayFormingCellState.RemoveTarget)
                {
                    SpawnSolidCube(center);
                    ClayFormingOutlineMesh.AddCellCubeOutline(center, CellHalfExtent, removeOutlineSegments);
                    continue;
                }

                if (session.PlayerLayer[u, v])
                {
                    SpawnSolidCube(center);
                    continue;
                }

                if (state == ClayFormingCellState.AddTarget)
                {
                    ClayFormingOutlineMesh.AddCellCubeOutline(center, CellHalfExtent, addOutlineSegments);
                }
            }
        }
    }

    private void FlushOutlineBatch(string name, List<LineSegment> segments, Color color)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var localSegments = new List<LineSegment>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            localSegments.Add(new LineSegment(
                visualsRoot.InverseTransformPoint(segment.From),
                visualsRoot.InverseTransformPoint(segment.To)));
        }

        spawned.Add(ClayFormingOutlineMesh.BuildBatchedOutline(
            visualsRoot,
            name,
            localSegments,
            color,
            OutlineWidthPixels));
    }

    public void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
            {
                Destroy(spawned[i]);
            }
        }

        spawned.Clear();
        addOutlineSegments.Clear();
        removeOutlineSegments.Clear();
    }

    private void SpawnSolidCube(Vector3 center)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(visualsRoot, true);
        cube.transform.position = center;
        cube.transform.localScale = Vector3.one * ClayFormingConstants.VoxelSize;
        cube.GetComponent<Collider>().enabled = false;
        cube.GetComponent<MeshRenderer>().sharedMaterial = layerClayMaterial;
        spawned.Add(cube);
    }

    private static Material CreateSolidMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Surface", 0f);
        material.SetInt("_ZWrite", 1);
        material.renderQueue = (int)RenderQueue.Geometry;
        return material;
    }
}
