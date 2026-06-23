using System.Collections.Generic;
using UnityEngine;

internal sealed class DetachedChunkMeshView : IVoxelBlockView
{
    private readonly ChunkMeshBuildSnapshot snapshot;

    public DetachedChunkMeshView(ChunkMeshBuildSnapshot snapshot)
    {
        this.snapshot = snapshot;
    }

    public int ChunkSize => snapshot.ChunkSize;

    public int ChiselResolution => snapshot.ChiselResolution;

    public bool IsInWorld(Vector3Int position)
    {
        return position.y >= snapshot.MinWorldY && position.y < snapshot.WorldHeight;
    }

    public VoxelBlockType GetBlock(Vector3Int position)
    {
        if (TryGetChunkBlock(position, out var blockType))
        {
            return blockType;
        }

        if (snapshot.ExternalBlocks.TryGetValue(position, out blockType))
        {
            return blockType;
        }

        return VoxelBlockType.Air;
    }

    public bool IsBlockOccupied(Vector3Int position)
    {
        return GetBlock(position) != VoxelBlockType.Air || snapshot.ChiseledBlocks.ContainsKey(position);
    }

    public bool IsFullCubeBlock(Vector3Int position)
    {
        if (snapshot.ChiseledBlocks.ContainsKey(position))
        {
            return false;
        }

        return VoxelBlockShapes.IsFullCube(GetBlock(position));
    }

    public bool HasChiseledBlock(Vector3Int position)
    {
        return snapshot.ChiseledBlocks.ContainsKey(position);
    }

    public bool TryGetChiseledBlock(Vector3Int position, out ChiseledBlockData block)
    {
        return snapshot.ChiseledBlocks.TryGetValue(position, out block);
    }

    public bool IsFaceOccludedByNeighbor(Vector3Int neighborPosition, int currentFaceIndex)
    {
        if (!IsInWorld(neighborPosition))
        {
            return true;
        }

        if (!IsNeighborLoaded(neighborPosition))
        {
            return true;
        }

        var neighborType = GetBlock(neighborPosition);
        if (neighborType != VoxelBlockType.Air)
        {
            return BlockShapeOcclusion.IsNeighborOccludingFace(neighborType, currentFaceIndex);
        }

        if (!snapshot.ChiseledBlocks.TryGetValue(neighborPosition, out var chiseled))
        {
            return false;
        }

        var neighborFace = VoxelConstants.OppositeFace[currentFaceIndex];
        return chiseled.IsSideFullySolid(neighborFace);
    }

    public bool IsMicroFaceOccludedByNeighbor(Vector3Int neighborBlockPos, int currentFace, int x, int y, int z, int resolution)
    {
        if (!IsInWorld(neighborBlockPos))
        {
            return true;
        }

        if (!IsNeighborLoaded(neighborBlockPos))
        {
            return true;
        }

        var neighborType = GetBlock(neighborBlockPos);
        if (neighborType != VoxelBlockType.Air)
        {
            if (!BlockShapeOcclusion.IsNeighborOccludingFace(neighborType, currentFace))
            {
                return false;
            }

            return true;
        }

        if (!snapshot.ChiseledBlocks.TryGetValue(neighborBlockPos, out var chiseledNeighbor))
        {
            return false;
        }

        return currentFace switch
        {
            0 => chiseledNeighbor.GetCell(0, y, z),
            1 => chiseledNeighbor.GetCell(resolution - 1, y, z),
            2 => chiseledNeighbor.GetCell(x, 0, z),
            3 => chiseledNeighbor.GetCell(x, resolution - 1, z),
            4 => chiseledNeighbor.GetCell(x, y, 0),
            5 => chiseledNeighbor.GetCell(x, y, resolution - 1),
            _ => false
        };
    }

    public Vector3Int LocalToWorld(Vector3Int chunkCoord, Vector3Int local)
    {
        return new Vector3Int(
            chunkCoord.x * ChunkSize + local.x,
            chunkCoord.y * ChunkSize + local.y,
            chunkCoord.z * ChunkSize + local.z);
    }

    private bool TryGetChunkBlock(Vector3Int worldPosition, out VoxelBlockType blockType)
    {
        blockType = VoxelBlockType.Air;
        if (!TryWorldToLocal(worldPosition, out var local))
        {
            return false;
        }

        blockType = snapshot.Blocks[ToIndex(local, snapshot.ChunkSize)];
        return true;
    }

    private bool TryWorldToLocal(Vector3Int worldPosition, out Vector3Int local)
    {
        local = default;
        var chunkSize = snapshot.ChunkSize;
        var origin = new Vector3Int(
            snapshot.Coord.x * chunkSize,
            snapshot.Coord.y * chunkSize,
            snapshot.Coord.z * chunkSize);
        local = worldPosition - origin;
        return local.x >= 0 && local.x < chunkSize
            && local.y >= 0 && local.y < chunkSize
            && local.z >= 0 && local.z < chunkSize;
    }

    private bool IsNeighborLoaded(Vector3Int worldPosition)
    {
        if (TryWorldToLocal(worldPosition, out _))
        {
            return true;
        }

        return snapshot.ExternalBlocks.ContainsKey(worldPosition)
            || snapshot.ChiseledBlocks.ContainsKey(worldPosition);
    }

    private static int ToIndex(Vector3Int local, int chunkSize)
    {
        return local.x + chunkSize * (local.y + chunkSize * local.z);
    }
}
