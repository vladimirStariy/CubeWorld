using UnityEngine;

public static class WorldCommandExecutor
{
    public static WorldCommandResult Execute(WorldCommand command, IWorldAuthority authority)
    {
        if (authority == null)
        {
            return WorldCommandResult.Fail("World authority is not ready.");
        }

        var inventory = authority.PlayerInventory;

        switch (command.Kind)
        {
            case WorldCommandKind.PlaceBlock:
                return TryPlaceBlock(command, authority, inventory);
            case WorldCommandKind.BreakBlock:
                return TryBreakBlock(command, authority);
            case WorldCommandKind.PlaceGroundItem:
                return TryPlaceGroundItem(command, authority, inventory);
            case WorldCommandKind.PickupGroundItem:
                return TryPickupGroundItem(command, authority, inventory);
            case WorldCommandKind.UseItemOnAssembly:
                return TryUseItemOnAssembly(command, authority, inventory);
            case WorldCommandKind.BreakCampfireAssembly:
                return TryBreakCampfireAssembly(command, authority);
            case WorldCommandKind.PlaceClayWorksite:
                return TryPlaceClayWorksite(command, authority, inventory);
            case WorldCommandKind.StartClayForming:
                return TryStartClayForming(command, authority);
            case WorldCommandKind.RemoveClayWorksite:
                return TryRemoveClayWorksite(command, authority);
            case WorldCommandKind.ClayFormingAdd:
                return TryClayFormingAdd(command, authority);
            case WorldCommandKind.ClayFormingRemove:
                return TryClayFormingRemove(command, authority);
            case WorldCommandKind.SetClayFormingToolMode:
                return TrySetClayFormingToolMode(command, authority);
            case WorldCommandKind.ChiselBegin:
                return TryChiselBegin(command, authority);
            case WorldCommandKind.ChiselRemove:
                return TryChiselRemove(command, authority);
            case WorldCommandKind.ChiselAdd:
                return TryChiselAdd(command, authority);
            default:
                return WorldCommandResult.Fail($"Unsupported command: {command.Kind}");
        }
    }

    private static WorldCommandResult TryPlaceBlock(WorldCommand command, IWorldAuthority authority, PlayerInventoryState inventory)
    {
        if (!inventory.TryGetSelectedItem(out var item) || !item.IsPlaceableBlock)
        {
            return WorldCommandResult.Fail("No placeable block selected.");
        }

        if (!authority.TrySetBlock(command.TargetBlockPosition, item.BlockType))
        {
            return WorldCommandResult.Fail("Cannot place block here.");
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryBreakBlock(WorldCommand command, IWorldAuthority authority)
    {
        if (!authority.TrySetBlock(command.BlockPosition, VoxelBlockType.Air))
        {
            return WorldCommandResult.Fail("Cannot break block.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryPlaceGroundItem(WorldCommand command, IWorldAuthority authority, PlayerInventoryState inventory)
    {
        if (!inventory.TryGetSelectedItem(out var item) || !item.IsGroundPlaceable)
        {
            return WorldCommandResult.Fail("No ground-placeable item selected.");
        }

        if (!authority.TryPlaceGroundItem(
                command.BlockPosition,
                command.FaceNormal,
                command.WorldHitPoint,
                item,
                out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryPickupGroundItem(WorldCommand command, IWorldAuthority authority, PlayerInventoryState inventory)
    {
        var pickupAmount = command.PickupAmount;
        if (pickupAmount <= 0)
        {
            return WorldCommandResult.Fail("Nothing to pick up.");
        }

        if (!authority.TryProbeGroundPickup(
                command.BlockPosition,
                command.FaceNormal,
                command.WorldHitPoint,
                pickupAmount,
                out var probeItem))
        {
            return WorldCommandResult.Fail("Nothing to pick up.");
        }

        var canAdd = inventory.GetAddableAmount(probeItem);
        if (canAdd <= 0)
        {
            return WorldCommandResult.Fail("Inventory is full.");
        }

        pickupAmount = Mathf.Min(pickupAmount, canAdd);

        if (!authority.TryPickupGroundItem(
                command.BlockPosition,
                command.FaceNormal,
                command.WorldHitPoint,
                pickupAmount,
                out var pickedItem,
                out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryAddItem(pickedItem);
        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryUseItemOnAssembly(WorldCommand command, IWorldAuthority authority, PlayerInventoryState inventory)
    {
        if (!inventory.TryGetSelectedItem(out var item) || !item.IsAssemblyComponent)
        {
            return WorldCommandResult.Fail("Invalid assembly item.");
        }

        if (!authority.TryUseItemOnTarget(command.BlockPosition, command.FaceNormal, item, out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryBreakCampfireAssembly(WorldCommand command, IWorldAuthority authority)
    {
        if (!authority.TryBreakCampfireAssembly(command.BlockPosition, command.FaceNormal, out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryPlaceClayWorksite(WorldCommand command, IWorldAuthority authority, PlayerInventoryState inventory)
    {
        if (!inventory.TryGetSelectedItem(out var item) || !item.IsClay)
        {
            return WorldCommandResult.Fail("Need clay in hand.");
        }

        if (!authority.TryPlaceClayWorksite(
                command.BlockPosition,
                Vector3Int.RoundToInt(command.FaceNormal),
                out var key,
                out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryConsumeFromSelected(1, out _);
        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.ClayWorksitePlaced(key, message);
    }

    private static WorldCommandResult TryStartClayForming(WorldCommand command, IWorldAuthority authority)
    {
        var key = command.GetClayWorksiteKey();
        if (!authority.TryStartClayForming(key, command.RecipeId, out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryRemoveClayWorksite(WorldCommand command, IWorldAuthority authority)
    {
        var key = command.GetClayWorksiteKey();
        authority.RemoveClayWorksite(key);
        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryClayFormingAdd(WorldCommand command, IWorldAuthority authority)
    {
        var key = command.GetClayWorksiteKey();
        var edit = authority.TryClayFormingAdd(key, command.ClayCellU, command.ClayCellV);
        if (!edit.Changed)
        {
            return WorldCommandResult.Fail("No clay change.");
        }

        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.FromClayEdit(edit);
    }

    private static WorldCommandResult TryClayFormingRemove(WorldCommand command, IWorldAuthority authority)
    {
        var key = command.GetClayWorksiteKey();
        var edit = authority.TryClayFormingRemove(key, command.ClayCellU, command.ClayCellV);
        if (!edit.Changed)
        {
            return WorldCommandResult.Fail("No clay change.");
        }

        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.FromClayEdit(edit);
    }

    private static WorldCommandResult TrySetClayFormingToolMode(WorldCommand command, IWorldAuthority authority)
    {
        var key = command.GetClayWorksiteKey();
        authority.SetClayFormingToolMode(key, (ClayFormingToolMode)command.ClayToolModeValue);
        ClayFormingEvents.RaiseWorksiteChanged(key);
        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryChiselBegin(WorldCommand command, IWorldAuthority authority)
    {
        if (!authority.TryBeginChiselBlock(command.BlockPosition))
        {
            return WorldCommandResult.Fail("Cannot chisel this block.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryChiselRemove(WorldCommand command, IWorldAuthority authority)
    {
        if (!authority.HasChiseledBlockAt(command.BlockPosition))
        {
            return WorldCommandResult.Fail("No chiseled block here.");
        }

        if (!authority.TryChiselRemoveVoxel(command.BlockPosition, command.ChiselLocalPoint))
        {
            return WorldCommandResult.Fail("Cannot remove voxel.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryChiselAdd(WorldCommand command, IWorldAuthority authority)
    {
        if (!authority.HasChiseledBlockAt(command.BlockPosition))
        {
            return WorldCommandResult.Fail("No chiseled block here.");
        }

        if (!authority.TryChiselAddVoxel(command.BlockPosition, command.ChiselLocalPoint))
        {
            return WorldCommandResult.Fail("Cannot add voxel.");
        }

        return WorldCommandResult.Ok();
    }
}
