using UnityEngine;

public sealed class ChunkData
{
    private readonly VoxelBlockType[] blocks;
    private readonly int size;

    public Vector3Int Coord { get; }
    public MeshFilter Filter { get; }
    public MeshCollider Collider { get; }

    public ChunkData(int size, Vector3Int coord, MeshFilter filter, MeshCollider collider)
    {
        this.size = size;
        Coord = coord;
        Filter = filter;
        Collider = collider;
        blocks = new VoxelBlockType[size * size * size];
    }

    public VoxelBlockType GetBlock(Vector3Int localPos)
    {
        return blocks[ToIndex(localPos.x, localPos.y, localPos.z)];
    }

    public void SetBlock(Vector3Int localPos, VoxelBlockType blockType)
    {
        blocks[ToIndex(localPos.x, localPos.y, localPos.z)] = blockType;
    }

    private int ToIndex(int x, int y, int z)
    {
        return x + size * (y + size * z);
    }
}
