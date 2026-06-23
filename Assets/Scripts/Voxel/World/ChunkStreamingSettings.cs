using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class ChunkStreamingSettings
{
    [SerializeField] private int viewDistanceChunks = 6;
    [SerializeField] private int unloadDistanceChunks = 8;
    [SerializeField] private int maxGenerationsPerFrame = 1;
    [SerializeField] private int maxMeshBuildsPerFrame = 1;
    [SerializeField] private bool useAdaptiveFramePacing = true;
    [SerializeField] private float targetFrameMilliseconds = 10f;
    [SerializeField] private float minStreamingMillisecondsPerFrame = 1.5f;
    [SerializeField] private float maxStreamingMillisecondsPerFrame = 3f;
    [SerializeField] private int maxChunkLoadRequestsPerFrame = 8;
    [SerializeField] private int syncGenerationRadiusChunks = 2;
    [SerializeField] private int verticalViewDistanceBelowChunks = 4;
    [SerializeField] private int verticalViewDistanceAboveChunks = 3;

    public int ViewDistanceChunks => Mathf.Max(1, viewDistanceChunks);
    public int UnloadDistanceChunks => Mathf.Max(ViewDistanceChunks + 1, unloadDistanceChunks);
    public int MaxGenerationsPerFrame => Mathf.Max(1, maxGenerationsPerFrame);
    public int MaxMeshBuildsPerFrame => Mathf.Max(1, maxMeshBuildsPerFrame);
    public bool UseAdaptiveFramePacing => useAdaptiveFramePacing;
    public float TargetFrameMilliseconds => Mathf.Max(6f, targetFrameMilliseconds);
    public float MinStreamingMillisecondsPerFrame => Mathf.Max(0.5f, minStreamingMillisecondsPerFrame);
    public float MaxStreamingMillisecondsPerFrame => Mathf.Max(MinStreamingMillisecondsPerFrame, maxStreamingMillisecondsPerFrame);
    public int MaxChunkLoadRequestsPerFrame => Mathf.Max(1, maxChunkLoadRequestsPerFrame);
    public int SyncGenerationRadiusChunks => Mathf.Max(0, syncGenerationRadiusChunks);
    public int VerticalViewDistanceBelowChunks => Mathf.Max(0, verticalViewDistanceBelowChunks);
    public int VerticalViewDistanceAboveChunks => Mathf.Max(0, verticalViewDistanceAboveChunks);
}
