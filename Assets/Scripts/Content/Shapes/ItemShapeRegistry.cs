using System.Collections.Generic;

public sealed class ItemShapeRegistry
{
    public static ItemShapeRegistry Active { get; set; }

    private readonly Dictionary<ContentId, ItemShapeDefinition> shapes = new();

    public IReadOnlyCollection<ItemShapeDefinition> All => shapes.Values;

    public void Register(ItemShapeDefinition definition)
    {
        shapes[definition.Id] = definition;
    }

    public bool TryGet(ContentId id, out ItemShapeDefinition definition)
    {
        return shapes.TryGetValue(id, out definition);
    }
}
