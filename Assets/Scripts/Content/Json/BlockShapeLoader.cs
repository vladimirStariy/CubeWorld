using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class BlockShapeLoader
{
    private const string BaseFolderName = "base";
    private const string ShapesFolderName = "shapes";

    public static int LoadBaseShapes(string contentRoot, BlockShapeRegistry registry)
    {
        if (registry == null)
        {
            return 0;
        }

        var shapesDirectory = Path.Combine(contentRoot, BaseFolderName, ShapesFolderName);
        if (!Directory.Exists(shapesDirectory))
        {
            Debug.LogWarning($"Block shapes folder not found: {shapesDirectory}");
            return 0;
        }

        var files = Directory.GetFiles(shapesDirectory, "*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        Array.Sort(files, string.CompareOrdinal);
        var rawShapes = new Dictionary<ContentId, BlockShapeJson>();

        for (int i = 0; i < files.Length; i++)
        {
            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            var json = JsonUtility.FromJson<BlockShapeJson>(jsonText);
            if (!ContentId.TryParse(json.id, out var shapeId))
            {
                Debug.LogError($"Shape {Path.GetFileName(files[i])} has invalid id: {json.id}");
                continue;
            }

            rawShapes[shapeId] = json;
        }

        var resolved = 0;
        foreach (var pair in rawShapes)
        {
            if (TryResolveShape(pair.Key, rawShapes, new HashSet<ContentId>(), out var definition, out var error))
            {
                registry.Register(definition);
                resolved++;
            }
            else
            {
                Debug.LogError($"Shape {pair.Key}: {error}");
            }
        }

        if (resolved > 0)
        {
            Debug.Log($"Loaded {resolved} base block shape(s) from {BaseFolderName}/{ShapesFolderName}/.");
        }

        return resolved;
    }

    private static bool TryResolveShape(
        ContentId id,
        Dictionary<ContentId, BlockShapeJson> rawShapes,
        HashSet<ContentId> chain,
        out BlockShapeDefinition definition,
        out string error)
    {
        definition = null;
        error = null;

        if (!rawShapes.TryGetValue(id, out var json))
        {
            error = $"Unknown shape id {id}.";
            return false;
        }

        if (!chain.Add(id))
        {
            error = $"Shape parent cycle detected at {id}.";
            return false;
        }

        BlockShapeJson parentJson = null;
        if (!string.IsNullOrWhiteSpace(json.parent))
        {
            if (!ContentId.TryParse(json.parent, out var parentId))
            {
                error = $"Invalid parent id: {json.parent}";
                return false;
            }

            if (!rawShapes.TryGetValue(parentId, out parentJson))
            {
                error = $"Unknown parent shape {json.parent}.";
                return false;
            }
        }

        if (!TryParseRenderKind(json.render ?? parentJson?.render, out var renderKind, out error))
        {
            return false;
        }

        if (!TryParseOcclusionMode(json.occlusion ?? parentJson?.occlusion, out var occlusionMode, out error))
        {
            return false;
        }

        var occlusionBoxes = ParseOcclusionBoxes(json.occlusionBoxes);
        if (occlusionBoxes.Count == 0 && parentJson?.occlusionBoxes != null)
        {
            occlusionBoxes = ParseOcclusionBoxes(parentJson.occlusionBoxes);
        }

        if (occlusionMode == BlockOcclusionMode.Boxes && occlusionBoxes.Count == 0)
        {
            error = "occlusion mode 'boxes' requires occlusionBoxes.";
            return false;
        }

        var elements = ParseElements(json.elements);
        if (elements.Count == 0 && parentJson?.elements != null)
        {
            elements = ParseElements(parentJson.elements);
        }

        if (occlusionMode == BlockOcclusionMode.Full && occlusionBoxes.Count == 0)
        {
            occlusionBoxes.Add(new BlockOcclusionBox(0, 0, 0, 16, 16, 16));
        }

        definition = new BlockShapeDefinition(
            id,
            renderKind,
            occlusionMode,
            occlusionBoxes,
            elements,
            ComputeLocalBounds(renderKind, occlusionBoxes, elements));

        chain.Remove(id);
        return true;
    }

    private static bool TryParseRenderKind(string text, out BlockShapeRenderKind renderKind, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "render is required.";
            renderKind = default;
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "full_cube":
            case "cube":
                renderKind = BlockShapeRenderKind.FullCube;
                return true;
            case "bottom_slab":
            case "slab":
                renderKind = BlockShapeRenderKind.BottomSlab;
                return true;
            case "custom_mesh":
            case "custom":
                renderKind = BlockShapeRenderKind.CustomMesh;
                return true;
            default:
                error = $"Unknown render kind: {text}";
                renderKind = default;
                return false;
        }
    }

    private static bool TryParseOcclusionMode(string text, out BlockOcclusionMode mode, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "occlusion is required.";
            mode = default;
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "full":
                mode = BlockOcclusionMode.Full;
                return true;
            case "none":
                mode = BlockOcclusionMode.None;
                return true;
            case "boxes":
                mode = BlockOcclusionMode.Boxes;
                return true;
            default:
                error = $"Unknown occlusion mode: {text}";
                mode = default;
                return false;
        }
    }

    private static List<BlockOcclusionBox> ParseOcclusionBoxes(BlockOcclusionBoxJson[] boxesJson)
    {
        var boxes = new List<BlockOcclusionBox>();
        if (boxesJson == null)
        {
            return boxes;
        }

        for (int i = 0; i < boxesJson.Length; i++)
        {
            if (TryParseBox(boxesJson[i], out var box))
            {
                boxes.Add(box);
            }
        }

        return boxes;
    }

    private static List<BlockShapeElement> ParseElements(BlockShapeElementJson[] elementsJson)
    {
        var elements = new List<BlockShapeElement>();
        if (elementsJson == null)
        {
            return elements;
        }

        for (int i = 0; i < elementsJson.Length; i++)
        {
            var elementJson = elementsJson[i];
            if (elementJson == null || !TryParseBox(elementJson.from, elementJson.to, out var bounds))
            {
                continue;
            }

            elements.Add(new BlockShapeElement(elementJson.name ?? "Element", bounds));
        }

        return elements;
    }

    private static bool TryParseBox(BlockOcclusionBoxJson json, out BlockOcclusionBox box)
    {
        box = default;
        return json != null && TryParseBox(json.from, json.to, out box);
    }

    private static bool TryParseBox(float[] from, float[] to, out BlockOcclusionBox box)
    {
        box = default;
        if (from == null || to == null || from.Length < 3 || to.Length < 3)
        {
            return false;
        }

        var minX = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(from[0], to[0])), 0, 16);
        var minY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(from[1], to[1])), 0, 16);
        var minZ = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(from[2], to[2])), 0, 16);
        var maxX = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(from[0], to[0])), 0, 16);
        var maxY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(from[1], to[1])), 0, 16);
        var maxZ = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(from[2], to[2])), 0, 16);

        if (maxX <= minX || maxY <= minY || maxZ <= minZ)
        {
            return false;
        }

        box = new BlockOcclusionBox(minX, minY, minZ, maxX, maxY, maxZ);
        return true;
    }

    private static Bounds ComputeLocalBounds(
        BlockShapeRenderKind renderKind,
        IReadOnlyList<BlockOcclusionBox> occlusionBoxes,
        IReadOnlyList<BlockShapeElement> elements)
    {
        if (renderKind == BlockShapeRenderKind.BottomSlab)
        {
            return new Bounds(Vector3.down * 0.25f, new Vector3(1f, 0.5f, 1f));
        }

        if (renderKind == BlockShapeRenderKind.CustomMesh)
        {
            return new Bounds(Vector3.down * 0.24f, new Vector3(0.84f, 0.56f, 0.84f));
        }

        if (occlusionBoxes.Count > 0)
        {
            return BoundsFromBoxes(occlusionBoxes);
        }

        if (elements.Count > 0)
        {
            var elementBounds = new BlockOcclusionBox[elements.Count];
            for (int i = 0; i < elements.Count; i++)
            {
                elementBounds[i] = elements[i].Bounds;
            }

            return BoundsFromBoxes(elementBounds);
        }

        return new Bounds(Vector3.zero, Vector3.one);
    }

    private static Bounds BoundsFromBoxes(IReadOnlyList<BlockOcclusionBox> boxes)
    {
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            min.x = Mathf.Min(min.x, box.MinX / 16f - 0.5f);
            min.y = Mathf.Min(min.y, box.MinY / 16f - 0.5f);
            min.z = Mathf.Min(min.z, box.MinZ / 16f - 0.5f);
            max.x = Mathf.Max(max.x, box.MaxX / 16f - 0.5f);
            max.y = Mathf.Max(max.y, box.MaxY / 16f - 0.5f);
            max.z = Mathf.Max(max.z, box.MaxZ / 16f - 0.5f);
        }

        var center = (min + max) * 0.5f;
        var size = max - min;
        return new Bounds(center, size);
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
            Debug.LogError($"Failed to read shape JSON {filePath}: {ex.Message}");
            return false;
        }
    }
}
