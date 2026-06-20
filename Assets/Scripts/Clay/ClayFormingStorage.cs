using System.Collections.Generic;
using UnityEngine;

public struct ClayFormingEditResult
{
    public bool Changed;
    public bool LayerCompleted;
    public bool RecipeCompleted;
    public HotbarItem OutputItem;
    public ClayWorksiteKey CompletionWorksiteKey;
    public bool HasCompletionWorksiteKey;
}

public sealed class ClayFormingStorage
{
    private readonly Dictionary<ClayWorksiteKey, ClayWorksite> worksites = new();
    private ClayFormingRecipeRegistry recipeRegistry;

    public void SetRecipeRegistry(ClayFormingRecipeRegistry registry)
    {
        recipeRegistry = registry;
    }

    public bool TryPlaceWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksiteKey key, out string message)
    {
        key = default;
        message = null;

        var normal = NormalizeFaceNormal(faceNormal);
        if (normal == Vector3Int.zero)
        {
            message = "Invalid clay forming surface.";
            return false;
        }

        key = new ClayWorksiteKey(anchorBlock, normal);
        if (worksites.ContainsKey(key))
        {
            message = "Clay is already placed here.";
            return false;
        }

        worksites[key] = new ClayWorksite(key);
        return true;
    }

    public bool TryFindWorksite(Vector3Int anchorBlock, Vector3Int faceNormal, out ClayWorksite worksite)
    {
        worksite = null;
        var normal = NormalizeFaceNormal(faceNormal);
        if (normal == Vector3Int.zero)
        {
            return false;
        }

        return worksites.TryGetValue(new ClayWorksiteKey(anchorBlock, normal), out worksite);
    }

    public bool TryStartForming(ClayWorksiteKey key, string recipeId, out string message)
    {
        message = null;
        if (!worksites.TryGetValue(key, out var worksite))
        {
            message = "No clay worksite here.";
            return false;
        }

        if (worksite.HasSession)
        {
            message = "This clay pad already has a recipe.";
            return false;
        }

        var recipes = recipeRegistry ?? ClayFormingRecipeRegistry.Active;
        if (recipes == null || !recipes.TryGet(recipeId, out var recipe))
        {
            message = $"Unknown clay recipe: {recipeId}";
            return false;
        }

        worksite.StartSession(recipe);
        return true;
    }

    public void RemoveWorksite(ClayWorksiteKey key)
    {
        worksites.Remove(key);
    }

    public ClayFormingEditResult TryAddClay(ClayWorksiteKey key, int u, int v)
    {
        var result = new ClayFormingEditResult();
        if (!worksites.TryGetValue(key, out var worksite) || worksite.Session == null)
        {
            return result;
        }

        if (!worksite.Session.TryAddClay(u, v, out var layerCompleted, out var recipeCompleted))
        {
            return result;
        }

        return FinalizeEdit(worksite, key, layerCompleted, recipeCompleted);
    }

    public ClayFormingEditResult TryRemoveClay(ClayWorksiteKey key, int u, int v)
    {
        var result = new ClayFormingEditResult();
        if (!worksites.TryGetValue(key, out var worksite) || worksite.Session == null)
        {
            return result;
        }

        if (!worksite.Session.TryRemoveClay(u, v, out var layerCompleted, out var recipeCompleted))
        {
            return result;
        }

        return FinalizeEdit(worksite, key, layerCompleted, recipeCompleted);
    }

    public void SetToolMode(ClayWorksiteKey key, ClayFormingToolMode toolMode)
    {
        if (worksites.TryGetValue(key, out var worksite) && worksite.Session != null)
        {
            worksite.Session.ToolMode = toolMode;
        }
    }

    public void CopySnapshots(List<ClayWorksiteSnapshot> buffer)
    {
        buffer.Clear();
        foreach (var pair in worksites)
        {
            var worksite = pair.Value;
            buffer.Add(new ClayWorksiteSnapshot
            {
                Key = pair.Key,
                BaseLayer = worksite.BaseLayer,
                HasSession = worksite.HasSession,
                Session = worksite.Session
            });
        }
    }

    private ClayFormingEditResult FinalizeEdit(ClayWorksite worksite, ClayWorksiteKey key, bool layerCompleted, bool recipeCompleted)
    {
        var result = new ClayFormingEditResult
        {
            Changed = true,
            LayerCompleted = layerCompleted,
            RecipeCompleted = recipeCompleted
        };

        if (recipeCompleted)
        {
            result.OutputItem = worksite.Session.Recipe.OutputItem;
            result.CompletionWorksiteKey = key;
            result.HasCompletionWorksiteKey = true;
            worksites.Remove(key);
        }

        return result;
    }

    private static Vector3Int NormalizeFaceNormal(Vector3Int faceNormal)
    {
        if (faceNormal == Vector3Int.right
            || faceNormal == Vector3Int.left
            || faceNormal == Vector3Int.up
            || faceNormal == Vector3Int.down
            || faceNormal == new Vector3Int(0, 0, 1)
            || faceNormal == new Vector3Int(0, 0, -1))
        {
            return faceNormal;
        }

        return Vector3Int.zero;
    }
}
