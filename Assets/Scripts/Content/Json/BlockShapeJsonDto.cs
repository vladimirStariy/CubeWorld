using System;

[Serializable]
public sealed class BlockShapeJson
{
    public string id;
    public string parent;
    public string render;
    public string occlusion;
    public BlockShapeElementJson[] elements;
    public BlockOcclusionBoxJson[] occlusionBoxes;
}

[Serializable]
public sealed class BlockShapeElementJson
{
    public string name;
    public float[] from;
    public float[] to;
}

[Serializable]
public sealed class BlockOcclusionBoxJson
{
    public float[] from;
    public float[] to;
}
