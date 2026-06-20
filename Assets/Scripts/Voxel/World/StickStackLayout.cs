using System.Collections.Generic;
using UnityEngine;

public readonly struct StickStackPose
{
    public readonly Vector3 LocalPosition;
    public readonly Quaternion LocalRotation;

    public StickStackPose(Vector3 localPosition, Quaternion localRotation)
    {
        LocalPosition = localPosition;
        LocalRotation = localRotation;
    }
}

public static class StickStackLayout
{
    public const int Capacity = 42;
    public const int SticksPerModel = 2;
    public const int ShiftPickupAmount = 4;
    public const int MaxVisualModels = 21;
    public const int LayerCount = 6;

    private const float BlockHalfExtent = 0.5f;
    private const float OutlinePadding = 0.02f;
    private const float StickThickness = ItemPreviewMeshBuilder.GroundStickThickness;

    // Visual stick models per layer, bottom to top: 6 + 5 + 4 + 3 + 2 + 1 = 21 (= 42 real sticks).
    private static readonly int[] LayerVisualCounts = { 6, 5, 4, 3, 2, 1 };
    private static readonly float[] LayerSpreads = { 0.38f, 0.34f, 0.30f, 0.26f, 0.20f, 0f };

    private static readonly StickStackPose[] Poses = CreatePoses();
    private static readonly Quaternion StickRotation = Quaternion.identity;
    private static Mesh bakedFullStackMesh;

    public static int PoseCount => Poses.Length;

    public static int GetVisibleModelCount(int stickCount)
    {
        if (stickCount <= 0)
        {
            return 0;
        }

        stickCount = Mathf.Min(stickCount, Capacity);
        var remaining = stickCount;
        var models = 0;

        for (int layer = 0; layer < LayerCount && remaining > 0; layer++)
        {
            var layerStickCapacity = LayerVisualCounts[layer] * SticksPerModel;
            var sticksInLayer = Mathf.Min(remaining, layerStickCapacity);
            models += (sticksInLayer + SticksPerModel - 1) / SticksPerModel;
            remaining -= sticksInLayer;
        }

        return models;
    }

    public static float GetStackHeight(int stickCount)
    {
        if (stickCount <= 0)
        {
            return 0f;
        }

        var visibleModels = GetVisibleModelCount(stickCount);
        if (visibleModels <= 0)
        {
            return 0f;
        }

        GetPose(visibleModels - 1, out var topPosition, out _);
        return topPosition.y + StickThickness + OutlinePadding;
    }

    public static void GetPose(int index, out Vector3 localPosition, out Quaternion localRotation)
    {
        if (index < 0 || index >= Poses.Length)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return;
        }

        var pose = Poses[index];
        localPosition = pose.LocalPosition;
        localRotation = pose.LocalRotation;
    }

    public static void BuildOutlineSegments(int stickCount, Vector3Int faceNormal, List<LineSegment> segments)
    {
        if (stickCount <= 0)
        {
            return;
        }

        GetFacePlaneAxes(faceNormal, out var axisU, out var axisV);
        var faceCenter = (Vector3)faceNormal * BlockHalfExtent;
        var stackHeight = GetStackHeight(stickCount);
        var faceOffset = (Vector3)faceNormal * OutlinePadding;

        var min = faceCenter - axisU * BlockHalfExtent - axisV * BlockHalfExtent + faceOffset;
        var max = faceCenter
                  + axisU * BlockHalfExtent
                  + axisV * BlockHalfExtent
                  + (Vector3)faceNormal * stackHeight
                  + faceOffset;

        AddBoxOutline(min, max, segments);
    }

    public static Mesh GetBakedFullStackMesh()
    {
        if (bakedFullStackMesh != null)
        {
            return bakedFullStackMesh;
        }

        var stickMesh = ItemPreviewMeshBuilder.GetGroundStickMesh();
        if (stickMesh == null)
        {
            return null;
        }

        var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(stickMesh);
        var combines = new CombineInstance[MaxVisualModels];
        for (int i = 0; i < MaxVisualModels; i++)
        {
            GetPose(i, out var position, out var rotation);
            combines[i] = new CombineInstance
            {
                mesh = stickMesh,
                transform = Matrix4x4.TRS(position + Vector3.up * groundOffset, rotation, Vector3.one)
            };
        }

        bakedFullStackMesh = new Mesh { name = "StickStack42" };
        bakedFullStackMesh.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);
        bakedFullStackMesh.RecalculateBounds();
        return bakedFullStackMesh;
    }

    private static void GetFacePlaneAxes(Vector3Int faceNormal, out Vector3 axisU, out Vector3 axisV)
    {
        if (faceNormal == Vector3Int.up)
        {
            axisU = Vector3.right;
            axisV = Vector3.forward;
            return;
        }

        if (faceNormal == Vector3Int.down)
        {
            axisU = Vector3.right;
            axisV = Vector3.forward;
            return;
        }

        if (faceNormal == Vector3Int.right || faceNormal == Vector3Int.left)
        {
            axisU = Vector3.forward;
            axisV = Vector3.up;
            return;
        }

        axisU = Vector3.right;
        axisV = Vector3.up;
    }

    private static void AddBoxOutline(Vector3 min, Vector3 max, List<LineSegment> segments)
    {
        var c000 = new Vector3(min.x, min.y, min.z);
        var c100 = new Vector3(max.x, min.y, min.z);
        var c010 = new Vector3(min.x, max.y, min.z);
        var c110 = new Vector3(max.x, max.y, min.z);
        var c001 = new Vector3(min.x, min.y, max.z);
        var c101 = new Vector3(max.x, min.y, max.z);
        var c011 = new Vector3(min.x, max.y, max.z);
        var c111 = new Vector3(max.x, max.y, max.z);

        segments.Add(new LineSegment(c000, c100));
        segments.Add(new LineSegment(c100, c101));
        segments.Add(new LineSegment(c101, c001));
        segments.Add(new LineSegment(c001, c000));

        segments.Add(new LineSegment(c010, c110));
        segments.Add(new LineSegment(c110, c111));
        segments.Add(new LineSegment(c111, c011));
        segments.Add(new LineSegment(c011, c010));

        segments.Add(new LineSegment(c000, c010));
        segments.Add(new LineSegment(c100, c110));
        segments.Add(new LineSegment(c101, c111));
        segments.Add(new LineSegment(c001, c011));
    }

    private static StickStackPose[] CreatePoses()
    {
        var poses = new StickStackPose[MaxVisualModels];
        var index = 0;

        for (int layer = 0; layer < LayerCount; layer++)
        {
            index = WriteParallelRow(
                poses,
                index,
                layerY: layer * StickThickness,
                spread: LayerSpreads[layer],
                count: LayerVisualCounts[layer]);
        }

        return poses;
    }

    private static int WriteParallelRow(
        StickStackPose[] poses,
        int startIndex,
        float layerY,
        float spread,
        int count)
    {
        if (count == 1)
        {
            poses[startIndex] = new StickStackPose(new Vector3(0f, layerY, 0f), StickRotation);
            return startIndex + 1;
        }

        var step = spread * 2f / (count - 1);
        for (int i = 0; i < count; i++)
        {
            var xOffset = -spread + step * i;
            poses[startIndex + i] = new StickStackPose(new Vector3(xOffset, layerY, 0f), StickRotation);
        }

        return startIndex + count;
    }
}
