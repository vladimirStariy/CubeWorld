using UnityEngine;

public sealed class PlanetWorldGenerator : IChunkWorldGenerator
{
    public static readonly ContentId GeneratorId = new("cubeworld", "planet");

    public ContentId Id => GeneratorId;

    public void GenerateChunk(Vector3Int chunkCoord, IChunkGenerationContext context)
    {
        var settings = context.Settings;
        var chunkSize = 16;
        var origin = new Vector3Int(
            chunkCoord.x * chunkSize,
            chunkCoord.y * chunkSize,
            chunkCoord.z * chunkSize);
        var surfaceHeights = new int[chunkSize, chunkSize];

        for (int localZ = 0; localZ < chunkSize; localZ++)
        {
            for (int localX = 0; localX < chunkSize; localX++)
            {
                var worldX = origin.x + localX;
                var worldZ = origin.z + localZ;

                var climate = context.SampleClimate(worldX, worldZ);
                var biome = context.ResolveBiome(climate);
                if (!context.TryResolveBlock(biome.SurfaceBlockId, out var surfaceBlock)
                    || !context.TryResolveBlock(biome.SubsurfaceBlockId, out var subsurfaceBlock)
                    || !context.TryResolveBlock(biome.FillerBlockId, out var fillerBlock))
                {
                    surfaceBlock = VoxelBlockType.GrassBlock;
                    subsurfaceBlock = VoxelBlockType.Dirt;
                    fillerBlock = VoxelBlockType.Dirt;
                }

                var surfaceY = ComputeSurfaceHeight(worldX, worldZ, settings, climate);
                surfaceHeights[localX, localZ] = surfaceY;
                var underwater = surfaceY < settings.SeaLevel;
                for (int worldY = settings.BaseLayerY; worldY <= surfaceY; worldY++)
                {
                    if (worldY < origin.y || worldY >= origin.y + chunkSize)
                    {
                        continue;
                    }

                    var blockType = worldY == surfaceY
                        ? underwater ? subsurfaceBlock : surfaceBlock
                        : worldY >= surfaceY - 2
                            ? subsurfaceBlock
                            : fillerBlock;
                    context.SetBlock(new Vector3Int(worldX, worldY, worldZ), blockType);
                }
            }
        }

        for (int localZ = 0; localZ < chunkSize; localZ++)
        {
            for (int localX = 0; localX < chunkSize; localX++)
            {
                OceanGenerationHelper.FillOceanColumn(
                    context,
                    origin.x + localX,
                    origin.z + localZ,
                    surfaceHeights[localX, localZ],
                    origin.y,
                    chunkSize);
            }
        }
    }

    private static int ComputeSurfaceHeight(int worldX, int worldZ, WorldSettings settings, ClimateSample climate)
    {
        var noise = GenerationNoise.Perlin(
            (worldX + settings.Seed) * settings.TerrainNoiseScale,
            (worldZ + settings.Seed) * settings.TerrainNoiseScale);
        var variation = GenerationNoise.RoundToInt((noise - 0.5f) * 2f * settings.TerrainHeightVariation);
        var surfaceY = settings.SeaLevel + variation;

        if (climate.Temperature < 0f)
        {
            surfaceY += 1;
        }

        return (int)GenerationNoise.Clamp(surfaceY, settings.BaseLayerY + 1, settings.Height - 2);
    }
}
