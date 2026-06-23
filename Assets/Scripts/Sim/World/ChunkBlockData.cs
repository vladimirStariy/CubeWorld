using System;
using UnityEngine;

public sealed class ChunkBlockData
{
    private readonly VoxelBlockType[] blocks;
    private readonly int size;

    public ChunkBlockData(int size, Vector3Int coord)
    {
        this.size = size;
        Coord = coord;
        blocks = new VoxelBlockType[size * size * size];
    }

    public Vector3Int Coord { get; }

    public VoxelBlockType GetBlock(Vector3Int localPos)
    {
        return blocks[ToIndex(localPos.x, localPos.y, localPos.z)];
    }

    public void SetBlock(Vector3Int localPos, VoxelBlockType blockType)
    {
        blocks[ToIndex(localPos.x, localPos.y, localPos.z)] = blockType;
    }

    public VoxelBlockType[] CopyBlocksToArray()
    {
        var copy = new VoxelBlockType[blocks.Length];
        Array.Copy(blocks, copy, blocks.Length);
        return copy;
    }

    public void CopyBlocksFrom(VoxelBlockType[] source)
    {
        if (source == null || source.Length != blocks.Length)
        {
            return;
        }

        Array.Copy(source, blocks, blocks.Length);
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != VoxelBlockType.Air)
            {
                return false;
            }
        }

        return true;
    }

    private int ToIndex(int x, int y, int z)
    {
        return x + size * (y + size * z);
    }
}
