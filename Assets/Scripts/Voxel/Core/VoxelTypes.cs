using UnityEngine;

public readonly struct LineSegment
{
    public readonly Vector3 From;
    public readonly Vector3 To;

    public LineSegment(Vector3 from, Vector3 to)
    {
        From = from;
        To = to;
    }
}

public readonly struct BlockQueryResult
{
    public readonly VoxelBlockType ChunkType;
    public readonly bool IsChiseled;
    public readonly int SolidMicroCells;
    public readonly int MicroResolution;

    public BlockQueryResult(VoxelBlockType chunkType, bool isChiseled, int solidMicroCells, int microResolution)
    {
        ChunkType = chunkType;
        IsChiseled = isChiseled;
        SolidMicroCells = solidMicroCells;
        MicroResolution = microResolution;
    }
}
