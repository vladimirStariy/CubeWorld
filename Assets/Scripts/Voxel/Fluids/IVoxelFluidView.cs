using UnityEngine;

public interface IVoxelFluidView
{
    FluidCell GetFluid(Vector3Int worldPosition);
}
