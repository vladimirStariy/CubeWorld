using UnityEngine;

public static class GroundItemPlacementMath
{
    public static bool TryCreateSurfaceKey(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        GroundItemPlacementProfile profile,
        out GroundItemSurfaceKey key,
        out string message)
    {
        message = null;
        key = default;

        var normal = Vector3Int.RoundToInt(faceNormal);
        if (normal == Vector3Int.zero)
        {
            message = "Invalid placement surface.";
            return false;
        }

        if (profile.RequiresTopFace && normal != Vector3Int.up)
        {
            message = "This item can only be placed on the top of a block.";
            return false;
        }

        key = new GroundItemSurfaceKey(hitBlock, normal);
        return true;
    }

    public static int ResolveSlotIndex(
        Vector3 worldHitPoint,
        Vector3Int foundationBlock,
        Vector3Int faceNormal,
        GroundPlacementLayout layout)
    {
        if (layout is GroundPlacementLayout.Single or GroundPlacementLayout.Pile or GroundPlacementLayout.Stack)
        {
            return 0;
        }

        if (!TryGetFaceLocalCoords(worldHitPoint, foundationBlock, faceNormal, out var localU, out var localV))
        {
            return 0;
        }

        return layout switch
        {
            GroundPlacementLayout.Dual => localU < 0.5f ? 0 : 1,
            GroundPlacementLayout.Quad => (localU < 0.5f ? 0 : 1) + (localV < 0.5f ? 0 : 2),
            _ => 0
        };
    }

    public static Vector3 GetSlotWorldPosition(
        Vector3Int foundationBlock,
        Vector3Int faceNormal,
        GroundPlacementLayout layout,
        int slotIndex)
    {
        return (Vector3)foundationBlock + GetSlotLocalPosition(faceNormal, layout, slotIndex);
    }

    public static Vector3 GetSlotLocalPosition(
        Vector3Int faceNormal,
        GroundPlacementLayout layout,
        int slotIndex)
    {
        GetFaceAxes(faceNormal, out var axisU, out var axisV);
        var normal = (Vector3)faceNormal;

        var (centerU, centerV) = layout switch
        {
            GroundPlacementLayout.Dual => slotIndex == 0 ? (0.25f, 0.5f) : (0.75f, 0.5f),
            GroundPlacementLayout.Quad => (
                (slotIndex % 2 == 0 ? 0.25f : 0.75f),
                (slotIndex < 2 ? 0.25f : 0.75f)),
            _ => (0.5f, 0.5f)
        };

        return normal * 0.5f
               + axisU * (centerU - 0.5f)
               + axisV * (centerV - 0.5f);
    }

    private static bool TryGetFaceLocalCoords(
        Vector3 worldHitPoint,
        Vector3Int foundationBlock,
        Vector3Int faceNormal,
        out float localU,
        out float localV)
    {
        localU = 0f;
        localV = 0f;

        GetFaceAxes(faceNormal, out var axisU, out var axisV);
        var origin = GetFaceOriginWorld(foundationBlock, faceNormal);
        var offset = worldHitPoint - origin;

        localU = Vector3.Dot(offset, axisU);
        localV = Vector3.Dot(offset, axisV);
        return localU >= 0f && localU <= 1f && localV >= 0f && localV <= 1f;
    }

    private static Vector3 GetFaceOriginWorld(Vector3Int foundationBlock, Vector3Int faceNormal)
    {
        var faceIndex = VoxelConstants.NormalToFaceIndex((Vector3)faceNormal);
        return (Vector3)foundationBlock + VoxelConstants.FaceVertices[faceIndex][0];
    }

    private static void GetFaceAxes(Vector3Int faceNormal, out Vector3 axisU, out Vector3 axisV)
    {
        var faceIndex = VoxelConstants.NormalToFaceIndex((Vector3)faceNormal);
        ClayFormingCoordinates.GetFaceAxes(faceIndex, out axisU, out axisV);
    }
}
