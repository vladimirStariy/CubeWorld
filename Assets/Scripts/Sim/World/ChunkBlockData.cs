using System;
using UnityEngine;

public sealed class ChunkBlockData
{
    private readonly VoxelBlockType[] blocks;
    private readonly FluidCell[] fluids;
    private readonly int size;

    public ChunkBlockData(int size, Vector3Int coord)
    {
        this.size = size;
        Coord = coord;
        var volume = size * size * size;
        blocks = new VoxelBlockType[volume];
        fluids = new FluidCell[volume];
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

    public FluidCell GetFluid(Vector3Int localPos)
    {
        return fluids[ToIndex(localPos.x, localPos.y, localPos.z)];
    }

    public void SetFluid(Vector3Int localPos, FluidCell fluid)
    {
        fluids[ToIndex(localPos.x, localPos.y, localPos.z)] = fluid;
    }

    public VoxelBlockType[] CopyBlocksToArray()
    {
        var copy = new VoxelBlockType[blocks.Length];
        Array.Copy(blocks, copy, blocks.Length);
        return copy;
    }

    public FluidCell[] CopyFluidsToArray()
    {
        var copy = new FluidCell[fluids.Length];
        Array.Copy(fluids, copy, fluids.Length);
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

    public void CopyFluidsFrom(FluidCell[] source)
    {
        if (source == null || source.Length != fluids.Length)
        {
            return;
        }

        Array.Copy(source, fluids, fluids.Length);
    }

    public bool IsSolidEmpty()
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

    public bool IsFluidEmpty()
    {
        for (int i = 0; i < fluids.Length; i++)
        {
            if (!fluids[i].IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasRenderableContent()
    {
        return !IsSolidEmpty() || !IsFluidEmpty();
    }

    public bool IsEmpty()
    {
        return !HasRenderableContent();
    }

    private int ToIndex(int x, int y, int z)
    {
        return x + size * (y + size * z);
    }
}
