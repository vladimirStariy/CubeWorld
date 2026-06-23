public static class LatitudeClimateModel

{

    /// <summary>

    /// +Z is south (warm). Spawn sits at temperate latitude. -Z is north (cold).

    /// </summary>

    public static ClimateSample Sample(int x, int z, WorldSettings settings)

    {

        var latitudeScale = settings.ClimateLatitudeScale > 0f

            ? settings.ClimateLatitudeScale

            : WorldConstants.DefaultClimateLatitudeScale;

        var normalizedLatitude = (settings.SpawnZ - z) / latitudeScale;

        normalizedLatitude = GenerationNoise.Clamp(normalizedLatitude, -1f, 1f);

        var temperature = settings.TemperateTemperature - normalizedLatitude * settings.PoleTemperatureDrop;

        var humidity = GenerationNoise.Perlin(
            (x + settings.Seed) * 0.008f,
            (z + settings.Seed) * 0.008f);



        return new ClimateSample(temperature, humidity);

    }

}


