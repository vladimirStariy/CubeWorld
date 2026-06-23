using UnityEngine;

/// <summary>
/// Client-side materialized world used for meshing and targeting raycasts.
/// Populated only from replication — not authoritative.
/// </summary>
public interface IClientWorldStore : IVoxelBlockView
{
    bool IsChunkLoaded(Vector3Int chunkCoord);

    bool TryGetChunkBlocks(Vector3Int chunkCoord, out VoxelBlockType[] blocks);

    void RequestChunk(Vector3Int chunkCoord);
}

public interface IClientWorldQueries
{
    bool TryGetOutlineSegments(Vector3Int blockPosition, System.Collections.Generic.List<LineSegment> segments);

    bool TryGetHitFaceOutline(Vector3Int blockPosition, Vector3 faceNormal, System.Collections.Generic.List<LineSegment> segments);

    bool TryBuildStickStackOutline(Vector3Int hitBlock, Vector3 faceNormal, System.Collections.Generic.List<LineSegment> segments);

    bool TryGetStickStackCount(Vector3Int hitBlock, Vector3 faceNormal, out int stickCount);
}
