using System.Collections.Generic;

public sealed class BiomeRegistry
{
    private readonly List<BiomeDefinition> biomes = new();

    public IReadOnlyList<BiomeDefinition> Biomes => biomes;

    public void Register(BiomeDefinition biome)
    {
        if (biome == null)
        {
            return;
        }

        for (int i = 0; i < biomes.Count; i++)
        {
            if (biomes[i].Id == biome.Id)
            {
                biomes[i] = biome;
                return;
            }
        }

        biomes.Add(biome);
    }

    public BiomeDefinition Resolve(ClimateSample climate)
    {
        BiomeDefinition best = null;
        var bestDistance = float.MaxValue;

        for (int i = 0; i < biomes.Count; i++)
        {
            var biome = biomes[i];
            if (climate.Temperature < biome.MinTemperature || climate.Temperature > biome.MaxTemperature)
            {
                continue;
            }

            var center = (biome.MinTemperature + biome.MaxTemperature) * 0.5f;
            var distance = System.Math.Abs(climate.Temperature - center) - biome.Priority * 0.01f;
            if (distance < bestDistance)
            {
                bestDistance = (float)distance;
                best = biome;
            }
        }

        if (best != null)
        {
            return best;
        }

        return ResolveFallback(climate.Temperature);
    }

    private BiomeDefinition ResolveFallback(float temperature)
    {
        BiomeDefinition closest = null;
        var closestDistance = float.MaxValue;

        for (int i = 0; i < biomes.Count; i++)
        {
            var biome = biomes[i];
            var center = (biome.MinTemperature + biome.MaxTemperature) * 0.5f;
            var distance = System.Math.Abs(temperature - center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = biome;
            }
        }

        return closest ?? biomes[0];
    }
}
