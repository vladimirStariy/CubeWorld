using System;
using UnityEngine;

public static class ContentJsonParser
{
    public static bool TryParseItem(ItemJson json, out ItemDefinition definition, out string error)
    {
        definition = null;
        error = null;

        if (json == null)
        {
            error = "Item entry is null.";
            return false;
        }

        if (!ContentId.TryParse(json.id, out var contentId))
        {
            error = $"Invalid item id: {json.id}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(json.displayName))
        {
            error = $"Item {contentId} is missing displayName.";
            return false;
        }

        if (!TryParseItemKind(json.runtimeKind, out var runtimeKind, out error))
        {
            error = $"Item {contentId}: {error}";
            return false;
        }

        var blockType = VoxelBlockType.Air;
        if (!string.IsNullOrWhiteSpace(json.blockType))
        {
            if (!TryParseBlockType(json.blockType, out blockType, out error))
            {
                error = $"Item {contentId}: {error}";
                return false;
            }
        }

        if (!TryParseCapabilities(json.capabilities, out var capabilities, out error))
        {
            error = $"Item {contentId}: {error}";
            return false;
        }

        GroundItemPlacementProfile? groundProfile = null;
        if (HasGroundPlacement(json.groundPlacement))
        {
            if (!TryParseGroundProfile(json.groundPlacement, out var profile, out error))
            {
                error = $"Item {contentId}: {error}";
                return false;
            }

            groundProfile = profile;
        }

        var shapeId = ResolveShapeId(json.shape, blockType);
        var guiTransform = TryParseDisplayTransform(json.guiTransform);
        var fpHandTransform = TryParseDisplayTransform(json.fpHandTransform);

        definition = new ItemDefinition(
            contentId,
            json.displayName,
            runtimeKind,
            capabilities,
            blockType,
            groundProfile,
            json.showInCreative,
            json.commandAliases ?? Array.Empty<string>(),
            shapeId,
            guiTransform,
            fpHandTransform);

        return true;
    }

    public static ContentId ResolveShapeId(string shapeText, VoxelBlockType blockType)
    {
        if (!string.IsNullOrWhiteSpace(shapeText) && ContentId.TryParse(shapeText, out var parsedShapeId))
        {
            return parsedShapeId;
        }

        return InferDefaultShapeId(blockType);
    }

    private static ContentId InferDefaultShapeId(VoxelBlockType blockType)
    {
        return blockType switch
        {
            VoxelBlockType.DirtSlab => new ContentId("base", "bottom_slab"),
            VoxelBlockType.Campfire => new ContentId("base", "custom_mesh"),
            _ => new ContentId("base", "cube")
        };
    }

    public static bool TryParseRecipe(RecipeJson json, ItemRegistry items, ContentCatalog catalog, out string error)
    {
        error = null;
        if (json == null)
        {
            error = "Recipe entry is null.";
            return false;
        }

        var recipeType = string.IsNullOrWhiteSpace(json.type)
            ? RecipeTypes.ClayForming
            : json.type.Trim();

        if (string.Equals(recipeType, RecipeTypes.ClayForming, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseClayFormingRecipe(json, items, out var recipe, out error))
            {
                return false;
            }

            catalog.ClayRecipes.Register(recipe);
            return true;
        }

        if (string.Equals(recipeType, RecipeTypes.Crafting, System.StringComparison.OrdinalIgnoreCase))
        {
            error = $"Crafting recipe type is not implemented yet: {json.id}";
            return false;
        }

        error = $"Unknown recipe type '{recipeType}' for recipe {json.id}.";
        return false;
    }

    public static bool TryParseClayRecipe(ClayRecipeJson json, ItemRegistry items, out ClayFormingRecipe recipe, out string error)
    {
        var wrapped = new RecipeJson
        {
            id = json?.id,
            type = RecipeTypes.ClayForming,
            displayName = json?.displayName,
            outputItemId = json?.outputItemId,
            layers = json?.layers
        };

        if (!TryParseClayFormingRecipe(wrapped, items, out recipe, out error))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseClayFormingRecipe(
        RecipeJson json,
        ItemRegistry items,
        out ClayFormingRecipe recipe,
        out string error)
    {
        recipe = null;
        error = null;

        if (json == null)
        {
            error = "Clay recipe entry is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(json.id))
        {
            error = "Clay recipe is missing id.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(json.displayName))
        {
            error = $"Clay recipe {json.id} is missing displayName.";
            return false;
        }

        if (!ContentId.TryParse(json.outputItemId, out var outputId))
        {
            error = $"Clay recipe {json.id} has invalid outputItemId: {json.outputItemId}";
            return false;
        }

        if (items == null || !items.TryGet(outputId, out var outputDefinition))
        {
            error = $"Clay recipe {json.id} references unknown output item: {json.outputItemId}";
            return false;
        }

        if (json.layers == null || json.layers.Length == 0)
        {
            error = $"Clay recipe {json.id} has no layers.";
            return false;
        }

        var pattern = new string[json.layers.Length][];
        for (int i = 0; i < json.layers.Length; i++)
        {
            var layer = json.layers[i];
            if (layer?.rows == null || layer.rows.Length == 0)
            {
                error = $"Clay recipe {json.id} layer {i} is empty.";
                return false;
            }

            pattern[i] = layer.rows;
        }

        recipe = new ClayFormingRecipe(
            json.id,
            json.displayName,
            outputDefinition.CreateStack(),
            pattern);

        return true;
    }

    private static bool TryParseItemKind(string text, out ItemKind kind, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "runtimeKind is required.";
            kind = default;
            return false;
        }

        if (Enum.TryParse(text, ignoreCase: true, out kind))
        {
            return true;
        }

        error = $"Unknown runtimeKind: {text}";
        return false;
    }

    private static bool TryParseBlockType(string text, out VoxelBlockType blockType, out string error)
    {
        error = null;
        if (Enum.TryParse(text, ignoreCase: true, out blockType))
        {
            return true;
        }

        error = $"Unknown blockType: {text}";
        return false;
    }

    private static bool TryParseCapabilities(string[] capabilityNames, out ItemCapabilities capabilities, out string error)
    {
        capabilities = ItemCapabilities.None;
        error = null;

        if (capabilityNames == null || capabilityNames.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < capabilityNames.Length; i++)
        {
            var name = capabilityNames[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!Enum.TryParse(name, ignoreCase: true, out ItemCapabilities flag))
            {
                error = $"Unknown capability: {name}";
                return false;
            }

            capabilities |= flag;
        }

        return true;
    }

    private static bool HasGroundPlacement(GroundPlacementJson json) =>
        json != null && !string.IsNullOrWhiteSpace(json.layout);

    public static bool HasBlockTextures(BlockTexturesJson json) =>
        json != null
        && (!string.IsNullOrWhiteSpace(json.all)
            || !string.IsNullOrWhiteSpace(json.top)
            || !string.IsNullOrWhiteSpace(json.bottom)
            || !string.IsNullOrWhiteSpace(json.side));

    private static bool TryParseGroundProfile(
        GroundPlacementJson json,
        out GroundItemPlacementProfile profile,
        out string error)
    {
        profile = default;
        error = null;

        if (string.IsNullOrWhiteSpace(json.layout))
        {
            error = "groundPlacement.layout is required.";
            return false;
        }

        if (!Enum.TryParse(json.layout, ignoreCase: true, out GroundPlacementLayout layout))
        {
            error = $"Unknown ground placement layout: {json.layout}";
            return false;
        }

        if (json.maxStackPerSlot <= 0)
        {
            error = "groundPlacement.maxStackPerSlot must be positive.";
            return false;
        }

        ContentId stackingShapeId = default;
        if (!string.IsNullOrWhiteSpace(json.stackingShape)
            && !ContentId.TryParse(json.stackingShape, out stackingShapeId))
        {
            error = $"Invalid groundPlacement.stackingShape: {json.stackingShape}";
            return false;
        }

        var transferQuantity = json.transferQuantity > 0 ? json.transferQuantity : 1;
        profile = new GroundItemPlacementProfile(
            layout,
            json.maxStackPerSlot,
            json.shiftPickupAmount,
            stackingShapeId,
            json.cuboidsPerModel,
            json.itemsPerModel,
            transferQuantity,
            json.cbScaleYByLayer);
        return true;
    }

    private static ItemDisplayTransform? TryParseDisplayTransform(ItemDisplayTransformJson json)
    {
        if (json == null)
        {
            return null;
        }

        var hasAny = json.translation != null
                     || json.rotation != null
                     || json.origin != null
                     || json.scale > 0f;
        if (!hasAny)
        {
            return null;
        }

        return new ItemDisplayTransform(
            ReadVector3(json.translation),
            ReadVector3(json.rotation),
            ReadVector3(json.origin),
            json.scale);
    }

    private static Vector3 ReadVector3(Vector3Json json) =>
        json == null ? Vector3.zero : new Vector3(json.x, json.y, json.z);
}
