using UnityEngine;

public static class BlockWorldMaterialSetup
{
    public static Material CreateBlockMaterial(BlockTextureRegistry registry, out Texture2D atlasTexture)
    {
        atlasTexture = null;
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("BlockWorldMaterialSetup: URP Lit shader not found.");
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
