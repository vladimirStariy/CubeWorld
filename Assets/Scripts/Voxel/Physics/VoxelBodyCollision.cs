using UnityEngine;

public static class VoxelBodyCollision
{
    private const float Skin = 0.032f;
    private const float GroundProbe = 0.08f;

    public static void ResolveMovement(
        IVoxelBlockView world,
        Vector3 position,
        float radius,
        float height,
        Vector3 delta,
        out Vector3 resolvedPosition,
        out bool grounded)
    {
        resolvedPosition = position;

        resolvedPosition.y += delta.y;
        resolvedPosition = Depenetrate(world, resolvedPosition, radius, height);

        resolvedPosition.x += delta.x;
        resolvedPosition = Depenetrate(world, resolvedPosition, radius, height);

        resolvedPosition.z += delta.z;
        resolvedPosition = Depenetrate(world, resolvedPosition, radius, height);

        grounded = IsGrounded(world, resolvedPosition, radius, height);
    }

    public static bool HasGroundSupport(IVoxelBlockView world, Vector3 position, float radius, Vector3 moveDirection, float edgeOverhang)
    {
        var footY = position.y;
        var side = Vector3.Cross(Vector3.up, moveDirection).normalized;
        if (side.sqrMagnitude <= 0.001f)
        {
            side = Vector3.right;
        }

        var centerProbe = new Vector3(position.x, footY, position.z) - moveDirection * edgeOverhang;
        var probes = new[]
        {
            centerProbe,
            centerProbe + side * radius,
            centerProbe - side * radius,
            new Vector3(position.x, footY, position.z)
        };

        for (int i = 0; i < probes.Length; i++)
        {
            if (IsGroundedAt(world, probes[i].x, probes[i].y, probes[i].z, radius))
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 Depenetrate(IVoxelBlockView world, Vector3 position, float radius, float height)
    {
        GetPlayerBounds(position, radius, height, out var min, out var max);
        var blockMin = new Vector3Int(
            VoxelConstants.WorldAxisToBlockIndex(min.x) - 1,
            VoxelConstants.WorldAxisToBlockIndex(min.y) - 1,
            VoxelConstants.WorldAxisToBlockIndex(min.z) - 1);
        var blockMax = new Vector3Int(
            VoxelConstants.WorldAxisToBlockIndex(max.x) + 1,
            VoxelConstants.WorldAxisToBlockIndex(max.y) + 1,
            VoxelConstants.WorldAxisToBlockIndex(max.z) + 1);

        for (int y = blockMin.y; y <= blockMax.y; y++)
        {
            for (int z = blockMin.z; z <= blockMax.z; z++)
            {
                for (int x = blockMin.x; x <= blockMax.x; x++)
                {
                    var blockPos = new Vector3Int(x, y, z);
                    if (!IsSolid(world, blockPos))
                    {
                        continue;
                    }

                    VoxelConstants.GetBlockBounds(blockPos, out var blockMinCorner, out var blockMaxCorner);
                    if (!Overlaps(min, max, blockMinCorner, blockMaxCorner))
                    {
                        continue;
                    }

                    position = PushOut(min, max, blockMinCorner, blockMaxCorner, position, radius, height);
                    GetPlayerBounds(position, radius, height, out min, out max);
                }
            }
        }

        return position;
    }

    private static Vector3 PushOut(
        Vector3 playerMin,
        Vector3 playerMax,
        Vector3 blockMin,
        Vector3 blockMax,
        Vector3 position,
        float radius,
        float height)
    {
        var overlapX1 = playerMax.x - blockMin.x;
        var overlapX2 = blockMax.x - playerMin.x;
        var overlapY1 = playerMax.y - blockMin.y;
        var overlapY2 = blockMax.y - playerMin.y;
        var overlapZ1 = playerMax.z - blockMin.z;
        var overlapZ2 = blockMax.z - playerMin.z;

        var minOverlap = overlapX1;
        var axis = 0;
        var sign = -1f;

        if (overlapX2 < minOverlap) { minOverlap = overlapX2; axis = 0; sign = 1f; }
        if (overlapY1 < minOverlap) { minOverlap = overlapY1; axis = 1; sign = -1f; }
        if (overlapY2 < minOverlap) { minOverlap = overlapY2; axis = 1; sign = 1f; }
        if (overlapZ1 < minOverlap) { minOverlap = overlapZ1; axis = 2; sign = -1f; }
        if (overlapZ2 < minOverlap) { minOverlap = overlapZ2; axis = 2; sign = 1f; }

        if (axis == 0)
        {
            position.x += sign * minOverlap;
        }
        else if (axis == 1)
        {
            position.y += sign * minOverlap;
        }
        else
        {
            position.z += sign * minOverlap;
        }

        return position;
    }

    private static bool IsGrounded(IVoxelBlockView world, Vector3 position, float radius, float height)
    {
        var samples = new[]
        {
            new Vector3(position.x, position.y, position.z),
            new Vector3(position.x - radius * 0.65f, position.y, position.z),
            new Vector3(position.x + radius * 0.65f, position.y, position.z),
            new Vector3(position.x, position.y, position.z - radius * 0.65f),
            new Vector3(position.x, position.y, position.z + radius * 0.65f)
        };

        for (int i = 0; i < samples.Length; i++)
        {
            if (IsGroundedAt(world, samples[i].x, samples[i].y, samples[i].z, radius))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGroundedAt(IVoxelBlockView world, float x, float y, float z, float radius)
    {
        var footBlockY = VoxelConstants.WorldAxisToBlockIndex(y - GroundProbe);
        var minX = VoxelConstants.WorldAxisToBlockIndex(x - radius + Skin);
        var maxX = VoxelConstants.WorldAxisToBlockIndex(x + radius - Skin);
        var minZ = VoxelConstants.WorldAxisToBlockIndex(z - radius + Skin);
        var maxZ = VoxelConstants.WorldAxisToBlockIndex(z + radius - Skin);

        for (int bx = minX; bx <= maxX; bx++)
        {
            for (int bz = minZ; bz <= maxZ; bz++)
            {
                if (IsSolid(world, new Vector3Int(bx, footBlockY, bz)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSolid(IVoxelBlockView world, Vector3Int blockPos)
    {
        return world.IsInWorld(blockPos) && world.IsBlockOccupied(blockPos);
    }

    private static void GetPlayerBounds(Vector3 position, float radius, float height, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(position.x - radius + Skin, position.y + Skin, position.z - radius + Skin);
        max = new Vector3(position.x + radius - Skin, position.y + height - Skin, position.z + radius - Skin);
    }

    private static bool Overlaps(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        return aMin.x < bMax.x && aMax.x > bMin.x
            && aMin.y < bMax.y && aMax.y > bMin.y
            && aMin.z < bMax.z && aMax.z > bMin.z;
    }
}
