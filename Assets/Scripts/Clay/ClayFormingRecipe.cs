using System;

public sealed class ClayFormingRecipe
{
    public string Id { get; }
    public string DisplayName { get; }
    public HotbarItem OutputItem { get; }
    public bool[][,] LayerTargets { get; }

    public int LayerCount => LayerTargets.Length;

    public ClayFormingRecipe(string id, string displayName, HotbarItem outputItem, string[][] pattern)
    {
        Id = id;
        DisplayName = displayName;
        OutputItem = outputItem;
        LayerTargets = ParsePattern(pattern);
    }

    public bool IsTargetSolid(int layer, int u, int v)
    {
        if (layer < 0 || layer >= LayerCount)
        {
            return false;
        }

        return LayerTargets[layer][u, v];
    }

    private static bool[][,] ParsePattern(string[][] pattern)
    {
        var layers = new bool[pattern.Length][,];
        for (int layer = 0; layer < pattern.Length; layer++)
        {
            layers[layer] = ParseLayer(pattern[layer]);
        }

        return layers;
    }

    private static bool[,] ParseLayer(string[] rows)
    {
        var grid = new bool[ClayFormingConstants.GridSize, ClayFormingConstants.GridSize];
        if (rows == null || rows.Length == 0)
        {
            return grid;
        }

        var height = rows.Length;
        var width = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            width = Math.Max(width, rows[i]?.Length ?? 0);
        }

        var offsetU = (ClayFormingConstants.GridSize - width) / 2;
        var offsetV = (ClayFormingConstants.GridSize - height) / 2;

        for (int row = 0; row < height; row++)
        {
            var line = rows[row] ?? string.Empty;
            for (int column = 0; column < line.Length; column++)
            {
                var u = offsetU + column;
                var v = offsetV + row;
                if (u < 0 || u >= ClayFormingConstants.GridSize || v < 0 || v >= ClayFormingConstants.GridSize)
                {
                    continue;
                }

                var ch = line[column];
                grid[u, v] = ch == '#' || ch == '+';
            }
        }

        return grid;
    }
}
