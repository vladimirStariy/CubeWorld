using UnityEngine;

public static class WorldItemInteraction
{
    public static bool IsTopFaceHit(Vector3 faceNormal)
    {
        return faceNormal.y > 0.5f;
    }

    public static Vector3Int GetAssemblyAnchor(Vector3Int foundationPosition)
    {
        return foundationPosition + Vector3Int.up;
    }

    public static bool TryResolveAssemblyAnchor(
        Vector3Int clickedBlock,
        Vector3 faceNormal,
        System.Func<Vector3Int, bool> hasAssemblyAt,
        out Vector3Int anchorPosition)
    {
        if (hasAssemblyAt(clickedBlock))
        {
            anchorPosition = clickedBlock;
            return true;
        }

        if (faceNormal.y > 0.5f)
        {
            var above = clickedBlock + Vector3Int.up;
            if (hasAssemblyAt(above))
            {
                anchorPosition = above;
                return true;
            }
        }

        if (faceNormal.y < -0.5f)
        {
            var below = clickedBlock + Vector3Int.down;
            if (hasAssemblyAt(below))
            {
                anchorPosition = below;
                return true;
            }
        }

        anchorPosition = default;
        return false;
    }
}
