using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class JsonContentLoader
{
    private const string ContentFolderName = "Content";
    private const string BlocksFolderName = "blocks";
    private const string ItemsFolderName = "items";
    private const string RecipesFolderName = "recipes";
    private const string ClayRecipesFolderName = "clay";
    private const string VanillaPackFolderName = "cubeworld";

    public static void LoadAllPacks(ContentCatalog catalog)
    {
        if (catalog == null)
        {
            Debug.LogError("JsonContentLoader: content catalog is null.");
            return;
        }

        var contentRoot = Path.Combine(Application.streamingAssetsPath, ContentFolderName);
        if (!Directory.Exists(contentRoot))
        {
            Debug.LogWarning($"JsonContentLoader: content folder not found at {contentRoot}. No JSON content loaded.");
            return;
        }

        var packDirectories = new List<string>(Directory.GetDirectories(contentRoot));
        packDirectories.Sort(ComparePackLoadOrder);

        var loadedAny = false;
        for (int i = 0; i < packDirectories.Count; i++)
        {
            if (TryLoadPackDirectory(packDirectories[i], catalog))
            {
                loadedAny = true;
            }
        }

        if (!loadedAny)
        {
            Debug.LogWarning("JsonContentLoader: no content packs were loaded.");
        }
    }

    public static bool TryLoadPackDirectory(string packDirectory, ContentCatalog catalog)
    {
        var packName = Path.GetFileName(packDirectory);
        var blockCount = LoadBlockFiles(Path.Combine(packDirectory, BlocksFolderName), catalog, packDirectory, packName);
        var itemCount = LoadItemFiles(Path.Combine(packDirectory, ItemsFolderName), catalog.Items, packName, ItemsFolderName);
        var recipeCount = LoadRecipeFiles(Path.Combine(packDirectory, RecipesFolderName), catalog, packName);

        var total = blockCount + itemCount + recipeCount;
        if (total > 0)
        {
            Debug.Log(
                $"Loaded content pack '{packName}': {blockCount} block(s), {itemCount} item(s), {recipeCount} recipe(s).");
            return true;
        }

        Debug.LogWarning(
            $"Content pack '{packName}' is empty. Expected JSON in {BlocksFolderName}/, {ItemsFolderName}/, {RecipesFolderName}/{ClayRecipesFolderName}/.");
        return false;
    }

    private static int LoadBlockFiles(string directory, ContentCatalog catalog, string packDirectory, string packName)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        System.Array.Sort(files, string.CompareOrdinal);
        var registered = 0;

        for (int i = 0; i < files.Length; i++)
        {
            var fileName = Path.GetFileName(files[i]);
            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            var json = JsonUtility.FromJson<ItemJson>(jsonText);
            if (!ContentJsonParser.TryParseItem(json, out var definition, out var error))
            {
                Debug.LogError($"Pack '{packName}' {BlocksFolderName}/{fileName}: {error}");
                continue;
            }

            catalog.Items.Register(definition);

            if (ContentJsonParser.HasBlockTextures(json.textures))
            {
                catalog.BlockTextures.RegisterBlock(
                    definition.RuntimeBlockType,
                    json.textures,
                    packDirectory);
            }

            registered++;
        }

        return registered;
    }

    private static int LoadItemFiles(string directory, ItemRegistry registry, string packName, string folderLabel)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        System.Array.Sort(files, string.CompareOrdinal);
        var registered = 0;

        for (int i = 0; i < files.Length; i++)
        {
            var fileName = Path.GetFileName(files[i]);
            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            var json = JsonUtility.FromJson<ItemJson>(jsonText);
            if (!ContentJsonParser.TryParseItem(json, out var definition, out var error))
            {
                Debug.LogError($"Pack '{packName}' {folderLabel}/{fileName}: {error}");
                continue;
            }

            registry.Register(definition);
            registered++;
        }

        return registered;
    }

    private static int LoadRecipeFiles(string recipesDirectory, ContentCatalog catalog, string packName)
    {
        if (!Directory.Exists(recipesDirectory))
        {
            return 0;
        }

        var clayCount = LoadRecipeJsonFiles(
            Path.Combine(recipesDirectory, ClayRecipesFolderName),
            catalog,
            packName,
            $"{RecipesFolderName}/{ClayRecipesFolderName}",
            RecipeTypes.ClayForming);

        return clayCount;
    }

    private static int LoadRecipeJsonFiles(
        string directory,
        ContentCatalog catalog,
        string packName,
        string folderLabel,
        string defaultRecipeType)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        System.Array.Sort(files, string.CompareOrdinal);
        var registered = 0;

        for (int i = 0; i < files.Length; i++)
        {
            var fileName = Path.GetFileName(files[i]);
            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            var json = JsonUtility.FromJson<RecipeJson>(jsonText);
            if (string.IsNullOrWhiteSpace(json.type))
            {
                json.type = defaultRecipeType;
            }

            if (!ContentJsonParser.TryParseRecipe(json, catalog.Items, catalog, out var error))
            {
                Debug.LogError($"Pack '{packName}' {folderLabel}/{fileName}: {error}");
                continue;
            }

            registered++;
        }

        return registered;
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
            Debug.LogError($"Failed to read JSON file {filePath}: {ex.Message}");
            return false;
        }
    }

    private static int ComparePackLoadOrder(string leftPath, string rightPath)
    {
        var leftName = Path.GetFileName(leftPath);
        var rightName = Path.GetFileName(rightPath);

        var leftPriority = leftName == VanillaPackFolderName ? 0 : 1;
        var rightPriority = rightName == VanillaPackFolderName ? 0 : 1;
        var priorityCompare = leftPriority.CompareTo(rightPriority);
        if (priorityCompare != 0)
        {
            return priorityCompare;
        }

        return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
    }
}
