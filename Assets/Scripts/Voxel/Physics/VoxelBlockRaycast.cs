using UnityEngine;

public static class VoxelBlockRaycast
{
    public static bool TryCast(
        IVoxelBlockView world,
        Ray ray,
        float maxDistance,
        out Vector3Int blockPosition,
        out Vector3 point,
        out Vector3 normal,
        out float distance)
    {
        blockPosition = default;
        point = default;
        normal = Vector3.zero;
        distance = 0f;

        if (world == null || maxDistance <= 0f)
        {
            return false;
        }

        var origin = ray.origin;
        var direction = ray.direction;

        var x = VoxelConstants.WorldAxisToBlockIndex(origin.x);
        var y = VoxelConstants.WorldAxisToBlockIndex(origin.y);
        var z = VoxelConstants.WorldAxisToBlockIndex(origin.z);

        if (IsSolid(world, x, y, z))
        {
            blockPosition = new Vector3Int(x, y, z);
            point = origin;
            normal = -direction.normalized;
            distance = 0f;
            return true;
        }

        var stepX = direction.x > 0f ? 1 : direction.x < 0f ? -1 : 0;
        var stepY = direction.y > 0f ? 1 : direction.y < 0f ? -1 : 0;
        var stepZ = direction.z > 0f ? 1 : direction.z < 0f ? -1 : 0;

        var tDeltaX = stepX != 0 ? Mathf.Abs(1f / direction.x) : float.PositiveInfinity;
        var tDeltaY = stepY != 0 ? Mathf.Abs(1f / direction.y) : float.PositiveInfinity;
        var tDeltaZ = stepZ != 0 ? Mathf.Abs(1f / direction.z) : float.PositiveInfinity;

        var tMaxX = VoxelConstants.NextCenteredBlockBoundary(origin.x, x, stepX, direction.x);
        var tMaxY = VoxelConstants.NextCenteredBlockBoundary(origin.y, y, stepY, direction.y);
        var tMaxZ = VoxelConstants.NextCenteredBlockBoundary(origin.z, z, stepZ, direction.z);

        var lastNormal = Vector3.zero;
        const int maxSteps = 512;

        for (int i = 0; i < maxSteps; i++)
        {
            float t;
            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    x += stepX;
                    lastNormal = new Vector3(-stepX, 0f, 0f);
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    z += stepZ;
                    lastNormal = new Vector3(0f, 0f, -stepZ);
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    y += stepY;
                    lastNormal = new Vector3(0f, -stepY, 0f);
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    z += stepZ;
                    lastNormal = new Vector3(0f, 0f, -stepZ);
                }
            }

            if (t > maxDistance)
            {
                return false;
            }

            if (IsSolid(world, x, y, z))
            {
                blockPosition = new Vector3Int(x, y, z);
                point = origin + direction * t;
                normal = lastNormal;
                distance = t;
                return true;
            }
        }

        return false;
    }

    private static bool IsSolid(IVoxelBlockView world, int x, int y, int z)
    {
        var blockPos = new Vector3Int(x, y, z);
        return world.IsInWorld(blockPos) && world.IsBlockOccupied(blockPos);
    }
}
