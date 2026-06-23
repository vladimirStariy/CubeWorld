using System.Collections.Generic;
using UnityEngine;

public sealed class BlockShapeDefinition
{
    public BlockShapeDefinition(
        ContentId id,
        BlockShapeRenderKind renderKind,
        BlockOcclusionMode occlusionMode,
        IReadOnlyList<BlockOcclusionBox> occlusionBoxes,
        IReadOnlyList<BlockShapeElement> elements,
        Bounds localBounds)
    {
        Id = id;
        RenderKind = renderKind;
        OcclusionMode = occlusionMode;
        OcclusionBoxes = occlusionBoxes;
        Elements = elements;
        LocalBounds = localBounds;
    }

    public ContentId Id { get; }
    public BlockShapeRenderKind RenderKind { get; }
    public BlockOcclusionMode OcclusionMode { get; }
    public IReadOnlyList<BlockOcclusionBox> OcclusionBoxes { get; }
    public IReadOnlyList<BlockShapeElement> Elements { get; }
    public Bounds LocalBounds { get; }
}
