public sealed class BiomeDefinition
{
    public BiomeDefinition(
        ContentId id,
        string displayName,
        float minTemperature,
        float maxTemperature,
        ContentId surfaceBlockId,
        ContentId subsurfaceBlockId,
        ContentId fillerBlockId,
        int priority)
    {
        Id = id;
        DisplayName = displayName;
        MinTemperature = minTemperature;
        MaxTemperature = maxTemperature;
        SurfaceBlockId = surfaceBlockId;
        SubsurfaceBlockId = subsurfaceBlockId;
        FillerBlockId = fillerBlockId;
        Priority = priority;
    }

    public ContentId Id { get; }
    public string DisplayName { get; }
    public float MinTemperature { get; }
    public float MaxTemperature { get; }
    public ContentId SurfaceBlockId { get; }
    public ContentId SubsurfaceBlockId { get; }
    public ContentId FillerBlockId { get; }
    public int Priority { get; }
}
