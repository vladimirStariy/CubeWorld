using UnityEngine;

public static class FluidTextureLibrary
{
    public static int GetAtlasSlot(FluidType fluidType, int faceIndex) => 0;

    public static Color GetTint(FluidType fluidType)
    {
        return fluidType switch
        {
            FluidType.SaltWater => new Color(0.15f, 0.42f, 0.72f, 0.72f),
            FluidType.FreshWater => new Color(0.12f, 0.55f, 0.82f, 0.68f),
            _ => new Color(0.2f, 0.45f, 0.8f, 0.7f)
        };
    }
}
