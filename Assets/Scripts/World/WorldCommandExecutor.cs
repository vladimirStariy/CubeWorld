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
        var server = authority as BlockWorldServer;

        switch (command.Kind)
        {
            case WorldCommandKind.PlaceBlock:
                return TryPlaceBlock(command, server, inventory);
            case WorldCommandKind.BreakBlock:
                return TryBreakBlock(command, server);
            case WorldCommandKind.PlaceGroundItem:
                return TryPlaceGroundItem(command, server, inventory);
            case WorldCommandKind.PickupGroundItem:
                return TryPickupGroundItem(command, server, inventory);
            case WorldCommandKind.UseItemOnAssembly:
                return TryUseItemOnAssembly(command, server, inventory);
            case WorldCommandKind.BreakCampfireAssembly:
                return TryBreakCampfireAssembly(command, server);
            case WorldCommandKind.PlaceClayWorksite:
                return TryPlaceClayWorksite(command, server, inventory);
            case WorldCommandKind.ChiselBegin:
                return TryChiselBegin(command, server);
            case WorldCommandKind.ChiselRemove:
                return TryChiselRemove(command, server);
            case WorldCommandKind.ChiselAdd:
                return TryChiselAdd(command, server);
            default:
                return WorldCommandResult.Fail($"Unsupported command: {command.Kind}");
        }
    }

    private static WorldCommandResult TryPlaceBlock(WorldCommand command, BlockWorldServer server, PlayerInventoryState inventory)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!inventory.TryGetSelectedItem(out var item) || !item.IsPlaceableBlock)
        {
            return WorldCommandResult.Fail("No placeable block selected.");
        }

        if (!server.TrySetBlock(command.TargetBlockPosition, item.BlockType))
        {
            return WorldCommandResult.Fail("Cannot place block here.");
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryBreakBlock(WorldCommand command, BlockWorldServer server)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!server.TrySetBlock(command.BlockPosition, VoxelBlockType.Air))
        {
            return WorldCommandResult.Fail("Cannot break block.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryPlaceGroundItem(WorldCommand command, BlockWorldServer server, PlayerInventoryState inventory)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!inventory.TryGetSelectedItem(out var item) || !item.IsGroundPlaceable)
        {
            return WorldCommandResult.Fail("No ground-placeable item selected.");
        }

        if (!server.TryPlaceGroundItem(
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

    private static WorldCommandResult TryPickupGroundItem(WorldCommand command, BlockWorldServer server, PlayerInventoryState inventory)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        var pickupAmount = command.PickupAmount;
        if (pickupAmount <= 0)
        {
            return WorldCommandResult.Fail("Nothing to pick up.");
        }

        if (!server.TryProbeGroundPickup(
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

        if (!server.TryPickupGroundItem(
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

    private static WorldCommandResult TryUseItemOnAssembly(WorldCommand command, BlockWorldServer server, PlayerInventoryState inventory)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!inventory.TryGetSelectedItem(out var item) || !item.IsAssemblyComponent)
        {
            return WorldCommandResult.Fail("Invalid assembly item.");
        }

        if (!server.TryUseItemOnTarget(command.BlockPosition, command.FaceNormal, item, out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryBreakCampfireAssembly(WorldCommand command, BlockWorldServer server)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!server.TryBreakCampfireAssembly(command.BlockPosition, command.FaceNormal, out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        return WorldCommandResult.Ok(message);
    }

    private static WorldCommandResult TryPlaceClayWorksite(WorldCommand command, BlockWorldServer server, PlayerInventoryState inventory)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!inventory.TryGetSelectedItem(out var item) || !item.IsClay)
        {
            return WorldCommandResult.Fail("Need clay in hand.");
        }

        if (!server.TryPlaceClayWorksite(
                command.BlockPosition,
                Vector3Int.RoundToInt(command.FaceNormal),
                out var key,
                out var message))
        {
            return WorldCommandResult.Fail(message);
        }

        inventory.TryConsumeFromSelected(1, out _);
        return WorldCommandResult.ClayWorksitePlaced(key, message);
    }

    private static WorldCommandResult TryChiselBegin(WorldCommand command, BlockWorldServer server)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!server.TryBeginChiselBlock(command.BlockPosition))
        {
            return WorldCommandResult.Fail("Cannot chisel this block.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryChiselRemove(WorldCommand command, BlockWorldServer server)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!server.HasChiseledBlockAt(command.BlockPosition))
        {
            return WorldCommandResult.Fail("No chiseled block here.");
        }

        if (!server.TryChiselRemoveVoxel(command.BlockPosition, command.ChiselLocalPoint))
        {
            return WorldCommandResult.Fail("Cannot remove voxel.");
        }

        return WorldCommandResult.Ok();
    }

    private static WorldCommandResult TryChiselAdd(WorldCommand command, BlockWorldServer server)
    {
        if (server == null)
        {
            return WorldCommandResult.Fail("World server is not ready.");
        }

        if (!server.HasChiseledBlockAt(command.BlockPosition))
        {
            return WorldCommandResult.Fail("No chiseled block here.");
        }

        if (!server.TryChiselAddVoxel(command.BlockPosition, command.ChiselLocalPoint))
        {
            return WorldCommandResult.Fail("Cannot add voxel.");
        }

        return WorldCommandResult.Ok();
    }
}
