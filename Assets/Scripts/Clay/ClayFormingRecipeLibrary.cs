using System.Collections.Generic;

public static class ClayFormingRecipeLibrary
{
    public static IReadOnlyCollection<ClayFormingRecipe> All =>
        ClayFormingRecipeRegistry.Active?.All ?? System.Array.Empty<ClayFormingRecipe>();

    public static bool TryGet(string id, out ClayFormingRecipe recipe)
    {
        if (ClayFormingRecipeRegistry.Active != null)
        {
            return ClayFormingRecipeRegistry.Active.TryGet(id, out recipe);
        }

        recipe = null;
        return false;
    }
}
