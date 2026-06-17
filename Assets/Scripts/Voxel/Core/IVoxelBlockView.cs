using UnityEngine;

public interface IVoxelBlockView
{
    int ChunkSize { get; }
    int ChiselResolution { get; }

    bool IsInWorld(Vector3Int position);
    VoxelBlockType GetBlock(Vector3Int position);
    bool IsBlockOccupied(Vector3Int position);
    bool IsFullCubeBlock(Vector3Int position);
    bool HasChiseledBlock(Vector3Int position);
    bool TryGetChiseledBlock(Vector3Int position, out ChiseledBlockData block);
    bool IsFaceOccludedByNeighbor(Vector3Int neighborPosition, int currentFaceIndex);
    bool IsMicroFaceOccludedByNeighbor(Vector3Int neighborBlockPos, int currentFace, int x, int y, int z, int resolution);
    Vector3Int LocalToWorld(Vector3Int chunkCoord, Vector3Int local);
}
