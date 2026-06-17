using UnityEngine;

public static class BlockWorldMaterialSetup
{
    public static Material CreateBlockMaterial(
        Texture2D dirtTexture,
        Texture2D grassTexture,
        Texture2D prebuiltAtlas,
        out Texture2D atlasTexture)
    {
        atlasTexture = null;
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("BlockWorldMaterialSetup: URP Lit shader not found.");
            return null;
        }

        var dirt = BlockAtlasBuilder.ResolveDefaultDirtTexture(dirtTexture);
        var grass = BlockAtlasBuilder.ResolveDefaultGrassTexture(grassTexture);
        if (dirt == null || grass == null)
        {
            Debug.LogWarning($"BlockWorldMaterialSetup: missing textures (dirt={dirt != null}, grass={grass != null}).");
            return CreateFallbackMaterial(litShader);
        }

        var atlas = prebuiltAtlas != null ? prebuiltAtlas : BlockAtlasBuilder.Build(dirt, grass);
        if (atlas == null)
        {
            Debug.LogWarning("BlockWorldMaterialSetup: block atlas not found, using fallback color.");
            return CreateFallbackMaterial(litShader);
        }

        atlas.wrapMode = TextureWrapMode.Clamp;
        atlas.filterMode = FilterMode.Point;
        atlasTexture = atlas;

        var material = new Material(litShader);
        material.SetTexture("_BaseMap", atlas);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.1f);
        material.SetFloat("_Metallic", 0f);
        BlockTextureLibrary.ApplyFullAtlasToMaterial(material);
        return material;
    }

    private static Material CreateFallbackMaterial(Shader litShader)
    {
        var material = new Material(litShader);
        material.SetColor("_BaseColor", new Color(0.39f, 0.25f, 0.12f));
        return material;
    }
}
