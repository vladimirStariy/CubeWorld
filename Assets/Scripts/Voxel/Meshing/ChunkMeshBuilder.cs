using System.Collections.Generic;
using UnityEngine;

public static class ChunkMeshBuilder
{
    public static void ClearChunkMesh(ChunkRenderEntry entry)
    {
        entry?.Clear();
    }

    public static void BuildChunkMesh(IVoxelBlockView world, ChunkBlockData blocks, ChunkRenderEntry entry)
    {
        var scratch = new ChunkMeshScratch();
        BuildChunkMesh(world, blocks, entry, scratch);
    }

    public static void BuildChunkMesh(IVoxelBlockView world, ChunkBlockData blocks, ChunkRenderEntry entry, ChunkMeshScratch scratch)
    {
        BuildChunkMesh(world, blocks, scratch);
        entry.ApplyMesh(scratch.Vertices, scratch.Triangles, scratch.Normals, scratch.Uvs, scratch.TileRects, scratch.TileCounts);
    }

    public static void BuildChunkMesh(IVoxelBlockView world, ChunkBlockData blocks, ChunkMeshScratch scratch)
    {
        scratch.Clear();

        var vertices = scratch.Vertices;
        var triangles = scratch.Triangles;
        var normals = scratch.Normals;
        var uvs = scratch.Uvs;
        var tileRects = scratch.TileRects;
        var tileCounts = scratch.TileCounts;

        var chunkSize = world.ChunkSize;
        var chunkWorldOrigin = new Vector3Int(
            blocks.Coord.x * chunkSize,
            blocks.Coord.y * chunkSize,
            blocks.Coord.z * chunkSize);

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            AddGreedyFullCubeFaces(world, blocks, face, vertices, triangles, normals, uvs, tileRects, tileCounts, scratch);
        }

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var local = new Vector3Int(x, y, z);
                    var blockWorld = world.LocalToWorld(blocks.Coord, local);
                    var blockType = blocks.GetBlock(local);
                    if (VoxelBlockShapes.IsBottomSlab(blockType))
                    {
                        AddBottomSlabToMesh(world, blockWorld, chunkWorldOrigin, blockType, vertices, triangles, normals, uvs, tileRects, tileCounts);
                    }
                    else if (VoxelBlockShapes.IsCustomMeshBlock(blockType))
                    {
                        AddCustomMeshBlockToMesh(world, blockWorld, chunkWorldOrigin, blockType, vertices, triangles, normals, uvs, tileRects, tileCounts);
                    }
                }
            }
        }

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var local = new Vector3Int(x, y, z);
                    var blockWorld = world.LocalToWorld(blocks.Coord, local);

                    if (world.TryGetChiseledBlock(blockWorld, out var chiseled))
                    {
                        AddChiseledBlockToMesh(world, chiseled, local, chunkWorldOrigin, vertices, triangles, normals, uvs, tileRects, tileCounts);
                    }
                }
            }
        }

    }

    private static void AddGreedyFullCubeFaces(
        IVoxelBlockView world,
        ChunkBlockData blocks,
        int faceIndex,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts,
        ChunkMeshScratch scratch)
    {
        var chunkSize = world.ChunkSize;
        var mask = scratch.GetGreedyMask(chunkSize);

        for (int d = 0; d < chunkSize; d++)
        {
            for (int u = 0; u < chunkSize; u++)
            {
                for (int v = 0; v < chunkSize; v++)
                {
                    var local = FaceSliceToLocal(faceIndex, d, u, v);
                    if (!ChunkMeshOcclusion.TryGetGreedyFaceSlot(world, blocks, chunkSize, local, faceIndex, out var textureSlot))
                    {
                        mask[u, v] = -1;
                        continue;
                    }

                    mask[u, v] = textureSlot;
                }
            }

            for (int u = 0; u < chunkSize; u++)
            {
                for (int v = 0; v < chunkSize; v++)
                {
                    var textureSlot = mask[u, v];
                    if (textureSlot < 0)
                    {
                        continue;
                    }

                    var width = 1;
                    while (v + width < chunkSize && mask[u, v + width] == textureSlot)
                    {
                        width++;
                    }

                    var height = 1;
                    var canGrow = true;
                    while (u + height < chunkSize && canGrow)
                    {
                        for (int k = 0; k < width; k++)
                        {
                            if (mask[u + height, v + k] != textureSlot)
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

                    AddGreedyFullCubeFace(faceIndex, d, u, v, height, width, textureSlot, vertices, triangles, normals, uvs, tileRects, tileCounts);

                    for (int du = 0; du < height; du++)
                    {
                        for (int dv = 0; dv < width; dv++)
                        {
                            mask[u + du, v + dv] = -1;
                        }
                    }
                }
            }
        }
    }

    private static Vector3Int FaceSliceToLocal(int faceIndex, int d, int u, int v)
    {
        return faceIndex switch
        {
            0 or 1 => new Vector3Int(d, u, v),
            2 or 3 => new Vector3Int(u, d, v),
            _ => new Vector3Int(u, v, d)
        };
    }

    /// <summary>
    /// Normalized 0..1 quad UVs per face (matches BlockPreviewMeshBuilder winding).
    /// Shader tiles with frac(quadUV * tileCount).
    /// </summary>
    private static void GetFaceQuadNormUv(
        int faceIndex,
        out Vector2 uv0,
        out Vector2 uv1,
        out Vector2 uv2,
        out Vector2 uv3)
    {
        switch (faceIndex)
        {
            case 0:
            case 1:
            case 5:
                uv0 = new Vector2(0f, 0f);
                uv1 = new Vector2(0f, 1f);
                uv2 = new Vector2(1f, 1f);
                uv3 = new Vector2(1f, 0f);
                break;
            case 2:
            case 3:
                uv0 = new Vector2(0f, 0f);
                uv1 = new Vector2(1f, 0f);
                uv2 = new Vector2(1f, 1f);
                uv3 = new Vector2(0f, 1f);
                break;
            default: // +Z
                uv0 = new Vector2(1f, 0f);
                uv1 = new Vector2(1f, 1f);
                uv2 = new Vector2(0f, 1f);
                uv3 = new Vector2(0f, 0f);
                break;
        }
    }

    private static Vector2 FaceTileCount(int faceIndex, int height, int width)
    {
        return faceIndex switch
        {
            0 or 1 => new Vector2(width, height),
            _ => new Vector2(height, width)
        };
    }

    private static void AddGreedyFullCubeFace(
        int faceIndex,
        int d,
        int uStart,
        int vStart,
        int height,
        int width,
        int textureSlot,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var uMin = uStart - 0.5f;
        var uMax = uStart + height - 0.5f;
        var vMin = vStart - 0.5f;
        var vMax = vStart + width - 0.5f;
        GetFaceQuadNormUv(faceIndex, out var uv0, out var uv1, out var uv2, out var uv3);
        var tileCount = FaceTileCount(faceIndex, height, width);

        switch (faceIndex)
        {
            case 0:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(d + 0.5f, uMin, vMin), new Vector3(d + 0.5f, uMax, vMin), new Vector3(d + 0.5f, uMax, vMax), new Vector3(d + 0.5f, uMin, vMax),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
            case 1:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(d - 0.5f, uMin, vMax), new Vector3(d - 0.5f, uMax, vMax), new Vector3(d - 0.5f, uMax, vMin), new Vector3(d - 0.5f, uMin, vMin),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
            case 2:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(uMin, d + 0.5f, vMax), new Vector3(uMax, d + 0.5f, vMax), new Vector3(uMax, d + 0.5f, vMin), new Vector3(uMin, d + 0.5f, vMin),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
            case 3:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(uMin, d - 0.5f, vMin), new Vector3(uMax, d - 0.5f, vMin), new Vector3(uMax, d - 0.5f, vMax), new Vector3(uMin, d - 0.5f, vMax),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
            case 4:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(uMax, vMin, d + 0.5f), new Vector3(uMax, vMax, d + 0.5f), new Vector3(uMin, vMax, d + 0.5f), new Vector3(uMin, vMin, d + 0.5f),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
            default:
                AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(uMin, vMin, d - 0.5f), new Vector3(uMin, vMax, d - 0.5f), new Vector3(uMax, vMax, d - 0.5f), new Vector3(uMax, vMin, d - 0.5f),
                    uv0, uv1, uv2, uv3, textureSlot, tileCount);
                break;
        }
    }

    private static void AddBlockFaceQuad(
        int faceIndex,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        int textureSlot,
        Vector2 tileCount)
    {
        GetFaceQuadNormUv(faceIndex, out var uv0, out var uv1, out var uv2, out var uv3);
        AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
            v0, v1, v2, v3, uv0, uv1, uv2, uv3, textureSlot, tileCount);
    }

    private static void AddBottomSlabToMesh(
        IVoxelBlockView world,
        Vector3Int blockWorld,
        Vector3Int chunkWorldOrigin,
        VoxelBlockType blockType,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var blockOffset = (Vector3)blockWorld - (Vector3)chunkWorldOrigin;
        var ymin = blockOffset.y - 0.5f;
        var ymax = blockOffset.y;
        var xmin = blockOffset.x - 0.5f;
        var xmax = blockOffset.x + 0.5f;
        var zmin = blockOffset.z - 0.5f;
        var zmax = blockOffset.z + 0.5f;

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            if (!IsBottomSlabFaceVisible(world, blockWorld, face))
            {
                continue;
            }

            var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(blockType, face);

            switch (face)
            {
                case 0: // +X
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                case 1: // -X
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                        textureSlot, Vector2.one);
                    break;
                case 2: // +Y
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
                        textureSlot, Vector2.one);
                    break;
                case 3: // -Y
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                case 4: // +Z
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                default: // -Z
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                        textureSlot, Vector2.one);
                    break;
            }
        }
    }

    private static bool IsBottomSlabFaceVisible(IVoxelBlockView world, Vector3Int blockWorld, int faceIndex)
    {
        var neighbor = blockWorld + VoxelConstants.NeighborDirs[faceIndex];
        if (!world.IsInWorld(neighbor))
        {
            return true;
        }

        if (world.IsFaceOccludedByNeighbor(neighbor, faceIndex))
        {
            return false;
        }

        var neighborType = world.GetBlock(neighbor);
        if (neighborType == VoxelBlockType.Air)
        {
            if (world.HasChiseledBlock(neighbor))
            {
                var neighborFace = VoxelConstants.OppositeFace[faceIndex];
                return !world.TryGetChiseledBlock(neighbor, out var chiseled) || !chiseled.IsSideFullySolid(neighborFace);
            }

            return true;
        }

        if (VoxelBlockShapes.IsFullCube(neighborType))
        {
            return false;
        }

        if (VoxelBlockShapes.IsBottomSlab(neighborType))
        {
            return faceIndex is 2 or 3;
        }

        return false;
    }

    private static void AddCustomMeshBlockToMesh(
        IVoxelBlockView world,
        Vector3Int blockWorld,
        Vector3Int chunkWorldOrigin,
        VoxelBlockType blockType,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        switch (blockType)
        {
            case VoxelBlockType.Campfire:
                AddCampfireToMesh(world, blockWorld, chunkWorldOrigin, vertices, triangles, normals, uvs, tileRects, tileCounts);
                break;
        }
    }

    private static void AddCampfireToMesh(
        IVoxelBlockView world,
        Vector3Int blockWorld,
        Vector3Int chunkWorldOrigin,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var blockOffset = (Vector3)blockWorld - (Vector3)chunkWorldOrigin;
        var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(VoxelBlockType.Dirt, BlockFace.Top);

        // Ash bed.
        AddTexturedBox(world, blockWorld, blockOffset, -0.30f, 0.30f, -0.50f, -0.38f, -0.30f, 0.30f, textureSlot, vertices, triangles, normals, uvs, tileRects, tileCounts);
        // Crossed logs.
        AddTexturedBox(world, blockWorld, blockOffset, -0.42f, 0.42f, -0.34f, -0.22f, -0.10f, 0.10f, textureSlot, vertices, triangles, normals, uvs, tileRects, tileCounts);
        AddTexturedBox(world, blockWorld, blockOffset, -0.10f, 0.10f, -0.22f, -0.10f, -0.42f, 0.42f, textureSlot, vertices, triangles, normals, uvs, tileRects, tileCounts);
        // Small center pile.
        AddTexturedBox(world, blockWorld, blockOffset, -0.14f, 0.14f, -0.10f, 0.06f, -0.14f, 0.14f, textureSlot, vertices, triangles, normals, uvs, tileRects, tileCounts);
    }

    private static void AddTexturedBox(
        IVoxelBlockView world,
        Vector3Int blockWorld,
        Vector3 blockOffset,
        float xmin,
        float xmax,
        float ymin,
        float ymax,
        float zmin,
        float zmax,
        int textureSlot,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            if (!IsCampfireFaceVisible(world, blockWorld, face))
            {
                continue;
            }

            switch (face)
            {
                case 0: // +X
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmax, ymin, zmin), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmax, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                case 1: // -X
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmin, ymin, zmax), blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmin, ymax, zmin), blockOffset + new Vector3(xmin, ymin, zmin),
                        textureSlot, Vector2.one);
                    break;
                case 2: // +Y
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmin, ymax, zmin),
                        textureSlot, Vector2.one);
                    break;
                case 3: // -Y
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmin, ymin, zmin), blockOffset + new Vector3(xmax, ymin, zmin), blockOffset + new Vector3(xmax, ymin, zmax), blockOffset + new Vector3(xmin, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                case 4: // +Z
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmax, ymin, zmax), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmin, ymin, zmax),
                        textureSlot, Vector2.one);
                    break;
                default: // -Z
                    AddBlockFaceQuad(face, vertices, triangles, normals, uvs, tileRects, tileCounts,
                        blockOffset + new Vector3(xmin, ymin, zmin), blockOffset + new Vector3(xmin, ymax, zmin), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmax, ymin, zmin),
                        textureSlot, Vector2.one);
                    break;
            }
        }
    }

    private static bool IsCampfireFaceVisible(IVoxelBlockView world, Vector3Int blockWorld, int faceIndex)
    {
        var neighbor = blockWorld + VoxelConstants.NeighborDirs[faceIndex];
        if (!world.IsInWorld(neighbor))
        {
            return true;
        }

        if (world.IsFaceOccludedByNeighbor(neighbor, faceIndex))
        {
            return false;
        }

        var neighborType = world.GetBlock(neighbor);
        if (neighborType == VoxelBlockType.Air)
        {
            if (world.HasChiseledBlock(neighbor))
            {
                var neighborFace = VoxelConstants.OppositeFace[faceIndex];
                return !world.TryGetChiseledBlock(neighbor, out var chiseled) || !chiseled.IsSideFullySolid(neighborFace);
            }

            return true;
        }

        return !VoxelBlockShapes.IsFullCube(neighborType) && !VoxelBlockShapes.IsCustomMeshBlock(neighborType);
    }

    private static void AddChiseledBlockToMesh(
        IVoxelBlockView world,
        ChiseledBlockData block,
        Vector3Int chunkLocalBlockPos,
        Vector3Int chunkWorldOrigin,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var resolution = block.Resolution;
        var mask = new bool[resolution, resolution];

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            for (int p = 0; p < resolution; p++)
            {
                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        var cell = VoxelConstants.MicroFaceToCell(face, p, u, v);
                        if (!block.GetCell(cell.x, cell.y, cell.z))
                        {
                            mask[u, v] = false;
                            continue;
                        }

                        var n = VoxelConstants.NeighborDirs[face];
                        var nx = cell.x + n.x;
                        var ny = cell.y + n.y;
                        var nz = cell.z + n.z;

                        if (nx >= 0 && nx < resolution &&
                            ny >= 0 && ny < resolution &&
                            nz >= 0 && nz < resolution)
                        {
                            mask[u, v] = !block.GetCell(nx, ny, nz);
                            continue;
                        }

                        var neighborBlockPos = block.WorldPosition + VoxelConstants.NeighborDirs[face];
                        mask[u, v] = !world.IsMicroFaceOccludedByNeighbor(neighborBlockPos, face, cell.x, cell.y, cell.z, resolution);
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

                        AddMicroGreedyFace(face, p, u, v, height, width, resolution, chunkLocalBlockPos, chunkWorldOrigin, block.BlockType, vertices, triangles, normals, uvs, tileRects, tileCounts);

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
    }

    private static void AddMicroGreedyFace(
        int faceIndex,
        int plane,
        int uStart,
        int vStart,
        int uSize,
        int vSize,
        int resolution,
        Vector3Int blockCenter,
        Vector3Int chunkWorldOrigin,
        VoxelBlockType blockType,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var blockOffset = (Vector3)blockCenter;
        var step = 1f / resolution;

        float xMin, xMax, yMin, yMax, zMin, zMax;
        Vector3 v0, v1, v2, v3;

        switch (faceIndex)
        {
            case 0: // +X
                xMin = xMax = -0.5f + (plane + 1) * step;
                yMin = -0.5f + uStart * step;
                yMax = -0.5f + (uStart + uSize) * step;
                zMin = -0.5f + vStart * step;
                zMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMin, yMin, zMin);
                v1 = blockOffset + new Vector3(xMax, yMax, zMin);
                v2 = blockOffset + new Vector3(xMax, yMax, zMax);
                v3 = blockOffset + new Vector3(xMin, yMin, zMax);
                break;
            case 1: // -X
                xMin = xMax = -0.5f + plane * step;
                yMin = -0.5f + uStart * step;
                yMax = -0.5f + (uStart + uSize) * step;
                zMin = -0.5f + vStart * step;
                zMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMin, yMin, zMax);
                v1 = blockOffset + new Vector3(xMax, yMax, zMax);
                v2 = blockOffset + new Vector3(xMax, yMax, zMin);
                v3 = blockOffset + new Vector3(xMin, yMin, zMin);
                break;
            case 2: // +Y
                yMin = yMax = -0.5f + (plane + 1) * step;
                xMin = -0.5f + uStart * step;
                xMax = -0.5f + (uStart + uSize) * step;
                zMin = -0.5f + vStart * step;
                zMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMin, yMin, zMax);
                v1 = blockOffset + new Vector3(xMax, yMax, zMax);
                v2 = blockOffset + new Vector3(xMax, yMax, zMin);
                v3 = blockOffset + new Vector3(xMin, yMin, zMin);
                break;
            case 3: // -Y
                yMin = yMax = -0.5f + plane * step;
                xMin = -0.5f + uStart * step;
                xMax = -0.5f + (uStart + uSize) * step;
                zMin = -0.5f + vStart * step;
                zMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMin, yMin, zMin);
                v1 = blockOffset + new Vector3(xMax, yMax, zMin);
                v2 = blockOffset + new Vector3(xMax, yMax, zMax);
                v3 = blockOffset + new Vector3(xMin, yMin, zMax);
                break;
            case 4: // +Z
                zMin = zMax = -0.5f + (plane + 1) * step;
                xMin = -0.5f + uStart * step;
                xMax = -0.5f + (uStart + uSize) * step;
                yMin = -0.5f + vStart * step;
                yMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMax, yMin, zMin);
                v1 = blockOffset + new Vector3(xMax, yMax, zMax);
                v2 = blockOffset + new Vector3(xMin, yMax, zMax);
                v3 = blockOffset + new Vector3(xMin, yMin, zMin);
                break;
            default: // -Z
                zMin = zMax = -0.5f + plane * step;
                xMin = -0.5f + uStart * step;
                xMax = -0.5f + (uStart + uSize) * step;
                yMin = -0.5f + vStart * step;
                yMax = -0.5f + (vStart + vSize) * step;
                v0 = blockOffset + new Vector3(xMin, yMin, zMin);
                v1 = blockOffset + new Vector3(xMin, yMax, zMax);
                v2 = blockOffset + new Vector3(xMax, yMax, zMax);
                v3 = blockOffset + new Vector3(xMax, yMin, zMin);
                break;
        }

        var textureSlot = BlockTextureLibrary.GetFaceAtlasSlot(blockType, faceIndex);
        var tileCount = FaceTileCount(faceIndex, uSize, vSize);
        GetFaceQuadNormUv(faceIndex, out var uv0, out var uv1, out var uv2, out var uv3);

        AddQuad(vertices, triangles, normals, uvs, tileRects, tileCounts, VoxelConstants.NeighborDirs[faceIndex],
            v0, v1, v2, v3, uv0, uv1, uv2, uv3, textureSlot, tileCount);
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts,
        Vector3 normal,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector2 uv0,
        Vector2 uv1,
        Vector2 uv2,
        Vector2 uv3,
        int textureSlot,
        Vector2 tileCount)
    {
        var tileRect = BlockTextureLibrary.GetAtlasTileRect(textureSlot);
        var baseIndex = vertices.Count;
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        tileRects.Add(tileRect);
        tileRects.Add(tileRect);
        tileRects.Add(tileRect);
        tileRects.Add(tileRect);
        tileCounts.Add(tileCount);
        tileCounts.Add(tileCount);
        tileCounts.Add(tileCount);
        tileCounts.Add(tileCount);
    }
}
