using UnityEngine;

public static class VanillaContentBootstrap
{
    public static void RegisterAll(ContentCatalog catalog)
    {
        JsonContentLoader.LoadAllPacks(catalog);

        if (!catalog.BlockTextures.BuildAtlas())
        {
            Debug.LogError("Failed to build block texture atlas from content packs.");
        }

        if (catalog.Items.CreativeEntries.Count == 0)
        {
            Debug.LogError(
                "No items loaded from JSON. Expected files at StreamingAssets/Content/<pack>/blocks/*.json and items/*.json");
        }

        catalog.ItemUse.Register(new CampfireItemUseProvider());

        ItemRegistry.Active = catalog.Items;
        ClayFormingRecipeRegistry.Active = catalog.ClayRecipes;
        BlockTextureRegistry.Active = catalog.BlockTextures;
    }

    /// <summary>
    /// Mods implement <see cref="IContentRegistrar"/> for code-only extensions,
    /// Mods drop a folder under StreamingAssets/Content/ with blocks/, items/, recipes/clay/, textures/.
    /// </summary>
    public static void RegisterMod(IContentRegistrar registrar, ContentCatalog catalog)
    {
        registrar?.Register(catalog);
    }
}
