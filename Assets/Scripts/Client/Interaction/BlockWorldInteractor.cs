using UnityEngine;
using UnityEngine.InputSystem;

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
        if (IsChiselSelected())
        {
            return TryChiselSecondaryAction();
        }

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

        if (item.Kind == ItemKind.Stick && !server.TryHasCampfireAssembly(hitBlock, hit.normal))
        {
            return TryPlaceGroundItem(hitBlock, hit.normal, hit.point, item);
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

        if (item.IsGroundPlaceable)
        {
            if (TryPlaceGroundItem(hitBlock, hit.normal, hit.point, item))
            {
                return true;
            }
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

        if (IsChiselSelected() && server.HasChiseledBlockAt(targetPosition))
        {
            return TryChiselRemoveVoxel();
        }

        if (server.TryBreakCampfireAssembly(targetPosition, hit.normal, out var assemblyMessage))
        {
            Debug.Log(assemblyMessage);
            onWorldChanged?.Invoke();
            return true;
        }

        var pickupAmount = server.ResolveGroundPickupAmount(targetPosition, hit.normal, hit.point, IsShiftHeld());
        if (server.TryProbeGroundPickup(targetPosition, hit.normal, hit.point, pickupAmount, out var probeItem))
        {
            if (creativeInventory != null)
            {
                var canAdd = creativeInventory.GetAddableAmount(probeItem);
                if (canAdd <= 0)
                {
                    Debug.Log("Inventory is full.");
                    return true;
                }

                pickupAmount = Mathf.Min(pickupAmount, canAdd);
            }

            if (server.TryPickupGroundItem(targetPosition, hit.normal, hit.point, pickupAmount, out var pickedItem, out var pickupMessage))
            {
                creativeInventory?.TryAddItem(pickedItem);
                Debug.Log(pickupMessage);
                onWorldChanged?.Invoke();
                return true;
            }
        }

        if (!server.TrySetBlock(targetPosition, VoxelBlockType.Air))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    private bool TryPlaceGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, HotbarItem item)
    {
        if (!server.TryPlaceGroundItem(hitBlock, faceNormal, worldHitPoint, item, out var message))
        {
            Debug.Log(message);
            return false;
        }

        creativeInventory?.TryConsumeFromSelected(1, out _);
        Debug.Log(message);
        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryChiselRemoveVoxel()
    {
        if (!IsChiselSelected() || !TryGetChiselHit(out var blockPosition, out var localPoint, addVoxel: false))
        {
            return false;
        }

        if (!server.HasChiseledBlockAt(blockPosition))
        {
            return false;
        }

        if (!server.TryChiselRemoveVoxel(blockPosition, localPoint))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryChiselAddVoxel()
    {
        if (!IsChiselSelected() || !TryGetChiselHit(out var blockPosition, out var localPoint, addVoxel: true))
        {
            return false;
        }

        if (!server.HasChiseledBlockAt(blockPosition))
        {
            return false;
        }

        if (!server.TryChiselAddVoxel(blockPosition, localPoint))
        {
            return false;
        }

        onWorldChanged?.Invoke();
        return true;
    }

    public bool TryChiselSecondaryAction()
    {
        if (!IsChiselSelected() || !BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        var blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        if (server.HasChiseledBlockAt(blockPosition))
        {
            return TryChiselAddVoxel();
        }

        if (!server.TryBeginChiselBlock(blockPosition))
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

    private bool IsChiselSelected()
    {
        return creativeInventory != null
               && creativeInventory.TryGetSelectedItem(out var item)
               && item.IsChisel;
    }

    private static bool IsShiftHeld()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }

    private bool TryGetChiselHit(out Vector3Int blockPosition, out Vector3 localPoint, bool addVoxel)
    {
        blockPosition = default;
        localPoint = default;

        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        var offset = addVoxel ? 0.001f : -0.001f;
        var point = hit.point + hit.normal * offset;
        localPoint = BlockWorldTargeting.WorldToLocalBlockPoint(point, blockPosition);
        return true;
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
