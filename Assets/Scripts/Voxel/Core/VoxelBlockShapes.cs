using UnityEngine;

public static class VoxelBlockShapes
{
    public static bool IsCustomMeshBlock(VoxelBlockType blockType)
    {
        return TryGetRenderKind(blockType, out var renderKind)
            ? renderKind == BlockShapeRenderKind.CustomMesh
            : blockType == VoxelBlockType.Campfire;
    }

    public static bool IsFullCube(VoxelBlockType blockType)
    {
        return TryGetRenderKind(blockType, out var renderKind)
            ? renderKind == BlockShapeRenderKind.FullCube
            : blockType is VoxelBlockType.Dirt or VoxelBlockType.GrassBlock;
    }

    public static bool IsBottomSlab(VoxelBlockType blockType)
    {
        return TryGetRenderKind(blockType, out var renderKind)
            ? renderKind == BlockShapeRenderKind.BottomSlab
            : blockType == VoxelBlockType.DirtSlab;
    }

    public static Bounds GetWorldBounds(Vector3Int blockPosition, VoxelBlockType blockType)
    {
        if (BlockShapeLibrary.TryGet(blockType, out var shape))
        {
            return new Bounds((Vector3)blockPosition + shape.LocalBounds.center, shape.LocalBounds.size);
        }

        if (IsBottomSlab(blockType))
        {
            return new Bounds((Vector3)blockPosition + Vector3.down * 0.25f, new Vector3(1f, 0.5f, 1f));
        }

        if (IsCustomMeshBlock(blockType))
        {
            return new Bounds((Vector3)blockPosition + Vector3.down * 0.24f, new Vector3(0.84f, 0.56f, 0.84f));
        }

        return new Bounds(blockPosition, Vector3.one);
    }

    private static bool TryGetRenderKind(VoxelBlockType blockType, out BlockShapeRenderKind renderKind)
    {
        if (BlockShapeLibrary.TryGet(blockType, out var shape))
        {
            renderKind = shape.RenderKind;
            return true;
        }

        renderKind = default;
        return false;
    }
}
