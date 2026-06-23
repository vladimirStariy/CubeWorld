using UnityEngine;

internal sealed class DetachedChunkGenerationContext : IChunkGenerationContext
{
    private readonly Vector3Int chunkCoord;
    private readonly int chunkSize;
    private readonly VoxelBlockType[] blocks;
    private readonly FluidCell[] fluids;

    public DetachedChunkGenerationContext(
        Vector3Int chunkCoord,
        int chunkSize,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes,
        VoxelBlockType[] blocks,
        FluidCell[] fluids)
    {
        this.chunkCoord = chunkCoord;
        this.chunkSize = chunkSize;
        this.blocks = blocks;
        this.fluids = fluids;
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
        if (worldPosition.y < Settings.BaseLayerY || worldPosition.y >= Settings.Height)
        {
            return;
        }

        if (ChunkGenerationMath.WorldToChunk(worldPosition, chunkSize) != chunkCoord)
        {
            return;
        }

        var local = ChunkGenerationMath.WorldToLocal(worldPosition, chunkSize);
        blocks[ChunkGenerationMath.ToIndex(local, chunkSize)] = blockType;
    }

    public void SetFluid(Vector3Int worldPosition, FluidCell fluid)
    {
        if (fluid.IsEmpty || worldPosition.y < Settings.BaseLayerY || worldPosition.y >= Settings.Height)
        {
            return;
        }

        if (ChunkGenerationMath.WorldToChunk(worldPosition, chunkSize) != chunkCoord)
        {
            return;
        }

        var local = ChunkGenerationMath.WorldToLocal(worldPosition, chunkSize);
        var index = ChunkGenerationMath.ToIndex(local, chunkSize);
        if (blocks[index] != VoxelBlockType.Air)
        {
            return;
        }

        fluids[index] = fluid;
    }
}
