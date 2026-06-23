using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Read-only world queries used by client presentation (outlines, ground item highlights).
/// </summary>
public interface IWorldPresentationQueries
{
    bool TryGetOutlineSegments(Vector3Int blockPosition, List<LineSegment> segments);

    bool TryGetHitFaceOutline(Vector3Int blockPosition, Vector3 faceNormal, List<LineSegment> segments);

    bool TryBuildStickStackOutline(Vector3Int hitBlock, Vector3 faceNormal, List<LineSegment> segments);

    bool TryBuildStickStackOutline(GroundItemSurfaceKey key, List<LineSegment> segments);

    bool TryGetStickStackCount(Vector3Int hitBlock, Vector3 faceNormal, out int stickCount);

    bool TryGetStickStackCount(GroundItemSurfaceKey key, out int stickCount);
}
