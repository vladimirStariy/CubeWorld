public sealed class ContentCatalog
{
    public ItemRegistry Items { get; } = new();
    public ClayFormingRecipeRegistry ClayRecipes { get; } = new();
    public ItemUseRegistry ItemUse { get; } = new();
    public BlockTextureRegistry BlockTextures { get; } = new();
}
