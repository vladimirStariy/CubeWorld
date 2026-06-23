using UnityEngine;

internal static class OceanGenerationHelper
{
    public static void FillOceanColumn(
        IChunkGenerationContext context,
        int worldX,
        int worldZ,
        int surfaceY,
        int chunkOriginY,
        int chunkSize)
    {
        var settings = context.Settings;
        if (surfaceY >= settings.SeaLevel)
        {
            return;
        }

        for (int worldY = surfaceY + 1; worldY <= settings.SeaLevel; worldY++)
        {
            if (worldY < chunkOriginY || worldY >= chunkOriginY + chunkSize)
            {
                continue;
            }

            context.SetFluid(
                new Vector3Int(worldX, worldY, worldZ),
                FluidCell.Source(FluidType.SaltWater));
        }
    }
}
