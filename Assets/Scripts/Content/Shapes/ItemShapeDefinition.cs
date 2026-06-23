using System.Collections.Generic;
using UnityEngine;

public sealed class ItemShapeDefinition
{
    public ItemShapeDefinition(
        ContentId id,
        int textureWidth,
        int textureHeight,
        IReadOnlyDictionary<string, string> texturePaths,
        IReadOnlyList<ItemShapePart> parts,
        Bounds localBounds)
    {
        Id = id;
        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        TexturePaths = texturePaths;
        Parts = parts;
        LocalBounds = localBounds;
    }

    public ContentId Id { get; }
    public int TextureWidth { get; }
    public int TextureHeight { get; }
    public IReadOnlyDictionary<string, string> TexturePaths { get; }
    public IReadOnlyList<ItemShapePart> Parts { get; }
    public Bounds LocalBounds { get; }

    public int PartCount => Parts.Count;
}

public sealed class ItemShapePart
{
    public ItemShapePart(string name, IReadOnlyList<ItemShapeFace> faces)
    {
        Name = name;
        Faces = faces;
    }

    public string Name { get; }
    public IReadOnlyList<ItemShapeFace> Faces { get; }
}

public readonly struct ItemShapeFace
{
    public ItemShapeFace(Vector3[] corners, Vector2[] uvs, string textureKey)
    {
        Corners = corners;
        Uvs = uvs;
        TextureKey = textureKey;
    }

    public Vector3[] Corners { get; }
    public Vector2[] Uvs { get; }
    public string TextureKey { get; }
}
