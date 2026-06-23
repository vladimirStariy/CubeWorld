using UnityEngine;

internal static class ChunkMeshOcclusion
{
    public static bool TryGetGreedyFaceSlot(
        IVoxelBlockView world,
        ChunkBlockData blocks,
        int chunkSize,
        Vector3Int local,
        int faceIndex,
        out int textureSlot)
    {
        textureSlot = -1;
        var blockType = blocks.GetBlock(local);
        if (!VoxelBlockShapes.IsFullCube(blockType))
        {
            return false;
        }

        if (IsFaceOccluded(world, blocks, chunkSize, local, faceIndex))
        {
            return false;
        }

        textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(blockType, faceIndex);
        return true;
    }

    private static bool IsFaceOccluded(
        IVoxelBlockView world,
        ChunkBlockData blocks,
        int chunkSize,
        Vector3Int local,
        int faceIndex)
    {
        var neighborLocal = local + VoxelConstants.NeighborDirs[faceIndex];
        if (IsInsideChunk(neighborLocal, chunkSize))
        {
            var neighborType = blocks.GetBlock(neighborLocal);
            if (neighborType != VoxelBlockType.Air)
            {
                return BlockShapeOcclusion.IsNeighborOccludingFace(neighborType, faceIndex);
            }

            return world.IsFaceOccludedByNeighbor(world.LocalToWorld(blocks.Coord, neighborLocal), faceIndex);
        }

        return world.IsFaceOccludedByNeighbor(world.LocalToWorld(blocks.Coord, neighborLocal), faceIndex);
    }

    private static bool IsInsideChunk(Vector3Int local, int chunkSize)
    {
        return local.x >= 0 && local.x < chunkSize
            && local.y >= 0 && local.y < chunkSize
            && local.z >= 0 && local.z < chunkSize;
    }
}
