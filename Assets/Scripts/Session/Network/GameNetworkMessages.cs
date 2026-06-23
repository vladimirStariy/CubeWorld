using System;
using UnityEngine;

public enum GameNetworkMessageKind
{
    WorldCommand = 1,
    WorldCommandResult = 2,
    BlockChange = 3,
    ChunkInvalidate = 4,
    ChunkUnload = 5,
    ChunkBlocksSnapshot = 6,
    ClayWorksiteChanged = 7
}

[Serializable]
public struct BlockChangeMessage
{
    public int X;
    public int Y;
    public int Z;
    public int OldType;
    public int NewType;

    public Vector3Int Position => new(X, Y, Z);

    public static BlockChangeMessage From(BlockChangeEvent change)
    {
        return new BlockChangeMessage
        {
            X = change.Position.x,
            Y = change.Position.y,
            Z = change.Position.z,
            OldType = (int)change.OldType,
            NewType = (int)change.NewType
        };
    }
}

[Serializable]
public struct ChunkCoordMessage
{
    public int X;
    public int Y;
    public int Z;

    public Vector3Int Coord => new(X, Y, Z);

    public static ChunkCoordMessage From(Vector3Int coord)
    {
        return new ChunkCoordMessage { X = coord.x, Y = coord.y, Z = coord.z };
    }
}

[Serializable]
public struct WorldCommandMessage
{
    public int Kind;
    public int BlockX;
    public int BlockY;
    public int BlockZ;
    public float FaceX;
    public float FaceY;
    public float FaceZ;
    public float HitX;
    public float HitY;
    public float HitZ;
    public int TargetX;
    public int TargetY;
    public int TargetZ;
    public int PickupAmount;
    public float ChiselX;
    public float ChiselY;
    public float ChiselZ;
    public string RecipeId;
    public int ClayU;
    public int ClayV;
    public int ToolMode;
}

[Serializable]
public struct WorldCommandResultMessage
{
    public bool Success;
    public string Message;
    public bool HasClayWorksiteKey;
    public int ClayAnchorX;
    public int ClayAnchorY;
    public int ClayAnchorZ;
    public int ClayFaceX;
    public int ClayFaceY;
    public int ClayFaceZ;
    public bool HasClayEditResult;
    public bool ClayEditChanged;
    public bool ClayLayerCompleted;
    public bool ClayRecipeCompleted;
}

[Serializable]
public struct ClayWorksiteChangedMessage
{
    public int AnchorX;
    public int AnchorY;
    public int AnchorZ;
    public int FaceX;
    public int FaceY;
    public int FaceZ;

    public ClayWorksiteKey Key => new(
        new Vector3Int(AnchorX, AnchorY, AnchorZ),
        new Vector3Int(FaceX, FaceY, FaceZ));

    public static ClayWorksiteChangedMessage From(ClayWorksiteKey key)
    {
        return new ClayWorksiteChangedMessage
        {
            AnchorX = key.AnchorBlock.x,
            AnchorY = key.AnchorBlock.y,
            AnchorZ = key.AnchorBlock.z,
            FaceX = key.FaceNormal.x,
            FaceY = key.FaceNormal.y,
            FaceZ = key.FaceNormal.z
        };
    }
}

[Serializable]
public struct ChunkBlocksSnapshotMessage
{
    public int ChunkX;
    public int ChunkY;
    public int ChunkZ;
    public int ChunkSize;
    public byte[] Blocks;
}
