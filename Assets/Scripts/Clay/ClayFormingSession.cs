using System.Collections.Generic;
using UnityEngine;

public sealed class ClayFormingSession
{
    private readonly bool[,] baseLayer;
    private readonly List<bool[,]> completedStages = new();

    public Vector3Int AnchorBlock { get; }
    public Vector3Int FaceNormal { get; }
    public ClayFormingRecipe Recipe { get; }
    public ClayFormingToolMode ToolMode { get; set; } = ClayFormingToolMode.Brush1;

    public int CurrentStage { get; private set; }
    public bool[,] PlayerLayer { get; private set; }

    public ClayFormingSession(Vector3Int anchorBlock, Vector3Int faceNormal, ClayFormingRecipe recipe, bool[,] baseLayer)
    {
        AnchorBlock = anchorBlock;
        FaceNormal = faceNormal;
        Recipe = recipe;
        this.baseLayer = baseLayer;
        CurrentStage = 0;
        PlayerLayer = baseLayer;
    }

    public int CurrentWorldLayer => CurrentStage;

    public int CompletedWorldLayer(int stageIndex) => stageIndex;

    public ClayFormingCellState GetCellState(int u, int v)
    {
        var targetSolid = Recipe.IsTargetSolid(CurrentStage, u, v);
        var playerSolid = PlayerLayer[u, v];
        if (targetSolid == playerSolid)
        {
            return targetSolid ? ClayFormingCellState.Correct : ClayFormingCellState.Inactive;
        }

        return targetSolid ? ClayFormingCellState.AddTarget : ClayFormingCellState.RemoveTarget;
    }

    public bool IsStageComplete()
    {
        for (int v = 0; v < ClayFormingConstants.GridSize; v++)
        {
            for (int u = 0; u < ClayFormingConstants.GridSize; u++)
            {
                if (Recipe.IsTargetSolid(CurrentStage, u, v) != PlayerLayer[u, v])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryAdvanceStage(out bool recipeCompleted)
    {
        recipeCompleted = false;
        if (!IsStageComplete())
        {
            return false;
        }

        completedStages.Add((bool[,])PlayerLayer.Clone());

        if (CurrentStage + 1 >= Recipe.LayerCount)
        {
            recipeCompleted = true;
            return true;
        }

        CurrentStage++;
        PlayerLayer = CreateEmptyLayer();
        return true;
    }

    public bool TryAddClay(int centerU, int centerV, out bool stageCompleted, out bool recipeCompleted)
    {
        stageCompleted = false;
        recipeCompleted = false;

        var changed = false;
        ForEachBrushCell(centerU, centerV, (u, v) =>
        {
            if (GetCellState(u, v) != ClayFormingCellState.AddTarget)
            {
                return;
            }

            PlayerLayer[u, v] = true;
            changed = true;
        });

        if (!changed)
        {
            return false;
        }

        return TryAdvanceIfComplete(out stageCompleted, out recipeCompleted);
    }

    public bool TryRemoveClay(int centerU, int centerV, out bool stageCompleted, out bool recipeCompleted)
    {
        stageCompleted = false;
        recipeCompleted = false;

        var changed = false;
        ForEachBrushCell(centerU, centerV, (u, v) =>
        {
            if (GetCellState(u, v) != ClayFormingCellState.RemoveTarget)
            {
                return;
            }

            PlayerLayer[u, v] = false;
            changed = true;
        });

        if (!changed)
        {
            return false;
        }

        return TryAdvanceIfComplete(out stageCompleted, out recipeCompleted);
    }

    public IReadOnlyList<bool[,]> CompletedStages => completedStages;

    private bool TryAdvanceIfComplete(out bool stageCompleted, out bool recipeCompleted)
    {
        stageCompleted = false;
        recipeCompleted = false;

        if (!TryAdvanceStage(out recipeCompleted))
        {
            return true;
        }

        stageCompleted = true;
        return true;
    }

    private void ForEachBrushCell(int centerU, int centerV, System.Action<int, int> visit)
    {
        var size = ToolMode switch
        {
            ClayFormingToolMode.Brush2 => 2,
            ClayFormingToolMode.Brush3 => 3,
            _ => 1
        };

        var startU = centerU - (size - 1) / 2;
        var startV = centerV - (size - 1) / 2;
        for (int du = 0; du < size; du++)
        {
            for (int dv = 0; dv < size; dv++)
            {
                var u = startU + du;
                var v = startV + dv;
                if (!IsInGrid(u, v))
                {
                    continue;
                }

                visit(u, v);
            }
        }
    }

    private static bool IsInGrid(int u, int v)
    {
        return u >= 0 && u < ClayFormingConstants.GridSize && v >= 0 && v < ClayFormingConstants.GridSize;
    }

    private static bool[,] CreateEmptyLayer()
    {
        return new bool[ClayFormingConstants.GridSize, ClayFormingConstants.GridSize];
    }
}
