using System.Globalization;
using System.Text;

public static class ItemJsonWriter
{
    public static string WriteTransformProperty(string propertyName, ItemDisplayTransformJson transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        WriteTransform(builder, 0, propertyName, transform);
        TrimTrailingComma(builder);
        return builder.ToString().TrimEnd();
    }

    public static string Write(ItemJson json)
    {
        var builder = new StringBuilder(512);
        builder.AppendLine("{");
        WriteField(builder, 1, "id", json.id, required: true);
        WriteField(builder, 1, "shape", json.shape);
        WriteField(builder, 1, "displayName", json.displayName, required: true);
        WriteField(builder, 1, "runtimeKind", json.runtimeKind, required: true);
        WriteField(builder, 1, "blockType", json.blockType);
        WriteStringArray(builder, 1, "capabilities", json.capabilities);
        WriteStringArray(builder, 1, "commandAliases", json.commandAliases);
        WriteBool(builder, 1, "showInCreative", json.showInCreative);
        WriteTransform(builder, 1, "guiTransform", json.guiTransform);
        WriteTransform(builder, 1, "fpHandTransform", json.fpHandTransform);
        WriteGroundPlacement(builder, 1, json.groundPlacement);
        WriteBlockTextures(builder, 1, json.textures);

        TrimTrailingComma(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void WriteField(StringBuilder builder, int indent, string name, string value, bool required = false)
    {
        if (!required && string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append('"').Append(name).Append("\": ");
        builder.Append('"').Append(EscapeString(value ?? string.Empty)).Append('"');
        builder.AppendLine(",");
    }

    private static void WriteBool(StringBuilder builder, int indent, string name, bool value)
    {
        AppendIndent(builder, indent);
        builder.Append('"').Append(name).Append("\": ");
        builder.Append(value ? "true" : "false");
        builder.AppendLine(",");
    }

    private static void WriteStringArray(StringBuilder builder, int indent, string name, string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append('"').Append(name).Append("\": [");
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append('"').Append(EscapeString(values[i] ?? string.Empty)).Append('"');
        }

        builder.AppendLine("],");
    }

    private static void WriteTransform(StringBuilder builder, int indent, string name, ItemDisplayTransformJson transform)
    {
        if (transform == null)
        {
            return;
        }

        var hasAny = transform.translation != null
                     || transform.rotation != null
                     || transform.origin != null
                     || transform.scale > 0f;
        if (!hasAny)
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append('"').Append(name).Append("\": {").AppendLine();
        WriteVector3(builder, indent + 1, "translation", transform.translation);
        WriteVector3(builder, indent + 1, "rotation", transform.rotation);
        WriteVector3(builder, indent + 1, "origin", transform.origin);
        if (transform.scale > 0f)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"scale\": ").Append(FormatFloat(transform.scale)).AppendLine();
        }
        else
        {
            TrimTrailingComma(builder);
        }

        AppendIndent(builder, indent);
        builder.AppendLine("},");
    }

    private static void WriteGroundPlacement(StringBuilder builder, int indent, GroundPlacementJson ground)
    {
        if (ground == null)
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append("\"groundPlacement\": {").AppendLine();
        WriteField(builder, indent + 1, "layout", ground.layout);
        if (ground.maxStackPerSlot > 0)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"maxStackPerSlot\": ").Append(ground.maxStackPerSlot).AppendLine(",");
        }

        if (ground.shiftPickupAmount > 0)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"shiftPickupAmount\": ").Append(ground.shiftPickupAmount).AppendLine(",");
        }

        WriteField(builder, indent + 1, "stackingShape", ground.stackingShape);
        if (ground.cuboidsPerModel > 0)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"cuboidsPerModel\": ").Append(ground.cuboidsPerModel).AppendLine(",");
        }

        if (ground.itemsPerModel > 0)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"itemsPerModel\": ").Append(ground.itemsPerModel).AppendLine(",");
        }

        if (ground.transferQuantity > 0)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"transferQuantity\": ").Append(ground.transferQuantity).AppendLine(",");
        }

        if (ground.cbScaleYByLayer > 0f)
        {
            AppendIndent(builder, indent + 1);
            builder.Append("\"cbScaleYByLayer\": ").Append(FormatFloat(ground.cbScaleYByLayer)).AppendLine();
        }
        else
        {
            TrimTrailingComma(builder);
        }

        AppendIndent(builder, indent);
        builder.AppendLine("},");
    }

    private static void WriteBlockTextures(StringBuilder builder, int indent, BlockTexturesJson textures)
    {
        if (textures == null)
        {
            return;
        }

        var hasAny = !string.IsNullOrWhiteSpace(textures.all)
                     || !string.IsNullOrWhiteSpace(textures.top)
                     || !string.IsNullOrWhiteSpace(textures.bottom)
                     || !string.IsNullOrWhiteSpace(textures.side);
        if (!hasAny)
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append("\"textures\": {").AppendLine();
        WriteField(builder, indent + 1, "all", textures.all);
        WriteField(builder, indent + 1, "top", textures.top);
        WriteField(builder, indent + 1, "bottom", textures.bottom);
        WriteField(builder, indent + 1, "side", textures.side);
        TrimTrailingComma(builder);
        AppendIndent(builder, indent);
        builder.AppendLine("}");
    }

    private static void WriteVector3(StringBuilder builder, int indent, string name, Vector3Json vector)
    {
        if (vector == null)
        {
            return;
        }

        AppendIndent(builder, indent);
        builder.Append('"').Append(name).Append("\": { ");
        builder.Append("\"x\": ").Append(FormatFloat(vector.x)).Append(", ");
        builder.Append("\"y\": ").Append(FormatFloat(vector.y)).Append(", ");
        builder.Append("\"z\": ").Append(FormatFloat(vector.z));
        builder.AppendLine(" },");
    }

    private static void AppendIndent(StringBuilder builder, int indent)
    {
        for (int i = 0; i < indent; i++)
        {
            builder.Append("  ");
        }
    }

    private static void TrimTrailingComma(StringBuilder builder)
    {
        for (int i = builder.Length - 1; i >= 0; i--)
        {
            var ch = builder[i];
            if (ch == ' ' || ch == '\t' || ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                continue;
            }

            if (ch == ',')
            {
                builder.Remove(i, 1);
            }

            break;
        }
    }

    private static string FormatFloat(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
