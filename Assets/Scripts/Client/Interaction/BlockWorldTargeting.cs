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
    public static bool TryRaycastBlock(Camera camera, float distance, out RaycastHit hit)
    {
        var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    public static bool TryRaycastInteractTarget(Camera camera, float distance, out WorldInteractTarget target)
    {
        target = default;

        var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        var hit = hits[0];

        var marker = hit.collider.GetComponentInParent<GroundItemSurfaceMarker>();
        if (marker != null)
        {
            target = new WorldInteractTarget(
                marker.FoundationBlock,
                marker.FaceNormal,
                hit.point,
                isGroundItem: true,
                groundItemKey: marker.Key);
            return true;
        }

        target = new WorldInteractTarget(
            GetHitBlockPosition(hit),
            hit.normal,
            hit.point,
            isGroundItem: false);
        return true;
    }

    public static Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        var pointInsideHitBlock = hit.point - hit.normal * 0.01f;
        return Vector3Int.RoundToInt(pointInsideHitBlock);
    }

    public static Vector3Int GetAdjacentBlockPosition(RaycastHit hit)
    {
        var pointInsideAdjacentBlock = hit.point + hit.normal * 0.01f;
        return Vector3Int.RoundToInt(pointInsideAdjacentBlock);
    }

    public static Vector3Int GetAdjacentBlockPosition(WorldInteractTarget target)
    {
        var pointInsideAdjacentBlock = target.Point + target.FaceNormal * 0.01f;
        return Vector3Int.RoundToInt(pointInsideAdjacentBlock);
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
