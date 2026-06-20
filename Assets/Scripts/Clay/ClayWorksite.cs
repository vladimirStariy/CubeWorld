using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ClayWorksiteKey : IEquatable<ClayWorksiteKey>
{
    public Vector3Int AnchorBlock { get; }
    public Vector3Int FaceNormal { get; }

    public ClayWorksiteKey(Vector3Int anchorBlock, Vector3Int faceNormal)
    {
        AnchorBlock = anchorBlock;
        FaceNormal = faceNormal;
    }

    public bool Equals(ClayWorksiteKey other)
    {
        return AnchorBlock == other.AnchorBlock && FaceNormal == other.FaceNormal;
    }

    public override bool Equals(object obj)
    {
        return obj is ClayWorksiteKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AnchorBlock, FaceNormal);
    }
}

public sealed class ClayWorksite
{
    public ClayWorksiteKey Key { get; }
    public bool[,] BaseLayer { get; }
    public ClayFormingSession Session { get; private set; }

    public ClayWorksite(ClayWorksiteKey key)
    {
        Key = key;
        BaseLayer = CreateInitialPadLayer();
    }

    public bool HasSession => Session != null;

    public void StartSession(ClayFormingRecipe recipe)
    {
        Session = new ClayFormingSession(Key.AnchorBlock, Key.FaceNormal, recipe, BaseLayer);
    }

    public void ClearSession()
    {
        Session = null;
    }

    private static bool[,] CreateInitialPadLayer()
    {
        var layer = new bool[ClayFormingConstants.GridSize, ClayFormingConstants.GridSize];
        var offset = ClayFormingConstants.InitialPadOffset;
        for (int v = 0; v < ClayFormingConstants.InitialPadSize; v++)
        {
            for (int u = 0; u < ClayFormingConstants.InitialPadSize; u++)
            {
                layer[offset + u, offset + v] = true;
            }
        }

        return layer;
    }
}

public struct ClayWorksiteSnapshot
{
    public ClayWorksiteKey Key;
    public bool[,] BaseLayer;
    public bool HasSession;
    public ClayFormingSession Session;
}
