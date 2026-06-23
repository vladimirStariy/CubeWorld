using UnityEngine;

public sealed class BlockWorldInteractor
{
    private readonly IGameServerConnection connection;
    private readonly IWorldAuthority authority;
    private readonly Camera playerCamera;
    private readonly CharacterController playerCharacterController;
    private readonly float interactDistance;
    private readonly System.Action onWorldChanged;
    private readonly BlockEntityUiController blockEntityUi;

    public BlockWorldInteractor(
        IGameServerConnection serverConnection,
        Camera playerCamera,
        CharacterController playerCharacterController,
        float interactDistance,
        System.Action onWorldChanged,
        BlockEntityUiController blockEntityUi)
    {
        connection = serverConnection;
        authority = serverConnection.Authority;
        this.playerCamera = playerCamera;
        this.playerCharacterController = playerCharacterController;
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

        if (!BlockWorldTargeting.TryRaycastInteractTarget(playerCamera, interactDistance, out var target))
        {
            return false;
        }

        var hitBlock = target.BlockPosition;
        if (!target.IsGroundItem && TryOpenBlockEntityUi(hitBlock))
        {
            return true;
        }

        if (!TryGetSelectedItem(out var item))
        {
            return false;
        }

        if (item.HasCapability(ItemCapabilities.CampfireAssemblyStick)
            && item.HasCapability(ItemCapabilities.GroundPlaceable)
            && !authority.TryHasCampfireAssembly(hitBlock, target.FaceNormal))
        {
            if (!InputModifiers.IsShiftHeld())
            {
                return false;
            }

            return ExecuteCommand(WorldCommand.PlaceGroundItem(hitBlock, target.FaceNormal, target.Point));
        }

        if (item.IsAssemblyComponent)
        {
            if (target.IsGroundItem)
            {
                return false;
            }

            if (!InputModifiers.IsShiftHeld())
            {
                return false;
            }

            return ExecuteCommand(WorldCommand.UseItemOnAssembly(hitBlock, target.FaceNormal));
        }

        if (item.IsGroundPlaceable)
        {
            if (!InputModifiers.IsShiftHeld())
            {
                return false;
            }

            if (ExecuteCommand(WorldCommand.PlaceGroundItem(hitBlock, target.FaceNormal, target.Point)))
            {
                return true;
            }
        }

        if (!item.IsPlaceableBlock)
        {
            return false;
        }

        var targetPosition = BlockWorldTargeting.GetAdjacentBlockPosition(target);
        if (WouldIntersectPlayer(targetPosition, item.BlockType))
        {
            return false;
        }

        return ExecuteCommand(WorldCommand.PlaceBlock(targetPosition));
    }

    public bool TryUseOrBreakBlock()
    {
        if (!BlockWorldTargeting.TryRaycastInteractTarget(playerCamera, interactDistance, out var target))
        {
            return false;
        }

        if (target.IsGroundItem)
        {
            return TryPickupGroundItemTarget(target);
        }

        var targetPosition = target.BlockPosition;

        if (IsChiselSelected() && authority.HasChiseledBlockAt(targetPosition))
        {
            return TryChiselRemoveVoxel();
        }

        if (TryExecuteCommand(WorldCommand.BreakCampfireAssembly(targetPosition, target.FaceNormal), logFailure: false))
        {
            return true;
        }

        if (TryPickupGroundItemTarget(target))
        {
            return true;
        }

        return ExecuteCommand(WorldCommand.BreakBlock(targetPosition));
    }

    private bool TryPickupGroundItemTarget(WorldInteractTarget target)
    {
        var pickupAmount = authority.ResolveGroundPickupAmount(
            target.BlockPosition,
            target.FaceNormal,
            target.Point,
            InputModifiers.IsShiftHeld());

        if (pickupAmount <= 0)
        {
            return false;
        }

        return ExecuteCommand(WorldCommand.PickupGroundItem(
            target.BlockPosition,
            target.FaceNormal,
            target.Point,
            pickupAmount));
    }

    public bool TryChiselRemoveVoxel()
    {
        if (!IsChiselSelected() || !TryGetChiselHit(out var blockPosition, out var localPoint, addVoxel: false))
        {
            return false;
        }

        return ExecuteCommand(WorldCommand.ChiselRemove(blockPosition, localPoint));
    }

    public bool TryChiselAddVoxel()
    {
        if (!IsChiselSelected() || !TryGetChiselHit(out var blockPosition, out var localPoint, addVoxel: true))
        {
            return false;
        }

        return ExecuteCommand(WorldCommand.ChiselAdd(blockPosition, localPoint));
    }

    public bool TryChiselSecondaryAction()
    {
        if (!IsChiselSelected() || !BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            return false;
        }

        var blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        if (authority.HasChiseledBlockAt(blockPosition))
        {
            return TryChiselAddVoxel();
        }

        return ExecuteCommand(WorldCommand.ChiselBegin(blockPosition));
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
        return authority.TryQueryBlock(blockPosition, out blockInfo);
    }

    private bool ExecuteCommand(WorldCommand command)
    {
        return TryExecuteCommand(command, logFailure: true);
    }

    private bool TryExecuteCommand(WorldCommand command, bool logFailure)
    {
        var result = connection.ExecuteCommand(command);
        if (!result.Success)
        {
            if (logFailure && !string.IsNullOrEmpty(result.Message))
            {
                Debug.Log(result.Message);
            }

            return false;
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            Debug.Log(result.Message);
        }

        onWorldChanged?.Invoke();
        return true;
    }

    private bool TryGetSelectedItem(out HotbarItem item)
    {
        item = default;
        return authority.PlayerInventory != null && authority.PlayerInventory.TryGetSelectedItem(out item);
    }

    private bool IsChiselSelected()
    {
        return TryGetSelectedItem(out var item) && item.IsChisel;
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
