using System.Collections.Generic;
using UnityEngine;

public static class FluidMeshBuilder
{
    public static void BuildFluidMesh(
        IVoxelBlockView world,
        IVoxelFluidView fluidView,
        ChunkBlockData blocks,
        ChunkMeshScratch scratch)
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

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var local = new Vector3Int(x, y, z);
                    var fluid = blocks.GetFluid(local);
                    if (fluid.IsEmpty)
                    {
                        continue;
                    }

                    var blockWorld = world.LocalToWorld(blocks.Coord, local);
                    var blockOffset = (Vector3)blockWorld - (Vector3)chunkWorldOrigin;
                    var fillHeight = fluid.GetFillHeight();
                    var ymin = blockOffset.y - 0.5f;
                    var ymax = ymin + fillHeight;
                    var xmin = blockOffset.x - 0.5f;
                    var xmax = blockOffset.x + 0.5f;
                    var zmin = blockOffset.z - 0.5f;
                    var zmax = blockOffset.z + 0.5f;

                    if (ShouldRenderFluidTop(fluidView, blockWorld, fluid))
                    {
                        AddFluidFace(
                            2,
                            fluid.Type,
                            new Vector3(xmin, ymax, zmax),
                            new Vector3(xmax, ymax, zmax),
                            new Vector3(xmax, ymax, zmin),
                            new Vector3(xmin, ymax, zmin),
                            vertices,
                            triangles,
                            normals,
                            uvs,
                            tileRects,
                            tileCounts);
                    }

                    for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
                    {
                        if (face is 2 or 3)
                        {
                            continue;
                        }

                        if (!ShouldRenderFluidSide(world, fluidView, blockWorld, face, fluid))
                        {
                            continue;
                        }

                        switch (face)
                        {
                            case 0:
                                AddFluidFace(face, fluid.Type,
                                    new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax),
                                    vertices, triangles, normals, uvs, tileRects, tileCounts);
                                break;
                            case 1:
                                AddFluidFace(face, fluid.Type,
                                    new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymin, zmin),
                                    vertices, triangles, normals, uvs, tileRects, tileCounts);
                                break;
                            case 4:
                                AddFluidFace(face, fluid.Type,
                                    new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax),
                                    vertices, triangles, normals, uvs, tileRects, tileCounts);
                                break;
                            default:
                                AddFluidFace(face, fluid.Type,
                                    new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin),
                                    vertices, triangles, normals, uvs, tileRects, tileCounts);
                                break;
                        }
                    }
                }
            }
        }
    }

    private static bool ShouldRenderFluidTop(IVoxelFluidView fluidView, Vector3Int blockWorld, FluidCell fluid)
    {
        var above = fluidView.GetFluid(blockWorld + Vector3Int.up);
        if (above.IsEmpty)
        {
            return true;
        }

        return above.GetFillHeight() < fluid.GetFillHeight() - 0.01f;
    }

    private static bool ShouldRenderFluidSide(
        IVoxelBlockView world,
        IVoxelFluidView fluidView,
        Vector3Int blockWorld,
        int faceIndex,
        FluidCell fluid)
    {
        var neighborPos = blockWorld + VoxelConstants.NeighborDirs[faceIndex];
        if (world.IsInWorld(neighborPos))
        {
            var neighborSolid = world.GetBlock(neighborPos);
            if (neighborSolid != VoxelBlockType.Air && VoxelBlockShapes.IsFullCube(neighborSolid))
            {
                return false;
            }
        }

        var neighborFluid = fluidView.GetFluid(neighborPos);
        if (neighborFluid.IsEmpty)
        {
            return true;
        }

        return neighborFluid.GetFillHeight() < fluid.GetFillHeight() - 0.001f;
    }

    private static void AddFluidFace(
        int faceIndex,
        FluidType fluidType,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Vector4> tileRects,
        List<Vector2> tileCounts)
    {
        var textureSlot = FluidTextureLibrary.GetAtlasSlot(fluidType, faceIndex);
        var tileRect = BlockTextureLibrary.GetAtlasTileRect(textureSlot);
        var normal = VoxelConstants.NeighborDirs[faceIndex];
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

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, 0f));

        tileRects.Add(tileRect);
        tileRects.Add(tileRect);
        tileRects.Add(tileRect);
        tileRects.Add(tileRect);

        tileCounts.Add(Vector2.one);
        tileCounts.Add(Vector2.one);
        tileCounts.Add(Vector2.one);
        tileCounts.Add(Vector2.one);
    }
}
