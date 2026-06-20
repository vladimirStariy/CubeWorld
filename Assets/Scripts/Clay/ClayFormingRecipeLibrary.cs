using System.Collections.Generic;

public static class ClayFormingRecipeLibrary
{
    private static readonly Dictionary<string, ClayFormingRecipe> Recipes = new();

    static ClayFormingRecipeLibrary()
    {
        RegisterBowl();
    }

    public static IReadOnlyCollection<ClayFormingRecipe> All => Recipes.Values;

    public static bool TryGet(string id, out ClayFormingRecipe recipe)
    {
        return Recipes.TryGetValue(id, out recipe);
    }

    private static void RegisterBowl()
    {
        var bowl = new ClayFormingRecipe(
            "bowl",
            "Bowl",
            HotbarItem.RawClayBowl(),
            new[]
            {
                new[]
                {
                    "  #####  ",
                    "  #####  ",
                    "  #####  ",
                    "  #####  ",
                    "  #####  "
                },
                new[]
                {
                    "  #####  ",
                    "  #   #  ",
                    "  #   #  ",
                    "  #   #  ",
                    "  #####  "
                }
            });

        Recipes[bowl.Id] = bowl;
    }
}
