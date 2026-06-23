using System.Collections.Generic;
using UnityEngine;

public static class BlockShapeLibrary
{
    public static BlockShapeRegistry Active { get; set; }

    private static readonly Dictionary<VoxelBlockType, BlockShapeDefinition> BlockShapes = new();

    public static void Clear()
    {
        BlockShapes.Clear();
    }

    public static void RegisterBlockShape(VoxelBlockType blockType, ContentId shapeId)
    {
        if (blockType == VoxelBlockType.Air)
        {
            return;
        }

        if (Active != null && Active.TryGet(shapeId, out var definition))
        {
            BlockShapes[blockType] = definition;
            return;
        }

        Debug.LogWarning($"Block {blockType} references unknown shape {shapeId}.");
    }

    public static bool TryGet(VoxelBlockType blockType, out BlockShapeDefinition definition)
    {
        return BlockShapes.TryGetValue(blockType, out definition);
    }

    public static BlockShapeDefinition Get(VoxelBlockType blockType)
    {
        BlockShapes.TryGetValue(blockType, out var definition);
        return definition;
    }
}
