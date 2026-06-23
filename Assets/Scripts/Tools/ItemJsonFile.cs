using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ItemJsonFile
{
    public static bool TryRead(string filePath, out ItemJson json, out string error)
    {
        json = null;
        error = null;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            error = $"File not found: {filePath}";
            return false;
        }

        try
        {
            json = JsonUtility.FromJson<ItemJson>(File.ReadAllText(filePath));
            if (json == null)
            {
                error = "JSON parsed to null.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryWrite(string filePath, ItemJson json, out string error)
    {
        error = null;
        if (json == null)
        {
            error = "Item JSON is null.";
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, ItemJsonWriter.Write(json));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static List<ItemJsonCatalogEntry> ScanItemFiles()
    {
        var entries = new List<ItemJsonCatalogEntry>();
        var contentRoot = Path.Combine(Application.streamingAssetsPath, "Content");
        if (!Directory.Exists(contentRoot))
        {
            return entries;
        }

        var packDirectories = Directory.GetDirectories(contentRoot);
        Array.Sort(packDirectories, string.CompareOrdinal);

        for (int i = 0; i < packDirectories.Length; i++)
        {
            var itemsDirectory = Path.Combine(packDirectories[i], "items");
            if (!Directory.Exists(itemsDirectory))
            {
                continue;
            }

            var files = Directory.GetFiles(itemsDirectory, "*.json");
            Array.Sort(files, string.CompareOrdinal);

            for (int f = 0; f < files.Length; f++)
            {
                if (!TryRead(files[f], out var json, out _))
                {
                    continue;
                }

                entries.Add(new ItemJsonCatalogEntry(
                    files[f],
                    json.id,
                    string.IsNullOrWhiteSpace(json.displayName) ? Path.GetFileNameWithoutExtension(files[f]) : json.displayName));
            }
        }

        return entries;
    }
}

public readonly struct ItemJsonCatalogEntry
{
    public ItemJsonCatalogEntry(string filePath, string contentId, string displayName)
    {
        FilePath = filePath;
        ContentId = contentId;
        DisplayName = displayName;
    }

    public string FilePath { get; }
    public string ContentId { get; }
    public string DisplayName { get; }

    public string Label => string.IsNullOrWhiteSpace(ContentId)
        ? DisplayName
        : $"{DisplayName} ({ContentId})";
}
