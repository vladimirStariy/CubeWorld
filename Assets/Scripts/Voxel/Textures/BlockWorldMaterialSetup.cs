using UnityEngine;

public static class BlockWorldMaterialSetup
{
    public static Material CreateBlockMaterial(BlockTextureRegistry registry, out Texture2D atlasTexture)
    {
        atlasTexture = null;
        var blockAtlasShader = Shader.Find("CubeWorld/BlockAtlasLit");
        if (blockAtlasShader == null)
        {
            Debug.LogError(
                "BlockWorldMaterialSetup: CubeWorld/BlockAtlasLit shader not found or failed to compile. " +
                "Block textures require this shader. Check Assets/Shaders/BlockAtlasLit.shader in the Console.");
            return null;
        }

        var litShader = blockAtlasShader;
        if (litShader == null)
        {
            Debug.LogError("BlockWorldMaterialSetup: block atlas shader not found.");
            return null;
        }

        if (registry == null || !registry.TryGetAtlas(out atlasTexture))
        {
            Debug.LogWarning("BlockWorldMaterialSetup: block atlas not built from content packs, using fallback color.");
            return CreateFallbackMaterial(litShader);
        }

        atlasTexture.wrapMode = TextureWrapMode.Clamp;
        atlasTexture.filterMode = FilterMode.Point;

        var material = new Material(litShader);
        material.SetTexture("_BaseMap", atlasTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_TilePixelSize", registry.TilePixelSize);
        return material;
    }

    public static Material CreateFluidMaterial(BlockTextureRegistry registry, out Texture2D atlasTexture)
    {
        atlasTexture = null;
        var fluidShader = Shader.Find("CubeWorld/BlockAtlasFluid");
        if (fluidShader == null)
        {
            Debug.LogError("BlockWorldMaterialSetup: CubeWorld/BlockAtlasFluid shader not found.");
            return null;
        }

        if (registry == null || !registry.TryGetAtlas(out atlasTexture))
        {
            var material = new Material(fluidShader);
            material.SetColor("_FluidColor", FluidTextureLibrary.GetTint(FluidType.SaltWater));
            material.SetFloat("_TilePixelSize", BlockAtlasBuilder.DefaultTileSize);
            return material;
        }

        atlasTexture.wrapMode = TextureWrapMode.Clamp;
        atlasTexture.filterMode = FilterMode.Point;

        var fluidMaterial = new Material(fluidShader);
        fluidMaterial.SetTexture("_BaseMap", atlasTexture);
        fluidMaterial.SetColor("_FluidColor", FluidTextureLibrary.GetTint(FluidType.SaltWater));
        fluidMaterial.SetFloat("_TilePixelSize", registry.TilePixelSize);
        return fluidMaterial;
    }

    private static Material CreateFallbackMaterial(Shader litShader)
    {
        var material = new Material(litShader);
        material.SetColor("_BaseColor", new Color(0.39f, 0.25f, 0.12f));
        material.SetFloat("_TilePixelSize", BlockAtlasBuilder.DefaultTileSize);
        return material;
    }
}
