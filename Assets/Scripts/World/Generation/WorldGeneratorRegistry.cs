using System.Collections.Generic;

public sealed class WorldGeneratorRegistry
{
    private readonly Dictionary<ContentId, IChunkWorldGenerator> byId = new();

    public ContentId DefaultGeneratorId { get; set; } = new("cubeworld", "planet");

    public void Register(IChunkWorldGenerator generator)
    {
        if (generator == null)
        {
            return;
        }

        byId[generator.Id] = generator;
    }

    public bool TryGet(ContentId id, out IChunkWorldGenerator generator) => byId.TryGetValue(id, out generator);

    public bool TryGetDefault(out IChunkWorldGenerator generator) => TryGet(DefaultGeneratorId, out generator);
}
