using System;
using System.Collections.Generic;
using UnityEngine;

public interface IWorldSimulation : IVoxelBlockView
{
    int LoadedChunkCount { get; }
    bool HasPendingStreaming { get; }
    int WorldHeight { get; }
    int MinWorldY { get; }

    event Action<Vector3Int> ChunkPresentationChanged;
    event Action<Vector3Int> ChunkUnloaded;

    void ConfigureStreaming(ChunkStreamingSettings settings);
    void InitializeWorldGeneration(
        WorldGeneratorRegistry registry,
        WorldSettings settings,
        ItemRegistry items,
        BiomeRegistry biomes);
    void PrimeSpawnArea(Vector3 spawnPosition);
    void UpdateChunkStreaming(Vector3 playerWorldPosition);
    void ProcessChunkLoadRequests(int maxRequests);
    void ProcessGenerationBudget(Vector3 playerWorldPosition, float maxMilliseconds);
    void ProcessSimulationQueues();
    void SetItemUseRegistry(ItemUseRegistry registry);
    void TickFunctionalBlocks(float deltaTime);

    bool TrySetBlock(Vector3Int position, VoxelBlockType blockType);
    bool TryQueryBlock(Vector3Int position, out BlockQueryResult result);
    bool TryGetBiomeAt(Vector3Int worldPosition, out BiomeDefinition biome, out ClimateSample climate);

    bool TryBeginChiselBlock(Vector3Int blockPosition);
    bool TryChiselRemoveVoxel(Vector3Int blockPosition, Vector3 localPoint);
    bool TryChiselAddVoxel(Vector3Int blockPosition, Vector3 localPoint);

    bool TryGetCampfireState(Vector3Int position, out CampfireState state);
    bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message);
    bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message);
    bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message);
    bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state);
    void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer);

    bool TryGetChunkBlocks(Vector3Int chunkCoord, out ChunkBlockData chunkBlocks);
}
