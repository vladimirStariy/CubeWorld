using UnityEngine;

internal static class ChunkGenerationMath
{
    public static int ToIndex(Vector3Int local, int chunkSize)
    {
        return local.x + chunkSize * (local.y + chunkSize * local.z);
    }

    public static Vector3Int WorldToChunk(Vector3Int world, int chunkSize)
    {
        return new Vector3Int(
            FloorDiv(world.x, chunkSize),
            FloorDiv(world.y, chunkSize),
            FloorDiv(world.z, chunkSize));
    }

    public static Vector3Int WorldToLocal(Vector3Int world, int chunkSize)
    {
        return new Vector3Int(
            Mod(world.x, chunkSize),
            Mod(world.y, chunkSize),
            Mod(world.z, chunkSize));
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }

        return (value - divisor + 1) / divisor;
    }

    private static int Mod(int value, int modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
