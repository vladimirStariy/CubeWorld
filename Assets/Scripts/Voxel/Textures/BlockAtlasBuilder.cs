using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BlockAtlasBuilder
{
    public const int Columns = 2;
    public const int Rows = 1;
    public const int DefaultTileSize = 32;

    public static Texture2D Build(Texture2D dirt, Texture2D grass)
    {
        if (dirt == null || grass == null)
        {
            return null;
        }

        var tileWidth = dirt.width;
        var tileHeight = dirt.height;
        var atlas = new Texture2D(tileWidth * Columns, tileHeight * Rows, TextureFormat.RGBA32, true, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "BlockAtlas_Runtime"
        };

        CopyTile(dirt, atlas, 0, 0, tileWidth, tileHeight);
        CopyTile(grass, atlas, tileWidth, 0, tileWidth, tileHeight);
        atlas.Apply(true, false);
        return atlas;
    }

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
