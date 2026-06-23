using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkTerrainDrawer : MonoBehaviour
{
    private Material terrainMaterial;
    private IReadOnlyDictionary<Vector3Int, ChunkRenderEntry> renderChunks;

    public void Configure(Material material, IReadOnlyDictionary<Vector3Int, ChunkRenderEntry> chunks)
    {
        terrainMaterial = material;
        renderChunks = chunks;
    }

    public void SetMaterial(Material material)
    {
        terrainMaterial = material;
    }

    private void LateUpdate()
    {
        if (terrainMaterial == null || renderChunks == null)
        {
            return;
        }

        var drawCount = 0;
        using (RuntimeFrameProfiler.Begin("terrain.draw"))
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

        RuntimeFrameProfiler.SetTerrainDrawCount(drawCount);
    }
}
