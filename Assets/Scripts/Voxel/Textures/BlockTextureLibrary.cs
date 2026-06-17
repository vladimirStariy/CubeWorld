using UnityEngine;

public enum BlockFace
{
    Top,
    Bottom,
    North,
    South,
    East,
    West
}

public enum BlockTextureSlot : byte
{
    Dirt = 0,
    GrassTop = 1
}

/// <summary>
/// Block face → atlas tile mapping. Mesh UVs stay within one atlas tile per face.
/// </summary>
public static class BlockTextureLibrary
{
    public readonly struct AtlasTile
    {
        public readonly Vector2 Offset;
        public readonly Vector2 Size;

        public AtlasTile(Vector2 offset, Vector2 size)
        {
            Offset = offset;
            Size = size;
        }

        public static AtlasTile FullTexture => new AtlasTile(Vector2.zero, Vector2.one);
    }

    public static AtlasTile GetAtlasTile(BlockTextureSlot slot)
    {
        var tileWidth = 1f / BlockAtlasBuilder.Columns;
        var tileHeight = 1f / BlockAtlasBuilder.Rows;
        var column = (int)slot % BlockAtlasBuilder.Columns;
        var row = (int)slot / BlockAtlasBuilder.Columns;
        return new AtlasTile(new Vector2(column * tileWidth, row * tileHeight), new Vector2(tileWidth, tileHeight));
    }

    public static AtlasTile GetTile(VoxelBlockType blockType, BlockFace face)
    {
        return GetAtlasTile(GetFaceTextureSlot(blockType, face));
    }

    public static BlockTextureSlot GetFaceTextureSlot(VoxelBlockType blockType, BlockFace face)
    {
        if (blockType == VoxelBlockType.GrassBlock && face == BlockFace.Top)
        {
            return BlockTextureSlot.GrassTop;
        }

        return BlockTextureSlot.Dirt;
    }

    public static BlockTextureSlot GetFaceTextureSlot(VoxelBlockType blockType, int faceIndex)
    {
        return GetFaceTextureSlot(blockType, FaceIndexToBlockFace(faceIndex));
    }

    public static BlockFace FaceIndexToBlockFace(int faceIndex)
    {
        return faceIndex switch
        {
            0 => BlockFace.East,
            1 => BlockFace.West,
            2 => BlockFace.Top,
            3 => BlockFace.Bottom,
            4 => BlockFace.North,
            5 => BlockFace.South,
            _ => BlockFace.Top
        };
    }

    /// <summary>
    /// Map 0..1 tile coordinates into atlas UV space, inset by half a texel to avoid bleeding.
    /// </summary>
    public static Vector2 GetAtlasUv(BlockTextureSlot textureSlot, float tileU, float tileV)
    {
        var tile = GetAtlasTile(textureSlot);
        var inset = 0.5f / BlockAtlasBuilder.DefaultTileSize;
        tileU = Mathf.Lerp(inset, 1f - inset, tileU);
        tileV = Mathf.Lerp(inset, 1f - inset, tileV);
        return tile.Offset + new Vector2(tileU * tile.Size.x, tileV * tile.Size.y);
    }

    public static void ApplyTileToMaterial(Material material, AtlasTile tile)
    {
        if (material == null)
        {
            return;
        }

        material.SetTextureScale("_BaseMap", tile.Size);
        material.SetTextureOffset("_BaseMap", tile.Offset);
        material.SetTextureScale("_MainTex", tile.Size);
        material.SetTextureOffset("_MainTex", tile.Offset);
    }

    public static void ApplyFullAtlasToMaterial(Material material)
    {
        ApplyTileToMaterial(material, AtlasTile.FullTexture);
    }
}
