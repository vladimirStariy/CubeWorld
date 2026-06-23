using System;
using UnityEngine;

[Serializable]
public struct BlockChangeEvent
{
    public Vector3Int Position;
    public VoxelBlockType OldType;
    public VoxelBlockType NewType;
}

public static class WorldSimulationEvents
{
    public static event Action<BlockChangeEvent> BlockChanged;

    public static void RaiseBlockChanged(Vector3Int position, VoxelBlockType oldType, VoxelBlockType newType)
    {
        BlockChanged?.Invoke(new BlockChangeEvent
        {
            Position = position,
            OldType = oldType,
            NewType = newType
        });
    }
}
