using System.Collections.Generic;
using UnityEngine;

internal sealed class ChunkMeshBuildSnapshot
{
    public Vector3Int Coord { get; private set; }
    public int ChunkSize { get; private set; }
    public int ChiselResolution { get; private set; }
    public int MinWorldY { get; private set; }
    public int WorldHeight { get; private set; }
    public VoxelBlockType[] Blocks { get; private set; }
    public Dictionary<Vector3Int, VoxelBlockType> ExternalBlocks { get; private set; }
    public Dictionary<Vector3Int, ChiseledBlockData> ChiseledBlocks { get; private set; }

    public static ChunkMeshBuildSnapshot Capture(IWorldSimulation world, ChunkBlockData chunk)
    {
        var chunkSize = world.ChunkSize;
        var coord = chunk.Coord;
        var externalBlocks = new Dictionary<Vector3Int, VoxelBlockType>();
        var chiseledBlocks = new Dictionary<Vector3Int, ChiseledBlockData>();

        for (int face = 0; face < VoxelConstants.NeighborDirs.Length; face++)
        {
            var neighborDir = VoxelConstants.NeighborDirs[face];
            for (int u = 0; u < chunkSize; u++)
            {
                for (int v = 0; v < chunkSize; v++)
                {
                    var local = FaceSliceToLocal(face, u, v, chunkSize - 1);
                    var worldPosition = world.LocalToWorld(coord, local);
                    var neighborPosition = worldPosition + neighborDir;
                    if (IsInsideChunk(neighborPosition, coord, chunkSize))
                    {
                        continue;
                    }

                    CaptureNeighbor(world, neighborPosition, externalBlocks, chiseledBlocks);
                }
            }
        }

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldPosition = world.LocalToWorld(coord, new Vector3Int(x, y, z));
                    if (world.TryGetChiseledBlock(worldPosition, out var chiseled))
                    {
                        chiseledBlocks[worldPosition] = chiseled.Clone();
                    }
                }
            }
        }

        return new ChunkMeshBuildSnapshot
        {
            Coord = coord,
            ChunkSize = chunkSize,
            ChiselResolution = world.ChiselResolution,
            MinWorldY = world.MinWorldY,
            WorldHeight = world.WorldHeight,
            Blocks = chunk.CopyBlocksToArray(),
            ExternalBlocks = externalBlocks,
            ChiseledBlocks = chiseledBlocks
        };
    }

    private static void CaptureNeighbor(
        IWorldSimulation world,
        Vector3Int neighborPosition,
        Dictionary<Vector3Int, VoxelBlockType> externalBlocks,
        Dictionary<Vector3Int, ChiseledBlockData> chiseledBlocks)
    {
        if (!world.IsInWorld(neighborPosition))
        {
            return;
        }

        var neighborChunk = ChunkGenerationMath.WorldToChunk(neighborPosition, world.ChunkSize);
        if (!world.TryGetChunkBlocks(neighborChunk, out _))
        {
            return;
        }

        externalBlocks[neighborPosition] = world.GetBlock(neighborPosition);

        if (world.TryGetChiseledBlock(neighborPosition, out var chiseled))
        {
            chiseledBlocks[neighborPosition] = chiseled.Clone();
        }
    }

    private static Vector3Int FaceSliceToLocal(int faceIndex, int u, int v, int edge)
    {
        return faceIndex switch
        {
            0 => new Vector3Int(edge, u, v),
            1 => new Vector3Int(0, u, v),
            2 => new Vector3Int(u, edge, v),
            3 => new Vector3Int(u, 0, v),
            4 => new Vector3Int(u, v, edge),
            _ => new Vector3Int(u, v, 0)
        };
    }

    private static bool IsInsideChunk(Vector3Int worldPosition, Vector3Int chunkCoord, int chunkSize)
    {
        var origin = new Vector3Int(
            chunkCoord.x * chunkSize,
            chunkCoord.y * chunkSize,
            chunkCoord.z * chunkSize);
        var local = worldPosition - origin;
        return local.x >= 0 && local.x < chunkSize
            && local.y >= 0 && local.y < chunkSize
            && local.z >= 0 && local.z < chunkSize;
    }
}
