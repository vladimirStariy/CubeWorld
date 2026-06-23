using System.Collections.Generic;
using UnityEngine;

public static class BlockOutlineBuilder
{
    private const float HitFaceOutlineOffset = 0.008f;

    public static bool TryGetOutlineSegments(IVoxelBlockView world, Vector3Int blockPosition, List<LineSegment> segments)
    {
        segments.Clear();
        if (!world.IsBlockOccupied(blockPosition))
        {
            return false;
        }

        if (world.TryGetChiseledBlock(blockPosition, out var chiseled))
        {
            if (chiseled.OutlineDirty)
            {
                chiseled.CachedOutlineSegments.Clear();
                BuildChiseledOutline(chiseled, chiseled.CachedOutlineSegments);
                chiseled.OutlineDirty = false;
            }

            segments.AddRange(chiseled.CachedOutlineSegments);
            return segments.Count > 0;
        }

        if (VoxelBlockShapes.IsBottomSlab(world.GetBlock(blockPosition)))
        {
            BuildBottomSlabOutline(blockPosition, segments);
            return true;
        }

        if (VoxelBlockShapes.IsCustomMeshBlock(world.GetBlock(blockPosition)))
        {
            BuildCustomMeshBlockOutline(blockPosition, world.GetBlock(blockPosition), segments);
            return segments.Count > 0;
        }

        BuildCubeOutline(blockPosition, segments);
        return true;
    }

    public static bool TryGetHitFaceOutline(
        IVoxelBlockView world,
        Vector3Int blockPosition,
        Vector3 faceNormal,
        List<LineSegment> segments)
    {
        segments.Clear();
        if (!world.IsBlockOccupied(blockPosition))
        {
            return false;
        }

        var faceIndex = VoxelConstants.NormalToFaceIndex(faceNormal);
        var offset = (Vector3)VoxelConstants.NeighborDirs[faceIndex] * HitFaceOutlineOffset;

        if (world.TryGetChiseledBlock(blockPosition, out var chiseled))
        {
            BuildChiseledFaceOutline(chiseled, faceIndex, offset, segments);
            PromoteLocalSegmentsToWorld(blockPosition, segments);
            return segments.Count > 0;
        }

        if (VoxelBlockShapes.IsBottomSlab(world.GetBlock(blockPosition)))
        {
            BuildBottomSlabFaceOutline(faceIndex, offset, segments);
            PromoteLocalSegmentsToWorld(blockPosition, segments);
            return segments.Count > 0;
        }

        if (VoxelBlockShapes.IsCustomMeshBlock(world.GetBlock(blockPosition)))
        {
            BuildCustomMeshBlockFaceOutline(blockPosition, world.GetBlock(blockPosition), faceIndex, offset, segments);
            return segments.Count > 0;
        }

        BuildFullBlockFaceOutline(faceIndex, offset, segments);
        PromoteLocalSegmentsToWorld(blockPosition, segments);
        return segments.Count > 0;
    }

    private static void BuildCustomMeshBlockOutline(Vector3Int center, VoxelBlockType blockType, List<LineSegment> segments)
    {
        var bounds = VoxelBlockShapes.GetWorldBounds(center, blockType);
        BuildBoundsOutline(bounds, segments);
    }

    private static void BuildCustomMeshBlockFaceOutline(
        Vector3Int center,
        VoxelBlockType blockType,
        int faceIndex,
        Vector3 offset,
        List<LineSegment> segments)
    {
        var bounds = VoxelBlockShapes.GetWorldBounds(center, blockType);
        BuildBoundsFaceOutline(bounds, faceIndex, offset, segments);
    }

    private static void BuildFullBlockFaceOutline(int faceIndex, Vector3 offset, List<LineSegment> segments)
    {
        var corners = VoxelConstants.FaceVertices[faceIndex];
        var a = corners[0] + offset;
        var b = corners[1] + offset;
        var c = corners[2] + offset;
        var d = corners[3] + offset;

        segments.Add(new LineSegment(a, b));
        segments.Add(new LineSegment(b, c));
        segments.Add(new LineSegment(c, d));
        segments.Add(new LineSegment(d, a));
    }

    private static void BuildChiseledFaceOutline(
        ChiseledBlockData block,
        int faceIndex,
        Vector3 offset,
        List<LineSegment> segments)
    {
        var resolution = block.Resolution;
        var plane = faceIndex is 0 or 2 or 4 ? resolution - 1 : 0;
        var mask = new bool[resolution, resolution];
        var step = 1f / resolution;
        const float half = 0.5f;
        var edgeKeys = new HashSet<string>();

        for (int u = 0; u < resolution; u++)
        {
            for (int v = 0; v < resolution; v++)
            {
                var cell = VoxelConstants.MicroFaceToCell(faceIndex, plane, u, v);
                mask[u, v] = block.GetCell(cell.x, cell.y, cell.z);
            }
        }

        for (int u = 0; u < resolution; u++)
        {
            for (int v = 0; v < resolution; v++)
            {
                if (!mask[u, v])
                {
                    continue;
                }

                TryAddFaceGridEdge(faceIndex, plane, u, v, 0, resolution, half, step, offset, mask, edgeKeys, segments);
                TryAddFaceGridEdge(faceIndex, plane, u, v, 1, resolution, half, step, offset, mask, edgeKeys, segments);
                TryAddFaceGridEdge(faceIndex, plane, u, v, 2, resolution, half, step, offset, mask, edgeKeys, segments);
                TryAddFaceGridEdge(faceIndex, plane, u, v, 3, resolution, half, step, offset, mask, edgeKeys, segments);
            }
        }
    }

    private static void TryAddFaceGridEdge(
        int faceIndex,
        int plane,
        int u,
        int v,
        int side,
        int resolution,
        float half,
        float step,
        Vector3 offset,
        bool[,] mask,
        HashSet<string> edgeKeys,
        List<LineSegment> segments)
    {
        var draw = side switch
        {
            0 => u == 0 || !mask[u - 1, v],
            1 => u == resolution - 1 || !mask[u + 1, v],
            2 => v == 0 || !mask[u, v - 1],
            _ => v == resolution - 1 || !mask[u, v + 1],
        };

        if (!draw)
        {
            return;
        }

        Vector3 a;
        Vector3 b;
        switch (side)
        {
            case 0:
                a = FaceGridPoint(faceIndex, plane, u, v, half, step);
                b = FaceGridPoint(faceIndex, plane, u, v + 1, half, step);
                break;
            case 1:
                a = FaceGridPoint(faceIndex, plane, u + 1, v, half, step);
                b = FaceGridPoint(faceIndex, plane, u + 1, v + 1, half, step);
                break;
            case 2:
                a = FaceGridPoint(faceIndex, plane, u, v, half, step);
                b = FaceGridPoint(faceIndex, plane, u + 1, v, half, step);
                break;
            default:
                a = FaceGridPoint(faceIndex, plane, u, v + 1, half, step);
                b = FaceGridPoint(faceIndex, plane, u + 1, v + 1, half, step);
                break;
        }

        AddUniqueOutlineEdge(a + offset, b + offset, edgeKeys, segments);
    }

    private static Vector3 FaceGridPoint(int faceIndex, int plane, int u, int v, float half, float step)
    {
        return faceIndex switch
        {
            0 => new Vector3(-half + (plane + 1) * step, -half + u * step, -half + v * step),
            1 => new Vector3(-half + plane * step, -half + u * step, -half + v * step),
            2 => new Vector3(-half + u * step, -half + (plane + 1) * step, -half + v * step),
            3 => new Vector3(-half + u * step, -half + plane * step, -half + v * step),
            4 => new Vector3(-half + u * step, -half + v * step, -half + (plane + 1) * step),
            _ => new Vector3(-half + u * step, -half + v * step, -half + plane * step),
        };
    }

    private static void PromoteLocalSegmentsToWorld(Vector3Int blockPosition, List<LineSegment> segments)
    {
        var origin = (Vector3)blockPosition;
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            segments[i] = new LineSegment(origin + segment.From, origin + segment.To);
        }
    }

    private static void AddUniqueOutlineEdge(Vector3 a, Vector3 b, HashSet<string> edgeKeys, List<LineSegment> segments)
    {
        var keyA = $"{Mathf.RoundToInt(a.x * 10000f)}_{Mathf.RoundToInt(a.y * 10000f)}_{Mathf.RoundToInt(a.z * 10000f)}";
        var keyB = $"{Mathf.RoundToInt(b.x * 10000f)}_{Mathf.RoundToInt(b.y * 10000f)}_{Mathf.RoundToInt(b.z * 10000f)}";
        var edgeKey = string.CompareOrdinal(keyA, keyB) < 0 ? $"{keyA}|{keyB}" : $"{keyB}|{keyA}";

        if (edgeKeys.Add(edgeKey))
        {
            segments.Add(new LineSegment(a, b));
        }
    }

    private static void BuildBottomSlabFaceOutline(int faceIndex, Vector3 offset, List<LineSegment> segments)
    {
        var corners = GetBottomSlabFaceCorners(faceIndex);
        var a = corners[0] + offset;
        var b = corners[1] + offset;
        var c = corners[2] + offset;
        var d = corners[3] + offset;

        segments.Add(new LineSegment(a, b));
        segments.Add(new LineSegment(b, c));
        segments.Add(new LineSegment(c, d));
        segments.Add(new LineSegment(d, a));
    }

    private static Vector3[] GetBottomSlabFaceCorners(int faceIndex)
    {
        return faceIndex switch
        {
            0 => new[] { new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f) },
            1 => new[] { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f) },
            2 => new[] { new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, -0.5f) },
            3 => new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) },
            4 => new[] { new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) },
            _ => new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f) }
        };
    }

    private static void BuildBottomSlabOutline(Vector3Int center, List<LineSegment> segments)
    {
        var c = (Vector3)center;
        var corners = new[]
        {
            c + new Vector3(-0.5f, -0.5f, -0.5f),
            c + new Vector3(0.5f, -0.5f, -0.5f),
            c + new Vector3(0.5f, -0.5f, 0.5f),
            c + new Vector3(-0.5f, -0.5f, 0.5f),
            c + new Vector3(-0.5f, 0f, -0.5f),
            c + new Vector3(0.5f, 0f, -0.5f),
            c + new Vector3(0.5f, 0f, 0.5f),
            c + new Vector3(-0.5f, 0f, 0.5f)
        };

        var edges = new (int from, int to)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0),
            (4, 5), (5, 6), (6, 7), (7, 4),
            (0, 4), (1, 5), (2, 6), (3, 7)
        };

        for (int i = 0; i < edges.Length; i++)
        {
            segments.Add(new LineSegment(corners[edges[i].from], corners[edges[i].to]));
        }
    }

    private static void BuildCubeOutline(Vector3Int center, List<LineSegment> segments)
    {
        var c = (Vector3)center;
        var corners = new[]
        {
            c + new Vector3(-0.5f, -0.5f, -0.5f),
            c + new Vector3(0.5f, -0.5f, -0.5f),
            c + new Vector3(0.5f, -0.5f, 0.5f),
            c + new Vector3(-0.5f, -0.5f, 0.5f),
            c + new Vector3(-0.5f, 0.5f, -0.5f),
            c + new Vector3(0.5f, 0.5f, -0.5f),
            c + new Vector3(0.5f, 0.5f, 0.5f),
            c + new Vector3(-0.5f, 0.5f, 0.5f)
        };

        var edges = new (int from, int to)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0),
            (4, 5), (5, 6), (6, 7), (7, 4),
            (0, 4), (1, 5), (2, 6), (3, 7)
        };

        for (int i = 0; i < edges.Length; i++)
        {
            segments.Add(new LineSegment(corners[edges[i].from], corners[edges[i].to]));
        }
    }

    private static void BuildBoundsOutline(Bounds bounds, List<LineSegment> segments)
    {
        var c = bounds.center;
        var e = bounds.extents;
        var corners = new[]
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, e.z),
            c + new Vector3(-e.x, -e.y, e.z),
            c + new Vector3(-e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, e.z),
            c + new Vector3(-e.x, e.y, e.z)
        };

        var edges = new (int from, int to)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0),
            (4, 5), (5, 6), (6, 7), (7, 4),
            (0, 4), (1, 5), (2, 6), (3, 7)
        };

        for (int i = 0; i < edges.Length; i++)
        {
            segments.Add(new LineSegment(corners[edges[i].from], corners[edges[i].to]));
        }
    }

    private static void BuildBoundsFaceOutline(Bounds bounds, int faceIndex, Vector3 offset, List<LineSegment> segments)
    {
        var c = bounds.center;
        var e = bounds.extents;

        Vector3 a;
        Vector3 b;
        Vector3 d;
        Vector3 f;
        switch (faceIndex)
        {
            case 0:
                a = c + new Vector3(e.x, -e.y, -e.z);
                b = c + new Vector3(e.x, e.y, -e.z);
                d = c + new Vector3(e.x, e.y, e.z);
                f = c + new Vector3(e.x, -e.y, e.z);
                break;
            case 1:
                a = c + new Vector3(-e.x, -e.y, e.z);
                b = c + new Vector3(-e.x, e.y, e.z);
                d = c + new Vector3(-e.x, e.y, -e.z);
                f = c + new Vector3(-e.x, -e.y, -e.z);
                break;
            case 2:
                a = c + new Vector3(-e.x, e.y, e.z);
                b = c + new Vector3(e.x, e.y, e.z);
                d = c + new Vector3(e.x, e.y, -e.z);
                f = c + new Vector3(-e.x, e.y, -e.z);
                break;
            case 3:
                a = c + new Vector3(-e.x, -e.y, -e.z);
                b = c + new Vector3(e.x, -e.y, -e.z);
                d = c + new Vector3(e.x, -e.y, e.z);
                f = c + new Vector3(-e.x, -e.y, e.z);
                break;
            case 4:
                a = c + new Vector3(e.x, -e.y, e.z);
                b = c + new Vector3(e.x, e.y, e.z);
                d = c + new Vector3(-e.x, e.y, e.z);
                f = c + new Vector3(-e.x, -e.y, e.z);
                break;
            default:
                a = c + new Vector3(-e.x, -e.y, -e.z);
                b = c + new Vector3(-e.x, e.y, -e.z);
                d = c + new Vector3(e.x, e.y, -e.z);
                f = c + new Vector3(e.x, -e.y, -e.z);
                break;
        }

        a += offset;
        b += offset;
        d += offset;
        f += offset;

        segments.Add(new LineSegment(a, b));
        segments.Add(new LineSegment(b, d));
        segments.Add(new LineSegment(d, f));
        segments.Add(new LineSegment(f, a));
    }

    private static void BuildChiseledOutline(ChiseledBlockData block, List<LineSegment> segments)
    {
        var edgeMap = new Dictionary<string, OutlineEdgeAccumulator>();
        var resolution = block.Resolution;
        var step = 1f / resolution;
        var half = 0.5f;
        var center = (Vector3)block.WorldPosition;
        var mask = new bool[resolution, resolution];

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            for (int p = 0; p < resolution; p++)
            {
                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        var n = VoxelConstants.NeighborDirs[face];
                        var cell = VoxelConstants.MicroFaceToCell(face, p, u, v);
                        if (!block.GetCell(cell.x, cell.y, cell.z))
                        {
                            mask[u, v] = false;
                            continue;
                        }

                        var nx = cell.x + n.x;
                        var ny = cell.y + n.y;
                        var nz = cell.z + n.z;

                        if (nx >= 0 && nx < resolution &&
                            ny >= 0 && ny < resolution &&
                            nz >= 0 && nz < resolution &&
                            block.GetCell(nx, ny, nz))
                        {
                            mask[u, v] = false;
                            continue;
                        }

                        mask[u, v] = true;
                    }
                }

                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        if (!mask[u, v])
                        {
                            continue;
                        }

                        var width = 1;
                        while (v + width < resolution && mask[u, v + width])
                        {
                            width++;
                        }

                        var height = 1;
                        var canGrow = true;
                        while (u + height < resolution && canGrow)
                        {
                            for (int k = 0; k < width; k++)
                            {
                                if (!mask[u + height, v + k])
                                {
                                    canGrow = false;
                                    break;
                                }
                            }

                            if (canGrow)
                            {
                                height++;
                            }
                        }

                        AddGreedyOutlineRect(face, p, u, v, height, width, resolution, center, half, step, edgeMap);

                        for (int du = 0; du < height; du++)
                        {
                            for (int dv = 0; dv < width; dv++)
                            {
                                mask[u + du, v + dv] = false;
                            }
                        }
                    }
                }
            }
        }

        FlushOutlineEdges(edgeMap, segments);
    }

    private static void AddGreedyOutlineRect(
        int faceIndex,
        int plane,
        int uStart,
        int vStart,
        int uSize,
        int vSize,
        int resolution,
        Vector3 center,
        float half,
        float step,
        Dictionary<string, OutlineEdgeAccumulator> edgeMap)
    {
        float xMin, xMax, yMin, yMax, zMin, zMax;
        Vector3 a, b, c, d;

        switch (faceIndex)
        {
            case 0: // +X
                xMin = xMax = center.x - half + (plane + 1) * step;
                yMin = center.y - half + uStart * step;
                yMax = center.y - half + (uStart + uSize) * step;
                zMin = center.z - half + vStart * step;
                zMax = center.z - half + (vStart + vSize) * step;
                a = new Vector3(xMin, yMin, zMin); b = new Vector3(xMax, yMax, zMin); c = new Vector3(xMax, yMax, zMax); d = new Vector3(xMin, yMin, zMax);
                break;
            case 1: // -X
                xMin = xMax = center.x - half + plane * step;
                yMin = center.y - half + uStart * step;
                yMax = center.y - half + (uStart + uSize) * step;
                zMin = center.z - half + vStart * step;
                zMax = center.z - half + (vStart + vSize) * step;
                a = new Vector3(xMin, yMin, zMax); b = new Vector3(xMax, yMax, zMax); c = new Vector3(xMax, yMax, zMin); d = new Vector3(xMin, yMin, zMin);
                break;
            case 2: // +Y
                yMin = yMax = center.y - half + (plane + 1) * step;
                xMin = center.x - half + uStart * step;
                xMax = center.x - half + (uStart + uSize) * step;
                zMin = center.z - half + vStart * step;
                zMax = center.z - half + (vStart + vSize) * step;
                a = new Vector3(xMin, yMin, zMax); b = new Vector3(xMax, yMax, zMax); c = new Vector3(xMax, yMax, zMin); d = new Vector3(xMin, yMin, zMin);
                break;
            case 3: // -Y
                yMin = yMax = center.y - half + plane * step;
                xMin = center.x - half + uStart * step;
                xMax = center.x - half + (uStart + uSize) * step;
                zMin = center.z - half + vStart * step;
                zMax = center.z - half + (vStart + vSize) * step;
                a = new Vector3(xMin, yMin, zMin); b = new Vector3(xMax, yMax, zMin); c = new Vector3(xMax, yMax, zMax); d = new Vector3(xMin, yMin, zMax);
                break;
            case 4: // +Z
                zMin = zMax = center.z - half + (plane + 1) * step;
                xMin = center.x - half + uStart * step;
                xMax = center.x - half + (uStart + uSize) * step;
                yMin = center.y - half + vStart * step;
                yMax = center.y - half + (vStart + vSize) * step;
                a = new Vector3(xMax, yMin, zMin); b = new Vector3(xMax, yMax, zMax); c = new Vector3(xMin, yMax, zMax); d = new Vector3(xMin, yMin, zMin);
                break;
            default: // -Z
                zMin = zMax = center.z - half + plane * step;
                xMin = center.x - half + uStart * step;
                xMax = center.x - half + (uStart + uSize) * step;
                yMin = center.y - half + vStart * step;
                yMax = center.y - half + (vStart + vSize) * step;
                a = new Vector3(xMin, yMin, zMin); b = new Vector3(xMin, yMax, zMax); c = new Vector3(xMax, yMax, zMax); d = new Vector3(xMax, yMin, zMin);
                break;
        }

        AccumulateEdge(a, b, edgeMap);
        AccumulateEdge(b, c, edgeMap);
        AccumulateEdge(c, d, edgeMap);
        AccumulateEdge(d, a, edgeMap);
    }

    private static void AccumulateEdge(Vector3 a, Vector3 b, Dictionary<string, OutlineEdgeAccumulator> edgeMap)
    {
        var keyA = $"{Mathf.RoundToInt(a.x * 1000f)}_{Mathf.RoundToInt(a.y * 1000f)}_{Mathf.RoundToInt(a.z * 1000f)}";
        var keyB = $"{Mathf.RoundToInt(b.x * 1000f)}_{Mathf.RoundToInt(b.y * 1000f)}_{Mathf.RoundToInt(b.z * 1000f)}";
        var edgeKey = string.CompareOrdinal(keyA, keyB) < 0 ? $"{keyA}|{keyB}" : $"{keyB}|{keyA}";

        if (edgeMap.TryGetValue(edgeKey, out var existing))
        {
            existing.Count++;
            edgeMap[edgeKey] = existing;
            return;
        }

        edgeMap.Add(edgeKey, new OutlineEdgeAccumulator(a, b));
    }

    private static void FlushOutlineEdges(Dictionary<string, OutlineEdgeAccumulator> edgeMap, List<LineSegment> segments)
    {
        foreach (var kv in edgeMap)
        {
            segments.Add(new LineSegment(kv.Value.From, kv.Value.To));
        }
    }

    private struct OutlineEdgeAccumulator
    {
        public Vector3 From;
        public Vector3 To;
        public int Count;

        public OutlineEdgeAccumulator(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
            Count = 1;
        }
    }
}
