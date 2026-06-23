using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ItemShapeMaterials
{
    private static readonly Dictionary<string, Material> ByTexturePath = new();
    private static readonly Dictionary<ItemKind, Material> FallbackByKind = new();

    public static Material GetForShape(ItemShapeDefinition definition, ItemKind fallbackKind)
    {
        if (definition == null)
        {
            return GetFallback(fallbackKind);
        }

        var texturePath = ResolvePrimaryTexturePath(definition);
        if (string.IsNullOrEmpty(texturePath))
        {
            return GetFallback(fallbackKind);
        }

        if (ByTexturePath.TryGetValue(texturePath, out var cached) && cached != null)
        {
            return cached;
        }

        if (!ContentTextureLoader.TryLoadTextureFile(texturePath, out var texture))
        {
            return GetFallback(fallbackKind);
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Unlit/Color");
        if (shader == null)
        {
            return GetFallback(fallbackKind);
        }

        var material = new Material(shader);
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        ByTexturePath[texturePath] = material;
        return material;
    }

    private static string ResolvePrimaryTexturePath(ItemShapeDefinition definition)
    {
        for (int i = 0; i < definition.Parts.Count; i++)
        {
            var faces = definition.Parts[i].Faces;
            for (int f = 0; f < faces.Count; f++)
            {
                var key = faces[f].TextureKey;
                if (!string.IsNullOrEmpty(key)
                    && definition.TexturePaths.TryGetValue(key, out var path)
                    && !string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
        }

        foreach (var path in definition.TexturePaths.Values)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
        }

        return null;
    }

    private static Material GetFallback(ItemKind kind)
    {
        if (FallbackByKind.TryGetValue(kind, out var cached) && cached != null)
        {
            return cached;
        }

        return ItemWorldPlaceholderMaterials.Get(kind);
    }
}
