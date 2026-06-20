using System;

[Flags]
public enum ItemCapabilities
{
    None = 0,
    GroundPlaceable = 1 << 0,
    CampfireFoundation = 1 << 1,
    CampfireAssemblyStick = 1 << 2,
    CampfireIgniter = 1 << 3,
    ChiselTool = 1 << 4,
    ClayMaterial = 1 << 5,
    PlaceableBlock = 1 << 6
}

public static class ItemCapabilitiesExtensions
{
    public static bool Has(this ItemCapabilities capabilities, ItemCapabilities flag) =>
        (capabilities & flag) != 0;

    public static bool IsAssemblyComponent(this ItemCapabilities capabilities) =>
        capabilities.Has(ItemCapabilities.CampfireFoundation)
        || capabilities.Has(ItemCapabilities.CampfireAssemblyStick)
        || capabilities.Has(ItemCapabilities.CampfireIgniter);
}
