using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BlockAtlasBuilder
{
    public const int DefaultTileSize = 32;

    public static Texture2D Build(Texture2D dirt, Texture2D grassTop, Texture2D grassSide)
    {
        if (dirt == null || grassTop == null || grassSide == null)
        {
            return null;
        }

        var tileWidth = dirt.width;
        var tileHeight = dirt.height;
        var atlas = new Texture2D(tileWidth * 3, tileHeight, TextureFormat.RGBA32, true, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "BlockAtlas_Runtime"
        };

        CopyTile(dirt, atlas, 0, 0, tileWidth, tileHeight);
        CopyTile(grassTop, atlas, tileWidth, 0, tileWidth, tileHeight);
        CopyTile(grassSide, atlas, tileWidth * 2, 0, tileWidth, tileHeight);
        atlas.Apply(true, false);
        return atlas;
    }

    public static Texture2D BuildDynamic(Texture2D[] tiles, out int columns, out int rows)
    {
        columns = 1;
        rows = 1;
        if (tiles == null || tiles.Length == 0 || tiles[0] == null)
        {
            return null;
        }

        columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(tiles.Length)));
        rows = Mathf.Max(1, Mathf.CeilToInt(tiles.Length / (float)columns));

        var tileWidth = tiles[0].width;
        var tileHeight = tiles[0].height;
        var atlas = new Texture2D(tileWidth * columns, tileHeight * rows, TextureFormat.RGBA32, true, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "BlockAtlas_Runtime"
        };

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null)
            {
                continue;
            }

            var column = i % columns;
            var row = i / columns;
            CopyTile(tiles[i], atlas, column * tileWidth, row * tileHeight, tileWidth, tileHeight);
        }

        atlas.Apply(true, false);
        return atlas;
    }

    public static int Columns => BlockTextureRegistry.Active?.AtlasColumns ?? 3;

    public static int Rows => BlockTextureRegistry.Active?.AtlasRows ?? 1;

    public static Texture2D ResolveDefaultDirtTexture(Texture2D assigned)
    {
        if (assigned != null)
        {
            return assigned;
        }

#if UNITY_EDITOR
        var fromAssets = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt.png");
        if (fromAssets != null)
        {
            return fromAssets;
        }
#endif

        return Resources.Load<Texture2D>("Textures/dirt")
            ?? Resources.Load<Texture2D>("test-texture");
    }

    public static Texture2D ResolveDefaultGrassTexture(Texture2D assigned)
    {
        if (assigned != null)
        {
            return assigned;
        }

#if UNITY_EDITOR
        var fromAssets = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass.png");
        if (fromAssets != null)
        {
            return fromAssets;
        }
#endif

        return Resources.Load<Texture2D>("Textures/grass");
    }

    public static Texture2D ResolveDefaultGrassSideTexture(Texture2D assigned)
    {
        if (assigned != null)
        {
            return assigned;
        }

#if UNITY_EDITOR
        var fromAssets = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass_block_side.png");
        if (fromAssets != null)
        {
            return fromAssets;
        }
#endif

        return Resources.Load<Texture2D>("Textures/grass_block_side");
    }

    private static void CopyTile(Texture2D source, Texture2D atlas, int destX, int destY, int tileWidth, int tileHeight)
    {
        var renderTarget = RenderTexture.GetTemporary(tileWidth, tileHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, renderTarget);

        var previous = RenderTexture.active;
        RenderTexture.active = renderTarget;

        var readable = new Texture2D(tileWidth, tileHeight, TextureFormat.RGBA32, false, false);
        readable.ReadPixels(new Rect(0, 0, tileWidth, tileHeight), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        if (RenderTexture.active == renderTarget)
        {
            RenderTexture.active = null;
        }

        RenderTexture.ReleaseTemporary(renderTarget);

        atlas.SetPixels(destX, destY, tileWidth, tileHeight, readable.GetPixels());
        Object.Destroy(readable);
    }
}
