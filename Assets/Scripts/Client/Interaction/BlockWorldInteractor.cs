using UnityEngine;

public sealed class BlockWorldInteractor
{
    private readonly BlockWorldServer server;
    private readonly Camera playerCamera;
    private readonly CharacterController playerCharacterController;
    private readonly CreativeInventory creativeInventory;
    private readonly float interactDistance;
    private readonly System.Action onWorldChanged;
    private readonly BlockEntityUiController blockEntityUi;

    public BlockWorldInteractor(
        BlockWorldServer server,
        Camera playerCamera,
        CharacterController playerCharacterController,
        CreativeInventory creativeInventory,
        float interactDistance,
        System.Action onWorldChanged,
        BlockEntityUiController blockEntityUi)
    {
        this.server = server;
        this.playerCamera = playerCamera;
        this.playerCharacterController = playerCharacterController;
        this.creativeInventory = creativeInventory;
        this.interactDistance = interactDistance;
        this.onWorldChanged = onWorldChanged;
        this.blockEntityUi = blockEntityUi;
    }

    public bool TryUseSelectedItem()
    {
        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        var hitBlock = BlockWorldTargeting.GetHitBlockPosition(hit);
        if (TryOpenBlockEntityUi(hitBlock))
        {
            return true;
        }

        if (creativeInventory == null || !creativeInventory.TryGetSelectedItem(out var item))
        {
            return false;
        }

        if (item.IsAssemblyComponent)
        {
            if (!server.TryUseItemOnTarget(hitBlock, hit.normal, item, out var message))
            {
                return false;
            }

            Debug.Log(message);
            onWorldChanged?.Invoke();
            return true;
        }

        if (!item.IsPlaceableBlock)
        {
            return false;
        }

        var targetPosition = BlockWorldTargeting.GetAdjacentBlockPosition(hit);
        if (WouldIntersectPlayer(targetPosition, item.BlockType))
        {
            return false;
        }

        if (!server.TrySetBlock(targetPosition, item.BlockType))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryUseOrBreakBlock()
    {
        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        var targetPosition = BlockWorldTargeting.GetHitBlockPosition(hit);

        if (server.TryBreakCampfireAssembly(targetPosition, hit.normal, out var assemblyMessage))
        {
            Debug.Log(assemblyMessage);
            onWorldChanged?.Invoke();
            return true;
        }

        if (!server.TrySetBlock(targetPosition, VoxelBlockType.Air))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryChiselBlock()
    {
        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        var blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        var pointInsideSurface = hit.point - hit.normal * 0.001f;
        var localPoint = BlockWorldTargeting.WorldToLocalBlockPoint(pointInsideSurface, blockPosition);
        if (!server.TryChiselBlock(blockPosition, localPoint))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryGetLookTargetInfo(out Vector3Int blockPosition, out Vector3 faceNormal, out BlockQueryResult blockInfo)
    {
        blockPosition = default;
        faceNormal = default;
        blockInfo = default;

        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        faceNormal = hit.normal;
        return server.TryQueryBlock(blockPosition, out blockInfo);
    }

    private bool TryOpenBlockEntityUi(Vector3Int blockPosition)
    {
        return blockEntityUi != null && blockEntityUi.TryOpen(blockPosition);
    }

    private bool WouldIntersectPlayer(Vector3Int blockPosition, VoxelBlockType blockType)
    {
        if (playerCharacterController == null)
        {
            return false;
        }

        return VoxelBlockShapes.GetWorldBounds(blockPosition, blockType).Intersects(playerCharacterController.bounds);
    }
}
