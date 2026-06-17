using System.Collections.Generic;
using UnityEngine;

public sealed class VoxelWorldStorage : IVoxelBlockView
{
    private readonly Dictionary<Vector3Int, ChunkData> chunks = new();
    private readonly HashSet<Vector3Int> dirtyChunks = new();
    private readonly Dictionary<Vector3Int, ChiseledBlockData> chiseledBlocks = new();
    private readonly Dictionary<Vector3Int, CampfireBlockEntity> campfires = new();
    private readonly Dictionary<Vector3Int, CampfireAssembly> campfireAssemblies = new();
    private readonly Transform chunksRoot;
    private readonly int worldWidth;
    private readonly int worldDepth;
    private readonly int worldHeight;
    private readonly int baseLayerY;

    private Material chunkMaterial;

    public int ChunkSize { get; }
    public int ChiselResolution { get; }
    public int WorldWidth => worldWidth;
    public int WorldDepth => worldDepth;
    public int WorldHeight => worldHeight;

    public VoxelWorldStorage(
        Transform chunksRoot,
        int worldWidth,
        int worldDepth,
        int worldHeight,
        int baseLayerY,
        int chunkSize,
        int chiselResolution)
    {
        this.chunksRoot = chunksRoot;
        this.worldWidth = worldWidth;
        this.worldDepth = worldDepth;
        this.worldHeight = worldHeight;
        this.baseLayerY = baseLayerY;
        ChunkSize = chunkSize;
        ChiselResolution = chiselResolution;
    }

    public void SetChunkMaterial(Material material)
    {
        chunkMaterial = material;
        foreach (var chunk in chunks.Values)
        {
            var renderer = chunk.Filter.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    public void GenerateFlatWorld()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldDepth; z++)
            {
                SetBlockFast(new Vector3Int(x, baseLayerY, z), VoxelBlockType.GrassBlock);
            }
        }
    }

    public void RebuildAllChunks()
    {
        foreach (var chunk in chunks.Values)
        {
            ChunkMeshBuilder.BuildChunkMesh(this, chunk);
        }

        dirtyChunks.Clear();
    }

    public bool TrySetBlock(Vector3Int position, VoxelBlockType blockType)
    {
        if (!IsInWorld(position))
        {
            return false;
        }

        var hadChiseledBlock = chiseledBlocks.ContainsKey(position);
        chiseledBlocks.Remove(position);

        var chunkCoord = WorldToChunk(position);
        var local = WorldToLocal(position);
        var chunk = GetOrCreateChunk(chunkCoord);
        var oldType = chunk.GetBlock(local);

        if (oldType == blockType && !hadChiseledBlock)
        {
            return false;
        }

        if (oldType != blockType)
        {
            chunk.SetBlock(local, blockType);
            UpdateFunctionalBlockEntities(position, oldType, blockType);
            RemoveCampfireAssembliesAffectedByBlockChange(position, oldType, blockType);
        }

        MarkChunkDirty(chunkCoord);
        MarkNeighborChunksDirtyIfBorder(local, chunkCoord);
        RebuildDirtyChunks();
        return true;
    }

    public bool TryGetCampfireState(Vector3Int position, out CampfireState state)
    {
        if (campfires.TryGetValue(position, out var campfire))
        {
            state = campfire.Snapshot();
            return true;
        }

        state = default;
        return false;
    }

    public bool TryGetCampfireAssemblyState(Vector3Int clickedBlock, Vector3 faceNormal, out CampfireAssemblyState state)
    {
        if (TryResolveCampfireAssembly(clickedBlock, faceNormal, out _, out var assembly))
        {
            state = assembly.Snapshot();
            return true;
        }

        state = default;
        return false;
    }

    public void CopyCampfireAssemblySnapshots(List<CampfireAssemblySnapshot> buffer)
    {
        buffer.Clear();
        foreach (var pair in campfireAssemblies)
        {
            buffer.Add(new CampfireAssemblySnapshot(pair.Key, pair.Value.Snapshot()));
        }
    }

    public bool TryUseItemOnTarget(Vector3Int hitBlock, Vector3 faceNormal, HotbarItem item, out string message)
    {
        switch (item.Kind)
        {
            case ItemKind.GrassBundle:
                return TryPlaceCampfireFoundation(hitBlock, faceNormal, out message);
            case ItemKind.Stick:
                if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out _, out var stickAssembly))
                {
                    message = "No campfire foundation here.";
                    return false;
                }

                return stickAssembly.TryAddStick(out message);
            case ItemKind.Flint:
                if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out var lightAnchor, out var lightAssembly))
                {
                    message = "No campfire foundation here.";
                    return false;
                }

                if (!lightAssembly.CanLight())
                {
                    message = $"Need {CampfireAssembly.RequiredSticks} sticks before lighting.";
                    return false;
                }

                return TryLightCampfireAssembly(lightAnchor, out message);
            default:
                message = "Cannot use this item here.";
                return false;
        }
    }

    public bool TryBreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal, out string message)
    {
        if (!TryResolveCampfireAssembly(hitBlock, faceNormal, out var anchor, out _))
        {
            message = "No campfire assembly here.";
            return false;
        }

        campfireAssemblies.Remove(anchor);
        message = "Removed campfire assembly.";
        return true;
    }

    public bool TryInteractCampfire(Vector3Int position, CampfireInteraction interaction, out CampfireState state, out string message)
    {
        if (!campfires.TryGetValue(position, out var campfire))
        {
            message = "No campfire at target.";
            state = default;
            return false;
        }

        var changed = campfire.TryInteract(interaction, out message);
        state = campfire.Snapshot();
        return changed;
    }

    public void TickFunctionalBlocks(float deltaTime)
    {
        if (deltaTime <= 0f || campfires.Count == 0)
        {
            return;
        }

        foreach (var campfire in campfires.Values)
        {
            campfire.Tick(deltaTime);
        }
    }

    public bool TryChiselBlock(Vector3Int blockPosition, Vector3 localPoint)
    {
        if (!IsInWorld(blockPosition) || !IsBlockOccupied(blockPosition))
        {
            return false;
        }

        if (GetBlock(blockPosition) == VoxelBlockType.Campfire)
        {
            return false;
        }

        if (!chiseledBlocks.TryGetValue(blockPosition, out var chiseled))
        {
            if (GetBlock(blockPosition) == VoxelBlockType.Air)
            {
                return false;
            }

            chiseled = new ChiseledBlockData(ChiselResolution, blockPosition);
            chiseled.FillSolid();
            chiseledBlocks[blockPosition] = chiseled;
            SetBlockFast(blockPosition, VoxelBlockType.Air);
        }

        var cell = ChiseledBlockData.LocalPointToCell(localPoint, ChiselResolution);
        if (!chiseled.SetCell(cell.x, cell.y, cell.z, false))
        {
            return false;
        }

        if (!chiseled.HasAnySolid())
        {
            chiseledBlocks.Remove(blockPosition);
        }

        MarkChunkDirty(WorldToChunk(blockPosition));
        MarkNeighborChunksDirtyIfBorder(WorldToLocal(blockPosition), WorldToChunk(blockPosition));
        RebuildDirtyChunks();
        return true;
    }

    public bool TryQueryBlock(Vector3Int position, out BlockQueryResult result)
    {
        result = default;
        if (!IsInWorld(position))
        {
            return false;
        }

        if (chiseledBlocks.TryGetValue(position, out var chiseled))
        {
            var solidCells = chiseled.CountSolidCells();
            if (solidCells == 0)
            {
                return false;
            }

            result = new BlockQueryResult(VoxelBlockType.Dirt, true, solidCells, chiseled.Resolution);
            return true;
        }

        var chunkType = GetBlock(position);
        if (chunkType == VoxelBlockType.Air)
        {
            return false;
        }

        result = new BlockQueryResult(chunkType, false, 0, 0);
        return true;
    }

    public bool IsInWorld(Vector3Int position)
    {
        return position.x >= 0 && position.x < worldWidth &&
               position.y >= 0 && position.y < worldHeight &&
               position.z >= 0 && position.z < worldDepth;
    }

    public bool IsBlockOccupied(Vector3Int position)
    {
        if (chiseledBlocks.TryGetValue(position, out var chiseled))
        {
            return chiseled.HasAnySolid();
        }

        return GetBlock(position) != VoxelBlockType.Air;
    }

    public VoxelBlockType GetBlock(Vector3Int position)
    {
        if (!IsInWorld(position))
        {
            return VoxelBlockType.Air;
        }

        var chunkCoord = WorldToChunk(position);
        if (!chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return VoxelBlockType.Air;
        }

        return chunk.GetBlock(WorldToLocal(position));
    }

    public bool IsFullCubeBlock(Vector3Int position)
    {
        if (chiseledBlocks.ContainsKey(position))
        {
            return false;
        }

        return VoxelBlockShapes.IsFullCube(GetBlock(position));
    }

    public bool HasChiseledBlock(Vector3Int position)
    {
        return chiseledBlocks.ContainsKey(position);
    }

    public bool TryGetChiseledBlock(Vector3Int position, out ChiseledBlockData block)
    {
        return chiseledBlocks.TryGetValue(position, out block);
    }

    public bool IsFaceOccludedByNeighbor(Vector3Int neighborPosition, int currentFaceIndex)
    {
        var neighborType = GetBlock(neighborPosition);
        if (neighborType != VoxelBlockType.Air)
        {
            if (VoxelBlockShapes.IsCustomMeshBlock(neighborType))
            {
                return false;
            }

            if (VoxelBlockShapes.IsBottomSlab(neighborType))
            {
                return false;
            }

            return true;
        }

        if (!chiseledBlocks.TryGetValue(neighborPosition, out var chiseled))
        {
            return false;
        }

        var neighborFace = VoxelConstants.OppositeFace[currentFaceIndex];
        return chiseled.IsSideFullySolid(neighborFace);
    }

    public bool IsMicroFaceOccludedByNeighbor(Vector3Int neighborBlockPos, int currentFace, int x, int y, int z, int resolution)
    {
        var neighborType = GetBlock(neighborBlockPos);
        if (neighborType != VoxelBlockType.Air)
        {
            if (VoxelBlockShapes.IsCustomMeshBlock(neighborType))
            {
                return false;
            }

            return true;
        }

        if (!chiseledBlocks.TryGetValue(neighborBlockPos, out var chiseledNeighbor))
        {
            return false;
        }

        return currentFace switch
        {
            0 => chiseledNeighbor.GetCell(0, y, z),
            1 => chiseledNeighbor.GetCell(resolution - 1, y, z),
            2 => chiseledNeighbor.GetCell(x, 0, z),
            3 => chiseledNeighbor.GetCell(x, resolution - 1, z),
            4 => chiseledNeighbor.GetCell(x, y, 0),
            5 => chiseledNeighbor.GetCell(x, y, resolution - 1),
            _ => false
        };
    }

    public Vector3Int LocalToWorld(Vector3Int chunkCoord, Vector3Int local)
    {
        return new Vector3Int(
            chunkCoord.x * ChunkSize + local.x,
            chunkCoord.y * ChunkSize + local.y,
            chunkCoord.z * ChunkSize + local.z);
    }

    private void SetBlockFast(Vector3Int position, VoxelBlockType blockType)
    {
        var chunkCoord = WorldToChunk(position);
        var local = WorldToLocal(position);
        var chunk = GetOrCreateChunk(chunkCoord);
        chunk.SetBlock(local, blockType);
        MarkChunkDirty(chunkCoord);
    }

    private ChunkData GetOrCreateChunk(Vector3Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out var existing))
        {
            return existing;
        }

        var chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
        chunkObject.transform.SetParent(chunksRoot, false);
        chunkObject.transform.position = new Vector3(
            chunkCoord.x * ChunkSize,
            chunkCoord.y * ChunkSize,
            chunkCoord.z * ChunkSize);

        var filter = chunkObject.AddComponent<MeshFilter>();
        var renderer = chunkObject.AddComponent<MeshRenderer>();
        var collider = chunkObject.AddComponent<MeshCollider>();
        if (chunkMaterial != null)
        {
            renderer.sharedMaterial = chunkMaterial;
        }

        var chunk = new ChunkData(ChunkSize, chunkCoord, filter, collider);
        chunks.Add(chunkCoord, chunk);
        return chunk;
    }

    private void MarkChunkDirty(Vector3Int chunkCoord)
    {
        dirtyChunks.Add(chunkCoord);
    }

    private void MarkNeighborChunksDirtyIfBorder(Vector3Int localPos, Vector3Int chunkCoord)
    {
        if (localPos.x == 0) MarkChunkDirty(chunkCoord + Vector3Int.left);
        if (localPos.x == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector3Int.right);
        if (localPos.y == 0) MarkChunkDirty(chunkCoord + Vector3Int.down);
        if (localPos.y == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector3Int.up);
        if (localPos.z == 0) MarkChunkDirty(chunkCoord + new Vector3Int(0, 0, -1));
        if (localPos.z == ChunkSize - 1) MarkChunkDirty(chunkCoord + new Vector3Int(0, 0, 1));
    }

    private void RebuildDirtyChunks()
    {
        foreach (var chunkCoord in dirtyChunks)
        {
            if (chunks.TryGetValue(chunkCoord, out var chunk))
            {
                ChunkMeshBuilder.BuildChunkMesh(this, chunk);
            }
        }

        dirtyChunks.Clear();
    }

    private bool TryPlaceCampfireFoundation(Vector3Int foundationBlock, Vector3 faceNormal, out string message)
    {
        if (!WorldItemInteraction.IsTopFaceHit(faceNormal))
        {
            message = "Place grass on top of a block.";
            return false;
        }

        if (!IsBlockOccupied(foundationBlock))
        {
            message = "Need a solid block for the campfire.";
            return false;
        }

        var anchor = WorldItemInteraction.GetAssemblyAnchor(foundationBlock);
        if (!IsInWorld(anchor))
        {
            message = "Not enough space above.";
            return false;
        }

        if (GetBlock(anchor) != VoxelBlockType.Air || campfireAssemblies.ContainsKey(anchor))
        {
            message = "Space above is blocked.";
            return false;
        }

        campfireAssemblies[anchor] = new CampfireAssembly(anchor, foundationBlock);
        message = "Campfire foundation placed. Add sticks.";
        return true;
    }

    private bool TryLightCampfireAssembly(Vector3Int anchorPosition, out string message)
    {
        if (!campfireAssemblies.TryGetValue(anchorPosition, out var assembly))
        {
            message = "No campfire here.";
            return false;
        }

        if (!assembly.CanLight())
        {
            message = $"Need {CampfireAssembly.RequiredSticks} sticks before lighting.";
            return false;
        }

        campfireAssemblies.Remove(anchorPosition);

        if (GetBlock(anchorPosition) != VoxelBlockType.Air)
        {
            message = "Space is blocked.";
            return false;
        }

        if (!TrySetBlock(anchorPosition, VoxelBlockType.Campfire))
        {
            message = "Could not place campfire.";
            return false;
        }

        if (campfires.TryGetValue(anchorPosition, out var campfire))
        {
            campfire.StartLit(8f);
        }

        message = "Campfire lit!";
        return true;
    }

    private bool TryResolveCampfireAssembly(Vector3Int clickedBlock, Vector3 faceNormal, out Vector3Int anchor, out CampfireAssembly assembly)
    {
        assembly = null;
        if (WorldItemInteraction.TryResolveAssemblyAnchor(
                clickedBlock,
                faceNormal,
                campfireAssemblies.ContainsKey,
                out anchor) &&
            campfireAssemblies.TryGetValue(anchor, out assembly))
        {
            return true;
        }

        anchor = default;
        return false;
    }

    private void RemoveCampfireAssembliesAffectedByBlockChange(Vector3Int position, VoxelBlockType oldType, VoxelBlockType newType)
    {
        campfireAssemblies.Remove(position);

        if (oldType != VoxelBlockType.Air && newType == VoxelBlockType.Air)
        {
            campfireAssemblies.Remove(position + Vector3Int.up);
        }
    }

    private void UpdateFunctionalBlockEntities(Vector3Int position, VoxelBlockType oldType, VoxelBlockType newType)
    {
        if (oldType == VoxelBlockType.Campfire && newType != VoxelBlockType.Campfire)
        {
            campfires.Remove(position);
        }

        if (newType == VoxelBlockType.Campfire && oldType != VoxelBlockType.Campfire)
        {
            campfires[position] = new CampfireBlockEntity();
        }
    }

    private Vector3Int WorldToChunk(Vector3Int world)
    {
        return new Vector3Int(
            Mathf.FloorToInt(world.x / (float)ChunkSize),
            Mathf.FloorToInt(world.y / (float)ChunkSize),
            Mathf.FloorToInt(world.z / (float)ChunkSize));
    }

    private Vector3Int WorldToLocal(Vector3Int world)
    {
        return new Vector3Int(
            VoxelConstants.Mod(world.x, ChunkSize),
            VoxelConstants.Mod(world.y, ChunkSize),
            VoxelConstants.Mod(world.z, ChunkSize));
    }
}
