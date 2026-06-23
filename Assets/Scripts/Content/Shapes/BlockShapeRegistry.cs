using System.Collections.Generic;

public sealed class BlockShapeRegistry
{
    public static BlockShapeRegistry Active { get; set; }

    private readonly Dictionary<ContentId, BlockShapeDefinition> shapes = new();

    public IReadOnlyCollection<BlockShapeDefinition> All => shapes.Values;

    public void Register(BlockShapeDefinition definition)
    {
        shapes[definition.Id] = definition;
    }

    public bool TryGet(ContentId id, out BlockShapeDefinition definition)
    {
        return shapes.TryGetValue(id, out definition);
    }
}
