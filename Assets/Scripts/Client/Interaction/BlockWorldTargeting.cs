using UnityEngine;

public static class BlockWorldTargeting
{
    public static bool TryRaycastBlock(Camera camera, float distance, out RaycastHit hit)
    {
        var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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

    public static Vector3 WorldToLocalBlockPoint(Vector3 worldPoint, Vector3Int blockCenter)
    {
        var local = worldPoint - (Vector3)blockCenter + new Vector3(0.5f, 0.5f, 0.5f);
        return new Vector3(
            Mathf.Clamp01(local.x),
            Mathf.Clamp01(local.y),
            Mathf.Clamp01(local.z));
    }
}
