using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkTerrainDrawer : MonoBehaviour
{
    private Material terrainMaterial;
    private Material fluidMaterial;
    private IReadOnlyDictionary<Vector3Int, ChunkRenderEntry> renderChunks;

    public void Configure(
        Material material,
        IReadOnlyDictionary<Vector3Int, ChunkRenderEntry> chunks,
        Material fluid = null)
    {
        terrainMaterial = material;
        fluidMaterial = fluid;
        renderChunks = chunks;
    }

    public void SetMaterial(Material material)
    {
        terrainMaterial = material;
    }

    public void SetFluidMaterial(Material material)
    {
        fluidMaterial = material;
    }

    private void LateUpdate()
    {
        if (renderChunks == null)
        {
            return;
        }

        var drawCount = 0;
        using (RuntimeFrameProfiler.Begin("terrain.draw"))
        {
            if (terrainMaterial != null)
            {
                foreach (var pair in renderChunks)
                {
                    var entry = pair.Value;
                    if (!entry.HasVisibleGeometry || entry.Mesh == null)
                    {
                        continue;
                    }

                    drawCount++;
                    Graphics.DrawMesh(
                        entry.Mesh,
                        entry.DrawMatrix,
                        terrainMaterial,
                        gameObject.layer,
                        null,
                        0,
                        null,
                        true,
                        true,
                        false);
                }
            }

            if (fluidMaterial != null)
            {
                foreach (var pair in renderChunks)
                {
                    var entry = pair.Value;
                    if (!entry.HasVisibleFluidGeometry || entry.FluidMesh == null)
                    {
                        continue;
                    }

                    drawCount++;
                    Graphics.DrawMesh(
                        entry.FluidMesh,
                        entry.DrawMatrix,
                        fluidMaterial,
                        gameObject.layer,
                        null,
                        0,
                        null,
                        false,
                        true,
                        false);
                }
            }
        }

        RuntimeFrameProfiler.SetTerrainDrawCount(drawCount);
    }
}
