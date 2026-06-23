using UnityEngine;

public static class VoxelConstants
{
    public static readonly Vector3Int[] NeighborDirs =
    {
        Vector3Int.right, Vector3Int.left, Vector3Int.up,
        Vector3Int.down, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    public static readonly int[] OppositeFace = { 1, 0, 3, 2, 5, 4 };

    public static readonly Vector3[][] FaceVertices =
    {
        new[] { new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f) },
        new[] { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f) },
        new[] { new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f) },
        new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) },
        new[] { new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) },
        new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f) }
    };

    public static int Mod(int value, int modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    public static int NormalToFaceIndex(Vector3 normal)
    {
        var ax = Mathf.Abs(normal.x);
        var ay = Mathf.Abs(normal.y);
        var az = Mathf.Abs(normal.z);
        if (ax >= ay && ax >= az)
        {
            return normal.x > 0f ? 0 : 1;
        }

        if (ay >= az)
        {
            return normal.y > 0f ? 2 : 3;
        }

        return normal.z > 0f ? 4 : 5;
    }

    public static Vector3Int FaceToLocal(int faceIndex, int plane, int u, int v)
    {
        return faceIndex switch
        {
            0 or 1 => new Vector3Int(plane, u, v),
            2 or 3 => new Vector3Int(u, plane, v),
            _ => new Vector3Int(u, v, plane)
        };
    }

    public static Vector3Int MicroFaceToCell(int faceIndex, int plane, int u, int v)
    {
        return faceIndex switch
        {
            0 or 1 => new Vector3Int(plane, u, v),
            2 or 3 => new Vector3Int(u, plane, v),
            _ => new Vector3Int(u, v, plane)
        };
    }

    public static Vector3Int WorldPositionToBlockIndex(Vector3 worldPosition)
    {
        return new Vector3Int(
            WorldAxisToBlockIndex(worldPosition.x),
            WorldAxisToBlockIndex(worldPosition.y),
            WorldAxisToBlockIndex(worldPosition.z));
    }

    public static int WorldAxisToBlockIndex(float axis)
    {
        return Mathf.FloorToInt(axis + 0.5f);
    }

    public static void GetBlockBounds(Vector3Int blockIndex, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(blockIndex.x - 0.5f, blockIndex.y - 0.5f, blockIndex.z - 0.5f);
        max = new Vector3(blockIndex.x + 0.5f, blockIndex.y + 0.5f, blockIndex.z + 0.5f);
    }

    public static float NextCenteredBlockBoundary(float originAxis, int blockIndex, int stepAxis, float directionAxis)
    {
        if (stepAxis > 0)
        {
            return (blockIndex + 0.5f - originAxis) / directionAxis;
        }

        if (stepAxis < 0)
        {
            return (originAxis - (blockIndex - 0.5f)) / -directionAxis;
        }

        return float.PositiveInfinity;
    }
}
