public readonly struct ClimateSample
{
    public ClimateSample(float temperature, float humidity)
    {
        Temperature = temperature;
        Humidity = humidity;
    }

    public float Temperature { get; }
    public float Humidity { get; }
}
