using System.Collections.Generic;
using UnityEngine;

public static class GroundStackMath
{
    private const float OutlinePadding = 0.02f;

    public static int GetItemModelCount(int stackSize, GroundItemPlacementProfile profile)
    {
        if (stackSize <= 0)
        {
            return 0;
        }

        var itemsPerModel = profile.ItemsPerModel > 0 ? profile.ItemsPerModel : 1;
        var capped = Mathf.Min(stackSize, profile.MaxStackPerSlot);
        return Mathf.CeilToInt(capped / (float)itemsPerModel);
    }

    public static int GetQuantityElements(int stackSize, GroundItemPlacementProfile profile)
    {
        if (!profile.HasStackingShape || stackSize <= 0)
        {
            return 0;
        }

        var cuboidsPerModel = profile.CuboidsPerModel > 0 ? profile.CuboidsPerModel : 1;
        return cuboidsPerModel * GetItemModelCount(stackSize, profile);
    }

    public static float GetStackHeight(int stackSize, GroundItemPlacementProfile profile)
    {
        if (stackSize <= 0)
        {
            return 0f;
        }

        if (profile.HasStackingShape
            && ItemShapeRegistry.Active != null
            && ItemShapeRegistry.Active.TryGet(profile.StackingShapeId, out var definition))
        {
            var quantity = GetQuantityElements(stackSize, profile);
            var height = ItemShapeMeshBuilder.GetGroundStackHeight(definition, quantity);
            return height > 0f ? height + OutlinePadding : 0f;
        }

        if (profile.CbScaleYByLayer > 0f)
        {
            return Mathf.Max(profile.CbScaleYByLayer * stackSize, 0.08f);
        }

        return 0.08f;
    }

    public static void BuildOutlineSegments(
        int stackSize,
        GroundItemPlacementProfile profile,
        Vector3Int faceNormal,
        List<LineSegment> segments)
    {
        segments.Clear();
        if (stackSize <= 0)
        {
            return;
        }

        var height = GetStackHeight(stackSize, profile);
        if (height <= 0f)
        {
            return;
        }

        const float blockHalfExtent = 0.5f;
        GetFacePlaneAxes(faceNormal, out var axisU, out var axisV);
        var faceCenter = (Vector3)faceNormal * blockHalfExtent;
        var faceOffset = (Vector3)faceNormal * OutlinePadding;

        var min = faceCenter - axisU * blockHalfExtent - axisV * blockHalfExtent + faceOffset;
        var max = faceCenter
                  + axisU * blockHalfExtent
                  + axisV * blockHalfExtent
                  + (Vector3)faceNormal * height
                  + faceOffset;

        AddBoxOutline(min, max, segments);
    }

    private static void GetFacePlaneAxes(Vector3Int faceNormal, out Vector3 axisU, out Vector3 axisV)
    {
        if (faceNormal == Vector3Int.up || faceNormal == Vector3Int.down)
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
}
