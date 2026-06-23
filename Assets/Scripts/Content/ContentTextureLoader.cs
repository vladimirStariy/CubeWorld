using System.IO;
using UnityEngine;

public static class ContentTextureLoader
{
    public static bool TryLoadTextureFile(string filePath, out Texture2D texture)
    {
        texture = null;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = Path.GetFileName(filePath)
            };

            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                texture = null;
                return false;
            }

            return true;
        }
        catch (IOException ex)
        {
            Debug.LogError($"Failed to load texture {filePath}: {ex.Message}");
            return false;
        }
    }
}
