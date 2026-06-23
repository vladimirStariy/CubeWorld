using UnityEngine;

public interface IChunkWorldGenerator
{
    ContentId Id { get; }

    void GenerateChunk(Vector3Int chunkCoord, IChunkGenerationContext context);
}
