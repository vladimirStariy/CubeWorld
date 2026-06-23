public sealed class ContentCatalog
{
    public ItemRegistry Items { get; } = new();
    public ClayFormingRecipeRegistry ClayRecipes { get; } = new();
    public ItemUseRegistry ItemUse { get; } = new();
    public BlockTextureRegistry BlockTextures { get; } = new();
    public BlockShapeRegistry Shapes { get; } = new();
    public ItemShapeRegistry ItemShapes { get; } = new();
    public BiomeRegistry Biomes { get; } = new();
    public WorldGeneratorRegistry WorldGenerators { get; } = new();
    public WorldSettings WorldSettings { get; set; }
}
