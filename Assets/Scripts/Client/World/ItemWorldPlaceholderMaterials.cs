using System.Collections.Generic;
using UnityEngine;

public static class ItemWorldPlaceholderMaterials
{
    private static readonly Dictionary<ItemKind, Material> Materials = new();

    public static Material Get(ItemKind kind)
    {
        if (Materials.TryGetValue(kind, out var existing) && existing != null)
        {
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            return null;
        }

        var material = new Material(shader);
        var color = ItemPreviewMeshBuilder.GetPreviewColor(kind);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        Materials[kind] = material;
        return material;
    }
}
