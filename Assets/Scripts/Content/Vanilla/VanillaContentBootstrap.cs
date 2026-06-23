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
        BlockShapeRegistry.Active = catalog.Shapes;
        BlockShapeLibrary.Active = catalog.Shapes;
        ItemShapeRegistry.Active = catalog.ItemShapes;
        BlockShapeLibrary.RegisterBlockShape(VoxelBlockType.Campfire, ContentJsonParser.ResolveShapeId(null, VoxelBlockType.Campfire));
    }
}
