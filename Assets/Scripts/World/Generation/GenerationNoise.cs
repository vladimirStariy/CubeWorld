using System;

/// <summary>
/// Thread-safe math/noise for background chunk generation (no UnityEngine API).
/// </summary>
internal static class GenerationNoise
{
    private static readonly int[] Permutation = BuildPermutation();

    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    public static int RoundToInt(float value) => (int)MathF.Round(value);

    public static float Perlin(float x, float y)
    {
        var x0 = (int)MathF.Floor(x) & 255;
        var y0 = (int)MathF.Floor(y) & 255;
        x -= MathF.Floor(x);
        y -= MathF.Floor(y);

        var u = Fade(x);
        var v = Fade(y);

        var aa = Permutation[Permutation[x0] + y0];
        var ab = Permutation[Permutation[x0] + y0 + 1];
        var ba = Permutation[Permutation[x0 + 1] + y0];
        var bb = Permutation[Permutation[x0 + 1] + y0 + 1];

        var x1 = Lerp(Grad(aa, x, y), Grad(ba, x - 1f, y), u);
        var x2 = Lerp(Grad(ab, x, y - 1f), Grad(bb, x - 1f, y - 1f), u);
        return Lerp(x1, x2, v);
    }

    private static int[] BuildPermutation()
    {
        var source = new int[256];
        for (int i = 0; i < 256; i++)
        {
            source[i] = i;
        }

        var random = new Random(0);
        for (int i = 255; i > 0; i--)
        {
            var swap = random.Next(i + 1);
            (source[i], source[swap]) = (source[swap], source[i]);
        }

        var table = new int[512];
        for (int i = 0; i < 512; i++)
        {
            table[i] = source[i & 255];
        }

        return table;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float Grad(int hash, float x, float y)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h == 12 || h == 14 ? x : 0f;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
