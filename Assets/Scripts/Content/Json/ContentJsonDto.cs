using System;

[Serializable]
public sealed class ItemJson
{
    public string id;
    public string displayName;
    public string runtimeKind;
    public string blockType;
    public string[] capabilities;
    public string[] commandAliases;
    public bool showInCreative = true;
    public GroundPlacementJson groundPlacement;
    public BlockTexturesJson textures;
}

[Serializable]
public sealed class BlockTexturesJson
{
    public string all;
    public string top;
    public string bottom;
    public string side;
}

[Serializable]
public sealed class GroundPlacementJson
{
    public string layout;
    public int maxStackPerSlot;
    public int shiftPickupAmount = 1;
}

[Serializable]
public sealed class RecipeJson
{
    public string id;
    public string type;
    public string displayName;
    public string outputItemId;
    public RecipeLayerJson[] layers;
}

[Serializable]
public sealed class RecipeLayerJson
{
    public string[] rows;
}

[Serializable]
public sealed class ClayRecipeJson
{
    public string id;
    public string displayName;
    public string outputItemId;
    public RecipeLayerJson[] layers;
}
