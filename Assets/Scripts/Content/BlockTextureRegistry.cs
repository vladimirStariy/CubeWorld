using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class BlockTextureRegistry
{
    public static BlockTextureRegistry Active { get; set; }

    private readonly Dictionary<VoxelBlockType, BlockFaceTextureRegistration> blockFaces = new();
    private readonly List<string> atlasTexturePaths = new();
    private readonly Dictionary<string, int> texturePathToSlot = new(StringComparer.OrdinalIgnoreCase);

    private Texture2D atlas;
    private int atlasColumns = 3;
    private int atlasRows = 1;
    private int tilePixelSize = BlockAtlasBuilder.DefaultTileSize;

    public int AtlasColumns => atlasColumns;
    public int AtlasRows => atlasRows;
    public int TilePixelSize => tilePixelSize;

    public void RegisterBlock(VoxelBlockType blockType, BlockTexturesJson textures, string packDirectory)
    {
        if (textures == null || string.IsNullOrWhiteSpace(packDirectory))
        {
            return;
        }

        var registration = new BlockFaceTextureRegistration
        {
            AllPath = RegisterTextureReference(textures.all, packDirectory),
            TopPath = RegisterTextureReference(textures.top, packDirectory),
            BottomPath = RegisterTextureReference(textures.bottom, packDirectory),
            SidePath = RegisterTextureReference(textures.side, packDirectory)
        };

        if (!registration.HasAnyPath)
        {
            return;
        }

        blockFaces[blockType] = registration;
    }

    public bool BuildAtlas()
    {
        if (atlasTexturePaths.Count == 0)
        {
            Debug.LogWarning("BlockTextureRegistry: no block textures registered.");
            return false;
        }

        var tiles = new Texture2D[atlasTexturePaths.Count];
        for (int i = 0; i < atlasTexturePaths.Count; i++)
        {
            if (!ContentTextureLoader.TryLoadTextureFile(atlasTexturePaths[i], out tiles[i]))
            {
                Debug.LogError($"BlockTextureRegistry: failed to load {atlasTexturePaths[i]}");
                return false;
            }
        }

        atlas = BlockAtlasBuilder.BuildDynamic(tiles, out atlasColumns, out atlasRows, out var builtTileWidth, out _);
        tilePixelSize = builtTileWidth > 0 ? builtTileWidth : BlockAtlasBuilder.DefaultTileSize;
        if (atlas == null)
        {
            return false;
        }

        for (int i = 0; i < atlasTexturePaths.Count; i++)
        {
            texturePathToSlot[atlasTexturePaths[i]] = i;
        }

        return true;
    }

    public bool TryGetAtlas(out Texture2D atlasTexture)
    {
        atlasTexture = atlas;
        return atlas != null;
    }

    public int GetAtlasSlot(VoxelBlockType blockType, BlockFace face)
    {
        if (blockFaces.TryGetValue(blockType, out var registration))
        {
            var path = registration.Resolve(face);
            if (!string.IsNullOrEmpty(path) && texturePathToSlot.TryGetValue(path, out var slot))
            {
                return slot;
            }
        }

        if (blockFaces.TryGetValue(VoxelBlockType.Dirt, out var dirtRegistration))
        {
            var dirtPath = dirtRegistration.Resolve(face);
            if (!string.IsNullOrEmpty(dirtPath) && texturePathToSlot.TryGetValue(dirtPath, out var dirtSlot))
            {
                return dirtSlot;
            }
        }

        return 0;
    }

    private string RegisterTextureReference(string fileName, string packDirectory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(packDirectory, "textures", "blocks", fileName.Trim()));
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Block texture not found: {path}");
            return null;
        }

        if (!atlasTexturePaths.Contains(path))
        {
            atlasTexturePaths.Add(path);
        }

        return path;
    }

    private sealed class BlockFaceTextureRegistration
    {
        public string AllPath;
        public string TopPath;
        public string BottomPath;
        public string SidePath;

        public bool HasAnyPath =>
            !string.IsNullOrEmpty(AllPath)
            || !string.IsNullOrEmpty(TopPath)
            || !string.IsNullOrEmpty(BottomPath)
            || !string.IsNullOrEmpty(SidePath);

        public string Resolve(BlockFace face)
        {
            return face switch
            {
                BlockFace.Top => TopPath ?? AllPath,
                BlockFace.Bottom => BottomPath ?? AllPath,
                _ => SidePath ?? AllPath
            };
        }
    }
}
