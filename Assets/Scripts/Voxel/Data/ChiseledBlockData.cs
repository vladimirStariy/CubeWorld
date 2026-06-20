using System.Collections.Generic;
using UnityEngine;

public sealed class ChiseledBlockData
{
    private readonly bool[] cells;

    public readonly List<LineSegment> CachedOutlineSegments = new();
    public int Resolution { get; }
    public Vector3Int WorldPosition { get; }
    public VoxelBlockType BlockType { get; }
    public bool OutlineDirty { get; set; } = true;

    public ChiseledBlockData(int resolution, Vector3Int worldPosition, VoxelBlockType blockType)
    {
        Resolution = resolution;
        WorldPosition = worldPosition;
        BlockType = blockType;
        cells = new bool[resolution * resolution * resolution];
    }

    public static Vector3Int LocalPointToCell(Vector3 localPoint, int resolution)
    {
        var clamped = new Vector3(
            Mathf.Clamp01(localPoint.x),
            Mathf.Clamp01(localPoint.y),
            Mathf.Clamp01(localPoint.z));

        var x = Mathf.Clamp(Mathf.FloorToInt(clamped.x * resolution), 0, resolution - 1);
        var y = Mathf.Clamp(Mathf.FloorToInt(clamped.y * resolution), 0, resolution - 1);
        var z = Mathf.Clamp(Mathf.FloorToInt(clamped.z * resolution), 0, resolution - 1);
        return new Vector3Int(x, y, z);
    }

    public void FillSolid()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = true;
        }

        OutlineDirty = true;
    }

    public bool GetCell(int x, int y, int z)
    {
        return cells[ToIndex(x, y, z)];
    }

    public bool SetCell(int x, int y, int z, bool value)
    {
        var index = ToIndex(x, y, z);
        if (cells[index] == value)
        {
            return false;
        }

        cells[index] = value;
        OutlineDirty = true;
        return true;
    }

    public bool HasAnySolid()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i])
            {
                return true;
            }
        }

        return false;
    }

    public int CountSolidCells()
    {
        var count = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i])
            {
                count++;
            }
        }

        return count;
    }

    public bool IsSideFullySolid(int faceIndex)
    {
        return faceIndex switch
        {
            0 => IsPlaneSolidX(Resolution - 1),
            1 => IsPlaneSolidX(0),
            2 => IsPlaneSolidY(Resolution - 1),
            3 => IsPlaneSolidY(0),
            4 => IsPlaneSolidZ(Resolution - 1),
            5 => IsPlaneSolidZ(0),
            _ => false
        };
    }

    private bool IsPlaneSolidX(int x)
    {
        for (int y = 0; y < Resolution; y++)
        {
            for (int z = 0; z < Resolution; z++)
            {
                if (!GetCell(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsPlaneSolidY(int y)
    {
        for (int x = 0; x < Resolution; x++)
        {
            for (int z = 0; z < Resolution; z++)
            {
                if (!GetCell(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsPlaneSolidZ(int z)
    {
        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                if (!GetCell(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int ToIndex(int x, int y, int z)
    {
        return x + Resolution * (y + Resolution * z);
    }
}
