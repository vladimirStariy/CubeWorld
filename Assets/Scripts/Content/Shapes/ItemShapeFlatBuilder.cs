using System.Collections.Generic;
using UnityEngine;

public static class ItemShapeFlatBuilder
{
    public static bool TryBuild(
        Dictionary<string, object> root,
        ContentId shapeId,
        out ItemShapeDefinition definition,
        out string error)
    {
        definition = null;
        error = null;

        if (root == null || !root.TryGetValue("elements", out var elementsObj) || elementsObj is not List<object> elements)
        {
            error = "Flat shape has no elements.";
            return false;
        }

        var parts = new List<ItemShapePart>();
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is not Dictionary<string, object> elementJson)
            {
                continue;
            }

            if (!TryBuildPart(elementJson, out var part))
            {
                continue;
            }

            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            error = "Flat shape has no solid elements.";
            return false;
        }

        definition = new ItemShapeDefinition(
            shapeId,
            16,
            16,
            new Dictionary<string, string>(),
            parts,
            ItemShapeBounds.Compute(parts));

        return true;
    }

    private static bool TryBuildPart(Dictionary<string, object> elementJson, out ItemShapePart part)
    {
        part = null;
        var from = ItemShapeJsonValues.ReadVec3(elementJson, "from");
        var to = ItemShapeJsonValues.ReadVec3(elementJson, "to");
        if (ItemShapeJsonValues.Approximately(from, to))
        {
            return false;
        }

        var origin = ItemShapeJsonValues.ReadVec3(elementJson, "rotationOrigin", (from + to) * 0.5f);
        var rotation = Quaternion.Euler(
            ItemShapeJsonValues.ReadFloat(elementJson, "rotationX"),
            ItemShapeJsonValues.ReadFloat(elementJson, "rotationY"),
            ItemShapeJsonValues.ReadFloat(elementJson, "rotationZ"));
        var transform = ItemShapeJsonValues.BuildLocalRotation(origin, rotation);
        var useEditorSpace = ItemShapeJsonValues.UsesEditorSpace(from, to);

        var min = Vector3.Min(from, to);
        var max = Vector3.Max(from, to);
        var faces = new List<ItemShapeFace>();

        for (int f = 0; f < ItemShapeFaceGeometry.All.Length; f++)
        {
            var faceName = ItemShapeFaceGeometry.All[f];
            if (!ItemShapeFaceGeometry.TryGetLocalCorners(faceName, min, max, out var localCorners))
            {
                continue;
            }

            var corners = new Vector3[4];
            for (int c = 0; c < 4; c++)
            {
                var point = transform.MultiplyPoint3x4(localCorners[c]);
                corners[c] = useEditorSpace ? ItemShapeJsonValues.EditorSpaceToUnity(point) : point;
            }

            faces.Add(new ItemShapeFace(corners, ItemShapeFaceGeometry.DefaultUvs(), null));
        }

        if (faces.Count == 0)
        {
            return false;
        }

        part = new ItemShapePart(ItemShapeJsonValues.ReadString(elementJson, "name") ?? "Element", faces);
        return true;
    }
}

internal static class ItemShapeBounds
{
    public static Bounds Compute(IReadOnlyList<ItemShapePart> parts)
    {
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < parts.Count; i++)
        {
            var faces = parts[i].Faces;
            for (int f = 0; f < faces.Count; f++)
            {
                var corners = faces[f].Corners;
                for (int c = 0; c < corners.Length; c++)
                {
                    min = Vector3.Min(min, corners[c]);
                    max = Vector3.Max(max, corners[c]);
                }
            }
        }

        return float.IsPositiveInfinity(min.x)
            ? new Bounds(Vector3.zero, Vector3.zero)
            : new Bounds((min + max) * 0.5f, max - min);
    }
}

internal static class ItemShapeFaceGeometry
{
    public static readonly string[] All = { "north", "south", "east", "west", "up", "down" };

    public static Vector2[] DefaultUvs() =>
        new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

    public static bool TryGetLocalCorners(string faceName, Vector3 min, Vector3 max, out Vector3[] corners)
    {
        corners = new Vector3[4];
        switch (faceName)
        {
            case "north":
                corners[0] = new Vector3(min.x, min.y, min.z);
                corners[1] = new Vector3(max.x, min.y, min.z);
                corners[2] = new Vector3(max.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, min.z);
                return true;
            case "south":
                corners[0] = new Vector3(max.x, min.y, max.z);
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, max.z);
                corners[3] = new Vector3(max.x, max.y, max.z);
                return true;
            case "east":
                corners[0] = new Vector3(max.x, min.y, min.z);
                corners[1] = new Vector3(max.x, min.y, max.z);
                corners[2] = new Vector3(max.x, max.y, max.z);
                corners[3] = new Vector3(max.x, max.y, min.z);
                return true;
            case "west":
                corners[0] = new Vector3(min.x, min.y, max.z);
                corners[1] = new Vector3(min.x, min.y, min.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                return true;
            case "up":
                corners[0] = new Vector3(min.x, max.y, min.z);
                corners[1] = new Vector3(max.x, max.y, min.z);
                corners[2] = new Vector3(max.x, max.y, max.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                return true;
            case "down":
                corners[0] = new Vector3(min.x, min.y, max.z);
                corners[1] = new Vector3(max.x, min.y, max.z);
                corners[2] = new Vector3(max.x, min.y, min.z);
                corners[3] = new Vector3(min.x, min.y, min.z);
                return true;
            default:
                return false;
        }
    }
}

internal static class ItemShapeJsonValues
{
    public static bool UsesEditorSpace(Vector3 from, Vector3 to) =>
        Mathf.Max(
            Mathf.Abs(from.x), Mathf.Abs(from.y), Mathf.Abs(from.z),
            Mathf.Abs(to.x), Mathf.Abs(to.y), Mathf.Abs(to.z)) > 2f;

    public static Vector3 EditorSpaceToUnity(Vector3 shapePoint) =>
        shapePoint / 16f - new Vector3(0.5f, 0.5f, 0.5f);

    public static Matrix4x4 BuildLocalRotation(Vector3 origin, Quaternion rotation)
    {
        if (rotation == Quaternion.identity)
        {
            return Matrix4x4.identity;
        }

        return Matrix4x4.Translate(origin)
               * Matrix4x4.Rotate(rotation)
               * Matrix4x4.Translate(-origin);
    }

    public static bool Approximately(Vector3 a, Vector3 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);

    public static string ReadString(Dictionary<string, object> source, string key) =>
        source != null && source.TryGetValue(key, out var value) ? value as string : null;

    public static float ReadFloat(Dictionary<string, object> source, string key) =>
        source != null && source.TryGetValue(key, out var value) ? ToFloat(value) : 0f;

    public static Vector3 ReadVec3(Dictionary<string, object> source, string key, Vector3? fallback = null)
    {
        if (source == null || !source.TryGetValue(key, out var value) || value is not List<object> list || list.Count < 3)
        {
            return fallback ?? Vector3.zero;
        }

        return new Vector3(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]));
    }

    public static float ToFloat(object value) =>
        value switch
        {
            double d => (float)d,
            long l => l,
            float f => f,
            _ => 0f
        };
}
