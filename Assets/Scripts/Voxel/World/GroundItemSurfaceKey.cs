using System;
using UnityEngine;

public readonly struct GroundItemSurfaceKey : IEquatable<GroundItemSurfaceKey>
{
    public Vector3Int FoundationBlock { get; }
    public Vector3Int FaceNormal { get; }

    public GroundItemSurfaceKey(Vector3Int foundationBlock, Vector3Int faceNormal)
    {
        FoundationBlock = foundationBlock;
        FaceNormal = faceNormal;
    }

    public static GroundItemSurfaceKey FromClayWorksite(ClayWorksiteKey worksiteKey)
    {
        return new GroundItemSurfaceKey(worksiteKey.AnchorBlock, worksiteKey.FaceNormal);
    }

    public bool Equals(GroundItemSurfaceKey other)
    {
        return FoundationBlock == other.FoundationBlock && FaceNormal == other.FaceNormal;
    }

    public override bool Equals(object obj)
    {
        return obj is GroundItemSurfaceKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FoundationBlock, FaceNormal);
    }
}

public struct GroundItemSlotSnapshot
{
    public ItemKind Kind;
    public int Count;
}

public struct GroundItemSurfaceSnapshot
{
    public GroundItemSurfaceKey Key;
    public GroundPlacementLayout Layout;
    public GroundItemSlotSnapshot[] Slots;
}
