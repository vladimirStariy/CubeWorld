using UnityEngine;

public interface IChunkGenerationContext
{
    WorldSettings Settings { get; }

    ItemRegistry Items { get; }

    BiomeRegistry Biomes { get; }

    ClimateSample SampleClimate(int worldX, int worldZ);

    BiomeDefinition ResolveBiome(ClimateSample climate);

    bool TryResolveBlock(ContentId blockId, out VoxelBlockType blockType);

    void SetBlock(Vector3Int worldPosition, VoxelBlockType blockType);
}
