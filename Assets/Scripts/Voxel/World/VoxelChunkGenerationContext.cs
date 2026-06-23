using UnityEngine;

internal sealed class VoxelChunkGenerationContext : IChunkGenerationContext
{
    private readonly WorldSimulation storage;
    private readonly ChunkBlockData targetChunk;

    public VoxelChunkGenerationContext(
        WorldSimulation storage,
        ChunkBlockData targetChunk,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes)
    {
        this.storage = storage;
        this.targetChunk = targetChunk;
        Settings = settings;
        Items = items;
        Biomes = biomes;
    }

    public WorldSettings Settings { get; }

    public ItemRegistry Items { get; }

    public BiomeRegistry Biomes { get; }

    public ClimateSample SampleClimate(int worldX, int worldZ) =>
        LatitudeClimateModel.Sample(worldX, worldZ, Settings);

    public BiomeDefinition ResolveBiome(ClimateSample climate) => Biomes.Resolve(climate);

    public bool TryResolveBlock(ContentId blockId, out VoxelBlockType blockType)
    {
        blockType = VoxelBlockType.Air;
        if (Items != null && Items.TryGet(blockId, out var definition))
        {
            blockType = definition.RuntimeBlockType;
            return blockType != VoxelBlockType.Air;
        }

        return false;
    }

    public void SetBlock(Vector3Int worldPosition, VoxelBlockType blockType)
    {
        storage.SetBlockForGeneration(worldPosition, blockType, targetChunk);
    }
}
