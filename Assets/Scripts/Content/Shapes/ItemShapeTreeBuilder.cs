using System;
using System.Collections.Generic;
using UnityEngine;

public static class ItemShapeTreeBuilder
{
    public static bool TryBuild(
        Dictionary<string, object> root,
        ContentId shapeId,
        string packDirectory,
        out ItemShapeDefinition definition,
        out string error)
    {
        definition = null;
        error = null;

        if (root == null)
        {
            error = "Shape root is null.";
            return false;
        }

        var textureWidth = ReadInt(root, "textureWidth", 16);
        var textureHeight = ReadInt(root, "textureHeight", 16);
        var texturePaths = ResolveTextures(ReadObjectMap(root, "textures"), packDirectory);

        var parts = new List<ItemShapePart>();
        if (root.TryGetValue("elements", out var elementsObj) && elementsObj is List<object> elements)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] is not Dictionary<string, object> element)
                {
                    continue;
                }

                var collectedFaces = new List<ItemShapeFace>();
                WalkElementCollect(element, Matrix4x4.identity, textureWidth, textureHeight, collectedFaces);
                if (collectedFaces.Count == 0)
                {
                    continue;
                }

                var partName = ItemShapeJsonValues.ReadString(element, "name") ?? $"Element_{i + 1}";
                parts.Add(new ItemShapePart(partName, collectedFaces));
            }
        }

        if (parts.Count == 0)
        {
            error = "Shape has no solid elements.";
            return false;
        }

        definition = new ItemShapeDefinition(
            shapeId,
            textureWidth,
            textureHeight,
            texturePaths,
            parts,
            ItemShapeBounds.Compute(parts));

        return true;
    }

    private static void WalkElementCollect(
        Dictionary<string, object> element,
        Matrix4x4 parentMatrix,
        int textureWidth,
        int textureHeight,
        List<ItemShapeFace> collected)
    {
        var from = ItemShapeJsonValues.ReadVec3(element, "from");
        var to = ItemShapeJsonValues.ReadVec3(element, "to");
        var origin = ItemShapeJsonValues.ReadVec3(element, "rotationOrigin", (from + to) * 0.5f);
        var rotation = Quaternion.Euler(
            ItemShapeJsonValues.ReadFloat(element, "rotationX"),
            ItemShapeJsonValues.ReadFloat(element, "rotationY"),
            ItemShapeJsonValues.ReadFloat(element, "rotationZ"));

        var localRotation = ItemShapeJsonValues.BuildLocalRotation(origin, rotation);
        var hasVolume = !ItemShapeJsonValues.Approximately(from, to);

        if (hasVolume)
        {
            var transform = parentMatrix * localRotation;
            collected.AddRange(BuildFaces(element, from, to, transform, textureWidth, textureHeight));
        }

        var childMatrix = parentMatrix * localRotation * Matrix4x4.Translate(from);

        if (!element.TryGetValue("children", out var childrenObj) || childrenObj is not List<object> children)
        {
            return;
        }

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is Dictionary<string, object> child)
            {
                WalkElementCollect(child, childMatrix, textureWidth, textureHeight, collected);
            }
        }
    }

    private static List<ItemShapeFace> BuildFaces(
        Dictionary<string, object> element,
        Vector3 from,
        Vector3 to,
        Matrix4x4 transform,
        int textureWidth,
        int textureHeight)
    {
        var faces = new List<ItemShapeFace>();
        if (!element.TryGetValue("faces", out var facesObj) || facesObj is not Dictionary<string, object> faceMap)
        {
            return faces;
        }

        var min = Vector3.Min(from, to);
        var max = Vector3.Max(from, to);

        for (int i = 0; i < ItemShapeFaceGeometry.All.Length; i++)
        {
            var faceName = ItemShapeFaceGeometry.All[i];
            if (!faceMap.TryGetValue(faceName, out var faceObj) || faceObj is not Dictionary<string, object> faceJson)
            {
                continue;
            }

            var textureKey = ResolveTextureKey(ReadString(faceJson, "texture"));
            if (string.IsNullOrEmpty(textureKey))
            {
                continue;
            }

            if (!ItemShapeFaceGeometry.TryGetLocalCorners(faceName, min, max, out var localCorners))
            {
                continue;
            }

            var corners = new Vector3[4];
            for (int c = 0; c < 4; c++)
            {
                corners[c] = ItemShapeJsonValues.EditorSpaceToUnity(transform.MultiplyPoint3x4(localCorners[c]));
            }

            var uvs = BuildFaceUvs(ReadFloatArray(faceJson, "uv"), textureWidth, textureHeight);
            faces.Add(new ItemShapeFace(corners, uvs, textureKey));
        }

        return faces;
    }

    private static Vector2[] BuildFaceUvs(float[] uv, int textureWidth, int textureHeight)
    {
        if (uv == null || uv.Length < 4)
        {
            return ItemShapeFaceGeometry.DefaultUvs();
        }

        var u1 = uv[0] / textureWidth;
        var v1 = 1f - uv[1] / textureHeight;
        var u2 = uv[2] / textureWidth;
        var v2 = 1f - uv[3] / textureHeight;

        return new[]
        {
            new Vector2(u1, v1),
            new Vector2(u2, v1),
            new Vector2(u2, v2),
            new Vector2(u1, v2)
        };
    }

    private static Dictionary<string, string> ResolveTextures(
        Dictionary<string, object> textureMap,
        string packDirectory)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        if (textureMap == null || string.IsNullOrWhiteSpace(packDirectory))
        {
            return resolved;
        }

        foreach (var pair in textureMap)
        {
            var path = ResolveTexturePath(pair.Value as string, packDirectory);
            if (!string.IsNullOrEmpty(path))
            {
                resolved[pair.Key] = path;
            }
        }

        return resolved;
    }

    private static string ResolveTexturePath(string textureRef, string packDirectory)
    {
        if (string.IsNullOrWhiteSpace(textureRef))
        {
            return null;
        }

        var trimmed = textureRef.Trim().Replace('\\', '/');
        if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 4);
        }

        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(packDirectory, "textures", trimmed + ".png"));
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Item shape texture not found: {path}");
            return null;
        }

        return path;
    }

    private static string ResolveTextureKey(string textureRef)
    {
        if (string.IsNullOrWhiteSpace(textureRef))
        {
            return null;
        }

        var trimmed = textureRef.Trim();
        if (trimmed.StartsWith("#"))
        {
            trimmed = trimmed.Substring(1);
        }

        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }

    private static Dictionary<string, object> ReadObjectMap(Dictionary<string, object> source, string key) =>
        source != null && source.TryGetValue(key, out var value) && value is Dictionary<string, object> map ? map : null;

    private static string ReadString(Dictionary<string, object> source, string key) =>
        source != null && source.TryGetValue(key, out var value) ? value as string : null;

    private static int ReadInt(Dictionary<string, object> source, string key, int fallback) =>
        source != null && source.TryGetValue(key, out var value)
            ? value switch
            {
                long l => (int)l,
                double d => (int)d,
                int i => i,
                _ => fallback
            }
            : fallback;

    private static float[] ReadFloatArray(Dictionary<string, object> source, string key)
    {
        if (source == null || !source.TryGetValue(key, out var value) || value is not List<object> list)
        {
            return null;
        }

        var result = new float[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            result[i] = ItemShapeJsonValues.ToFloat(list[i]);
        }

        return result;
    }
}
