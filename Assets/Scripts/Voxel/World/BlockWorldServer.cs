using System.Collections.Generic;
using UnityEngine;

public sealed class BlockWorldServer : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private int worldWidth = 100;
    [SerializeField] private int worldDepth = 100;
    [SerializeField] private int worldHeight = 16;
    [SerializeField] private int baseLayerY = 0;
    [SerializeField] private int chunkSize = 16;

    [Header("Visuals")]
    [SerializeField] private Texture2D dirtTexture;
    [SerializeField] private Texture2D grassTexture;
    [SerializeField] private Texture2D blockAtlasTexture;
    [SerializeField] private int chiselResolution = 16;

    private VoxelWorldStorage world;
    private Transform chunksRoot;

    public Texture2D BlockAtlasTexture { get; private set; }

    public int WorldWidth => world.WorldWidth;
    public int WorldDepth => world.WorldDepth;
    public int WorldHeight => world.WorldHeight;

    private void Awake()
    {
        chiselResolution = 16;

        chunksRoot = new GameObject("Chunks").transform;
        chunksRoot.SetParent(transform, false);

        world = new VoxelWorldStorage(
            chunksRoot,
            worldWidth,
            worldDepth,
            worldHeight,
            baseLayerY,
            chunkSize,
            chiselResolution);

        var material = BlockWorldMaterialSetup.CreateBlockMaterial(
            dirtTexture,
            grassTexture,
            blockAtlasTexture,
            out var atlas);
        BlockAtlasTexture = atlas;
        world.SetChunkMaterial(material);

        world.GenerateFlatWorld();
        world.RebuildAllChunks();
    }

    private void Update()
    {
        world.TickFunctionalBlocks(Time.deltaTime);
    }

    public bool TrySetBlock(Vector3Int position, VoxelBlockType blockType)
    {
        return world.TrySetBlock(position, blockType);
    }

    public bool IsInWorld(Vector3Int position)
    {
        return world.IsInWorld(position);
    }

    public bool HasSolidBlockAt(Vector3Int position)
    {
        return world.IsBlockOccupied(position);
    }

    public bool TryQueryBlock(Vector3Int position, out BlockQueryResult result)
    {
        return world.TryQueryBlock(position, out result);
    }

    public bool TryGetOutlineSegments(Vector3Int blockPosition, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetOutlineSegments(world, blockPosition, segments);
    }

    public bool TryGetHitFaceOutline(Vector3Int blockPosition, Vector3 faceNormal, List<LineSegment> segments)
    {
        return BlockOutlineBuilder.TryGetHitFaceOutline(world, blockPosition, faceNormal, segments);
    }

    public bool TryChiselBlock(Vector3Int blockPosition, Vector3 localPoint)
    {
        return world.TryChiselBlock(blockPosition, localPoint);
    }

    public bool TryGetCampfireState(Vector3Int position, out CampfireState state)
    {
        return world.TryGetCampfireState(position, out state);
    }

    public bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message)
    {
        return world.TryUseItemOnTarget(hitBlock, faceNormal, item, out message);
    }

    public bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        return world.TryBreakCampfireAssembly(hitBlock, faceNormal, out message);
    }

    public bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state)
    {
        return world.TryGetCampfireAssemblyState(clickedBlock, faceNormal, out state);
    }

    public void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer)
    {
        world.CopyCampfireAssemblySnapshots(buffer);
    }

    public bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message)
    {
        return world.TryInteractCampfire(position, interaction, out state, out message);
    }
}
