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

    public static AtlasTile GetAtlasTile(int slotIndex)
    {
        var columns = BlockAtlasBuilder.Columns;
        var rows = BlockAtlasBuilder.Rows;
        var tileWidth = 1f / columns;
        var tileHeight = 1f / rows;
        var column = slotIndex % columns;
        var row = slotIndex / columns;
        return new AtlasTile(new Vector2(column * tileWidth, row * tileHeight), new Vector2(tileWidth, tileHeight));
    }

    public static AtlasTile GetTile(VoxelBlockType blockType, BlockFace face)
    {
        return GetAtlasTile(GetFaceAtlasSlot(blockType, face));
    }

    public static int GetFaceAtlasSlot(VoxelBlockType blockType, BlockFace face)
    {
        if (BlockTextureRegistry.Active != null)
        {
            return BlockTextureRegistry.Active.GetAtlasSlot(blockType, face);
        }

        return GetLegacyFaceAtlasSlot(blockType, face);
    }

    public static int GetFaceAtlasSlot(VoxelBlockType blockType, int faceIndex)
    {
        return GetFaceAtlasSlot(blockType, FaceIndexToBlockFace(faceIndex));
    }

    private static int GetLegacyFaceAtlasSlot(VoxelBlockType blockType, BlockFace face)
    {
        if (blockType == VoxelBlockType.GrassBlock)
        {
            return face switch
            {
                BlockFace.Top => 1,
                BlockFace.Bottom => 0,
                _ => 2
            };
        }

        return 0;
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
    public static Vector2 GetAtlasUv(int atlasSlot, float tileU, float tileV)
    {
        var tile = GetAtlasTile(atlasSlot);
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
