using UnityEngine;

public sealed class FlatWorldGenerator : IChunkWorldGenerator
{
    public static readonly ContentId GeneratorId = new("cubeworld", "flat");

    public ContentId Id => GeneratorId;

    private readonly ContentId surfaceBlockId;

    public FlatWorldGenerator(ContentId surfaceBlockId)
    {
        this.surfaceBlockId = surfaceBlockId;
    }

    public void GenerateChunk(Vector3Int chunkCoord, IChunkGenerationContext context)
    {
        if (!context.TryResolveBlock(surfaceBlockId, out var surfaceBlock))
        {
            surfaceBlock = VoxelBlockType.GrassBlock;
        }

        var settings = context.Settings;
        var chunkSize = 16;
        var origin = new Vector3Int(
            chunkCoord.x * chunkSize,
            chunkCoord.y * chunkSize,
            chunkCoord.z * chunkSize);

        for (int localZ = 0; localZ < chunkSize; localZ++)
        {
            for (int localX = 0; localX < chunkSize; localX++)
            {
                var worldX = origin.x + localX;
                var worldZ = origin.z + localZ;

                context.SetBlock(new Vector3Int(worldX, settings.BaseLayerY, worldZ), surfaceBlock);
            }
        }
    }
}
