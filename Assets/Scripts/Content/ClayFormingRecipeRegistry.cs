using System.Collections.Generic;

public sealed class ClayFormingRecipeRegistry
{
    public static ClayFormingRecipeRegistry Active { get; set; }

    private readonly Dictionary<string, ClayFormingRecipe> recipes = new();

    public IReadOnlyCollection<ClayFormingRecipe> All => recipes.Values;

    public void Register(ClayFormingRecipe recipe)
    {
        if (recipe == null || string.IsNullOrWhiteSpace(recipe.Id))
        {
            return;
        }

        recipes[recipe.Id] = recipe;
    }

    public bool TryGet(string id, out ClayFormingRecipe recipe) => recipes.TryGetValue(id, out recipe);
}
