using UnityEngine;

public readonly struct WorldInteractTarget
{
    public readonly Vector3Int BlockPosition;
    public readonly Vector3 FaceNormal;
    public readonly Vector3 Point;
    public readonly bool IsGroundItem;
    public readonly GroundItemSurfaceKey? GroundItemKey;

    public WorldInteractTarget(
        Vector3Int blockPosition,
        Vector3 faceNormal,
        Vector3 point,
        bool isGroundItem,
        GroundItemSurfaceKey? groundItemKey = null)
    {
        BlockPosition = blockPosition;
        FaceNormal = faceNormal;
        Point = point;
        IsGroundItem = isGroundItem;
        GroundItemKey = groundItemKey;
    }
}

public static class BlockWorldTargeting
{
    private static IVoxelBlockView voxelWorld;

    public static void ConfigureVoxelWorld(IVoxelBlockView world)
    {
        voxelWorld = world;
    }

    public static bool TryRaycastBlock(Camera camera, float distance, out RaycastHit hit)
    {
        var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (voxelWorld != null
            && VoxelBlockRaycast.TryCast(voxelWorld, ray, distance, out var blockPosition, out var point, out var normal, out var voxelDistance))
        {
            hit = new RaycastHit
            {
                point = point,
                normal = normal,
                distance = voxelDistance
            };
            return true;
        }

        return Physics.Raycast(ray, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    public static bool TryRaycastInteractTarget(Camera camera, float distance, out WorldInteractTarget target)
    {
        target = default;

        var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        var voxelPoint = default(Vector3);
        var voxelNormal = default(Vector3);
        var voxelBlockPosition = default(Vector3Int);
        var voxelDistance = 0f;
        var hasVoxelHit = voxelWorld != null
            && VoxelBlockRaycast.TryCast(voxelWorld, ray, distance, out voxelBlockPosition, out voxelPoint, out voxelNormal, out voxelDistance);

        var hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var hit = hits[0];
            var marker = hit.collider.GetComponentInParent<GroundItemSurfaceMarker>();
            if (marker != null && (!hasVoxelHit || hit.distance < voxelDistance))
            {
                target = new WorldInteractTarget(
                    marker.FoundationBlock,
                    marker.FaceNormal,
                    hit.point,
                    isGroundItem: true,
                    groundItemKey: marker.Key);
                return true;
            }
        }

        if (hasVoxelHit)
        {
            target = new WorldInteractTarget(
                voxelBlockPosition,
                voxelNormal,
                voxelPoint,
                isGroundItem: false);
            return true;
        }

        if (hits.Length == 0)
        {
            return false;
        }

        var physicsHit = hits[0];
        target = new WorldInteractTarget(
            GetHitBlockPosition(physicsHit),
            physicsHit.normal,
            physicsHit.point,
            isGroundItem: false);
        return true;
    }

    public static Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return GetHitBlockPosition(hit.point, hit.normal);
    }

    private static Vector3Int GetHitBlockPosition(Vector3 point, Vector3 normal)
    {
        var pointInsideHitBlock = point - normal * 0.01f;
        return VoxelConstants.WorldPositionToBlockIndex(pointInsideHitBlock);
    }

    public static Vector3Int GetAdjacentBlockPosition(RaycastHit hit)
    {
        var pointInsideAdjacentBlock = hit.point + hit.normal * 0.01f;
        return VoxelConstants.WorldPositionToBlockIndex(pointInsideAdjacentBlock);
    }

    public static Vector3Int GetAdjacentBlockPosition(WorldInteractTarget target)
    {
        var pointInsideAdjacentBlock = target.Point + target.FaceNormal * 0.01f;
        return VoxelConstants.WorldPositionToBlockIndex(pointInsideAdjacentBlock);
    }

    public static Vector3 WorldToLocalBlockPoint(Vector3 worldPoint, Vector3Int blockCenter)
    {
        var local = worldPoint - (Vector3)blockCenter + new Vector3(0.5f, 0.5f, 0.5f);
        return new Vector3(
            Mathf.Clamp01(local.x),
            Mathf.Clamp01(local.y),
            Mathf.Clamp01(local.z));
    }
}
