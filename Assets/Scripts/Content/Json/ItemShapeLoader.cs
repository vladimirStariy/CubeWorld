using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ItemShapeLoader
{
    private const string ShapesFolderName = "shapes";

    public static int LoadPackShapes(string packDirectory, ItemShapeRegistry registry)
    {
        if (registry == null || string.IsNullOrWhiteSpace(packDirectory))
        {
            return 0;
        }

        var shapesRoot = Path.Combine(packDirectory, ShapesFolderName);
        if (!Directory.Exists(shapesRoot))
        {
            return 0;
        }

        var categories = Directory.GetDirectories(shapesRoot);
        System.Array.Sort(categories, string.CompareOrdinal);

        var loaded = 0;
        for (int i = 0; i < categories.Length; i++)
        {
            loaded += LoadShapeCategory(categories[i], packDirectory, registry);
        }

        return loaded;
    }

    private static int LoadShapeCategory(string categoryDirectory, string packDirectory, ItemShapeRegistry registry)
    {
        if (!Directory.Exists(categoryDirectory))
        {
            return 0;
        }

        var files = Directory.GetFiles(categoryDirectory, "*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        System.Array.Sort(files, string.CompareOrdinal);
        var packName = Path.GetFileName(packDirectory);
        var category = Path.GetFileName(categoryDirectory);
        var loaded = 0;

        for (int i = 0; i < files.Length; i++)
        {
            var fileName = Path.GetFileName(files[i]);
            if (ShouldSkipFile(fileName))
            {
                continue;
            }

            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            Dictionary<string, object> root;
            try
            {
                root = ContentJsonReader.ParseObject(jsonText);
            }
            catch (System.FormatException ex)
            {
                Debug.LogError($"Shape {category}/{fileName}: {ex.Message}");
                continue;
            }

            if (!TryResolveShapeId(root, packName, category, fileName, out var shapeId, out var error))
            {
                Debug.LogError($"Shape {category}/{fileName}: {error}");
                continue;
            }

            if (!TryBuildDefinition(root, shapeId, packDirectory, out var definition, out error))
            {
                Debug.LogError($"Shape {shapeId}: {error}");
                continue;
            }

            registry.Register(definition);
            loaded++;
        }

        return loaded;
    }

    private static bool ShouldSkipFile(string fileName)
    {
        return fileName.Contains("_old", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveShapeId(
        Dictionary<string, object> root,
        string packName,
        string category,
        string fileName,
        out ContentId shapeId,
        out string error)
    {
        error = null;
        if (root.TryGetValue("id", out var idValue) && idValue is string idText && ContentId.TryParse(idText, out shapeId))
        {
            return true;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            error = "Missing shape id.";
            shapeId = default;
            return false;
        }

        shapeId = new ContentId(packName, $"{category}/{stem}");
        return true;
    }

    private static bool TryBuildDefinition(
        Dictionary<string, object> root,
        ContentId shapeId,
        string packDirectory,
        out ItemShapeDefinition definition,
        out string error)
    {
        if (IsEditorShape(root))
        {
            return ItemShapeTreeBuilder.TryBuild(root, shapeId, packDirectory, out definition, out error);
        }

        return ItemShapeFlatBuilder.TryBuild(root, shapeId, out definition, out error);
    }

    private static bool IsEditorShape(Dictionary<string, object> root)
    {
        if (root.ContainsKey("textureWidth") || root.ContainsKey("textures"))
        {
            return true;
        }

        return ContainsNestedElements(root);
    }

    private static bool ContainsNestedElements(Dictionary<string, object> root)
    {
        if (!root.TryGetValue("elements", out var elementsObj) || elementsObj is not List<object> elements)
        {
            return false;
        }

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is Dictionary<string, object> element
                && element.ContainsKey("children"))
            {
                return true;
            }

            if (elements[i] is Dictionary<string, object> withFaces
                && withFaces.ContainsKey("faces"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadJson(string filePath, out string jsonText)
    {
        jsonText = null;
        try
        {
            jsonText = File.ReadAllText(filePath);
            return true;
        }
        catch (IOException ex)
        {
            Debug.LogError($"Failed to read item shape JSON {filePath}: {ex.Message}");
            return false;
        }
    }
}
