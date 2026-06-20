using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ChunkMeshBuilder
{
    public static void BuildChunkMesh(IVoxelBlockView world, ChunkData chunk)
    {
        var vertices = new List<Vector3>(1024);
        var triangles = new List<int>(2048);
        var normals = new List<Vector3>(1024);
        var uvs = new List<Vector2>(1024);

        var chunkSize = world.ChunkSize;
        var chunkWorldOrigin = new Vector3Int(
            chunk.Coord.x * chunkSize,
            chunk.Coord.y * chunkSize,
            chunk.Coord.z * chunkSize);

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            AddFullBlockFaces(world, chunk, face, vertices, triangles, normals, uvs);
        }

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var local = new Vector3Int(x, y, z);
                    var blockWorld = world.LocalToWorld(chunk.Coord, local);
                    var blockType = world.GetBlock(blockWorld);
                    if (VoxelBlockShapes.IsBottomSlab(blockType))
                    {
                        AddBottomSlabToMesh(world, blockWorld, chunkWorldOrigin, blockType, vertices, triangles, normals, uvs);
                    }
                    else if (VoxelBlockShapes.IsCustomMeshBlock(blockType))
                    {
                        AddCustomMeshBlockToMesh(world, blockWorld, chunkWorldOrigin, blockType, vertices, triangles, normals, uvs);
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
                    var blockWorld = world.LocalToWorld(chunk.Coord, local);

                    if (world.TryGetChiseledBlock(blockWorld, out var chiseled))
                    {
                        AddChiseledBlockToMesh(world, chiseled, local, chunkWorldOrigin, vertices, triangles, normals, uvs);
                    }
                }
            }
        }

        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        chunk.Filter.sharedMesh = mesh;
        chunk.Collider.sharedMesh = null;
        chunk.Collider.sharedMesh = mesh;
    }

    private static void AddFullBlockFaces(
        IVoxelBlockView world,
        ChunkData chunk,
        int faceIndex,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        var chunkSize = world.ChunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var local = new Vector3Int(x, y, z);
                    var blockWorld = world.LocalToWorld(chunk.Coord, local);

                    if (!world.IsFullCubeBlock(blockWorld) || world.HasChiseledBlock(blockWorld))
                    {
                        continue;
                    }

                    var neighbor = blockWorld + VoxelConstants.NeighborDirs[faceIndex];
                    if (world.IsFaceOccludedByNeighbor(neighbor, faceIndex))
                    {
                        continue;
                    }

                    var blockType = world.GetBlock(blockWorld);
                    var textureSlot = BlockTextureLibrary.GetFaceTextureSlot(blockType, faceIndex);
                    AddSingleBlockFace(faceIndex, local, textureSlot, vertices, triangles, normals, uvs);
                }
            }
        }
    }

    private static void AddSingleBlockFace(
        int faceIndex,
        Vector3Int local,
        BlockTextureSlot textureSlot,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        var x = local.x;
        var y = local.y;
        var z = local.z;
        var xmin = x - 0.5f;
        var xmax = x + 0.5f;
        var ymin = y - 0.5f;
        var ymax = y + 0.5f;
        var zmin = z - 0.5f;
        var zmax = z + 0.5f;

        switch (faceIndex)
        {
            case 0:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                    AtlasTileUv(textureSlot, 0f, 0f), AtlasTileUv(textureSlot, 0f, 1f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 1f, 0f));
                break;
            case 1:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                    AtlasTileUv(textureSlot, 0f, 0f), AtlasTileUv(textureSlot, 0f, 1f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 1f, 0f));
                break;
            case 2:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
                    AtlasTileUv(textureSlot, 0f, 0f), AtlasTileUv(textureSlot, 1f, 0f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 0f, 1f));
                break;
            case 3:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
                    AtlasTileUv(textureSlot, 0f, 0f), AtlasTileUv(textureSlot, 1f, 0f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 0f, 1f));
                break;
            case 4:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                    AtlasTileUv(textureSlot, 1f, 0f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 0f, 1f), AtlasTileUv(textureSlot, 0f, 0f));
                break;
            default:
                AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
                    new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                    AtlasTileUv(textureSlot, 0f, 0f), AtlasTileUv(textureSlot, 0f, 1f), AtlasTileUv(textureSlot, 1f, 1f), AtlasTileUv(textureSlot, 1f, 0f));
                break;
        }
    }

    private static Vector2 AtlasTileUv(BlockTextureSlot slot, float tileU, float tileV)
    {
        return BlockTextureLibrary.GetAtlasUv(slot, tileU, tileV);
    }

    private static Vector2 AtlasUvBlockLocal(int faceIndex, Vector3 blockLocal, BlockTextureSlot slot)
    {
        float tileU;
        float tileV;

        switch (faceIndex)
        {
            case 0:
                tileU = blockLocal.z + 0.5f;
                tileV = blockLocal.y + 0.5f;
                break;
            case 1:
                tileU = 0.5f - blockLocal.z;
                tileV = blockLocal.y + 0.5f;
                break;
            case 2:
                tileU = blockLocal.x + 0.5f;
                tileV = 0.5f - blockLocal.z;
                break;
            case 3:
                tileU = blockLocal.x + 0.5f;
                tileV = blockLocal.z + 0.5f;
                break;
            case 4:
                tileU = blockLocal.x + 0.5f;
                tileV = blockLocal.y + 0.5f;
                break;
            default:
                tileU = blockLocal.x + 0.5f;
                tileV = blockLocal.y + 0.5f;
                break;
        }

        return BlockTextureLibrary.GetAtlasUv(slot, tileU, tileV);
    }

    private static void AddBottomSlabToMesh(
        IVoxelBlockView world,
        Vector3Int blockWorld,
        Vector3Int chunkWorldOrigin,
        VoxelBlockType blockType,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
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

            var textureSlot = BlockTextureLibrary.GetFaceTextureSlot(blockType, face);

            switch (face)
            {
                case 0: // +X
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax) - blockOffset, textureSlot));
                    break;
                case 1: // -X
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin) - blockOffset, textureSlot));
                    break;
                case 2: // +Y
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmin, ymax, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin) - blockOffset, textureSlot));
                    break;
                case 3: // -Y
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax) - blockOffset, textureSlot));
                    break;
                case 4: // +Z
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax) - blockOffset, textureSlot));
                    break;
                default: // -Z
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin) - blockOffset, textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin) - blockOffset, textureSlot));
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
        List<Vector2> uvs)
    {
        switch (blockType)
        {
            case VoxelBlockType.Campfire:
                AddCampfireToMesh(world, blockWorld, chunkWorldOrigin, vertices, triangles, normals, uvs);
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
        List<Vector2> uvs)
    {
        var blockOffset = (Vector3)blockWorld - (Vector3)chunkWorldOrigin;
        var textureSlot = BlockTextureLibrary.GetFaceTextureSlot(VoxelBlockType.Dirt, BlockFace.Top);

        // Ash bed.
        AddTexturedBox(world, blockWorld, blockOffset, -0.30f, 0.30f, -0.50f, -0.38f, -0.30f, 0.30f, textureSlot, vertices, triangles, normals, uvs);
        // Crossed logs.
        AddTexturedBox(world, blockWorld, blockOffset, -0.42f, 0.42f, -0.34f, -0.22f, -0.10f, 0.10f, textureSlot, vertices, triangles, normals, uvs);
        AddTexturedBox(world, blockWorld, blockOffset, -0.10f, 0.10f, -0.22f, -0.10f, -0.42f, 0.42f, textureSlot, vertices, triangles, normals, uvs);
        // Small center pile.
        AddTexturedBox(world, blockWorld, blockOffset, -0.14f, 0.14f, -0.10f, 0.06f, -0.14f, 0.14f, textureSlot, vertices, triangles, normals, uvs);
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
        BlockTextureSlot textureSlot,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs)
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
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmax, ymin, zmin), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmax, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax), textureSlot));
                    break;
                case 1: // -X
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmin, ymin, zmax), blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmin, ymax, zmin), blockOffset + new Vector3(xmin, ymin, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin), textureSlot));
                    break;
                case 2: // +Y
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmin, ymax, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin), textureSlot));
                    break;
                case 3: // -Y
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmin, ymin, zmin), blockOffset + new Vector3(xmax, ymin, zmin), blockOffset + new Vector3(xmax, ymin, zmax), blockOffset + new Vector3(xmin, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax), textureSlot));
                    break;
                case 4: // +Z
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmax, ymin, zmax), blockOffset + new Vector3(xmax, ymax, zmax), blockOffset + new Vector3(xmin, ymax, zmax), blockOffset + new Vector3(xmin, ymin, zmax),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmax), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmax), textureSlot));
                    break;
                default: // -Z
                    AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[face],
                        blockOffset + new Vector3(xmin, ymin, zmin), blockOffset + new Vector3(xmin, ymax, zmin), blockOffset + new Vector3(xmax, ymax, zmin), blockOffset + new Vector3(xmax, ymin, zmin),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymin, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmin, ymax, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymax, zmin), textureSlot),
                        AtlasUvBlockLocal(face, new Vector3(xmax, ymin, zmin), textureSlot));
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
        List<Vector2> uvs)
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

                        AddMicroGreedyFace(face, p, u, v, height, width, resolution, chunkLocalBlockPos, chunkWorldOrigin, block.BlockType, vertices, triangles, normals, uvs);

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
        List<Vector2> uvs)
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

        var textureSlot = BlockTextureLibrary.GetFaceTextureSlot(blockType, faceIndex);

        AddQuad(vertices, triangles, normals, uvs, VoxelConstants.NeighborDirs[faceIndex],
            v0, v1, v2, v3,
            AtlasUvBlockLocal(faceIndex, v0 - blockOffset, textureSlot),
            AtlasUvBlockLocal(faceIndex, v1 - blockOffset, textureSlot),
            AtlasUvBlockLocal(faceIndex, v2 - blockOffset, textureSlot),
            AtlasUvBlockLocal(faceIndex, v3 - blockOffset, textureSlot));
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        Vector3 normal,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector2 uv0,
        Vector2 uv1,
        Vector2 uv2,
        Vector2 uv3)
    {
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
    }
}
