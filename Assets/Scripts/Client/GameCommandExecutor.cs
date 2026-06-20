using System;
using System.Globalization;
using UnityEngine;

public static class GameCommandExecutor
{
    public sealed class Context
    {
        public FirstPersonCharacterController Player;
        public CreativeInventory Inventory;
        public BlockWorldServer World;
        public Action ClearLog;
    }

    public static bool TryExecute(string line, Context context, out string response)
    {
        response = null;
        if (context == null)
        {
            response = "Command context is not ready.";
            return true;
        }

        line = line.Trim();
        if (line.Length == 0)
        {
            return false;
        }

        if (line.StartsWith('/'))
        {
            line = line.Substring(1);
        }

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var command = parts[0].ToLowerInvariant();
        switch (command)
        {
            case "help":
            case "?":
                response = "Commands: help, pos, tp <x> <y> <z>, give <item> [count], clear";
                return true;

            case "pos":
                if (context.Player == null)
                {
                    response = "Player not found.";
                    return true;
                }

                var position = context.Player.transform.position;
                response = $"Position: {Mathf.FloorToInt(position.x)} {Mathf.FloorToInt(position.y)} {Mathf.FloorToInt(position.z)}";
                return true;

            case "tp":
            case "teleport":
                if (context.Player == null)
                {
                    response = "Player not found.";
                    return true;
                }

                if (parts.Length < 4
                    || !TryParseCoord(parts[1], out var x)
                    || !TryParseCoord(parts[2], out var y)
                    || !TryParseCoord(parts[3], out var z))
                {
                    response = "Usage: /tp <x> <y> <z>";
                    return true;
                }

                context.Player.Teleport(new Vector3(x, y, z));
                response = $"Teleported to {Mathf.FloorToInt(x)} {Mathf.FloorToInt(y)} {Mathf.FloorToInt(z)}";
                return true;

            case "give":
                if (context.Inventory == null)
                {
                    response = "Inventory not found.";
                    return true;
                }

                if (parts.Length < 2)
                {
                    response = "Usage: /give <dirt|grass|dirt_slab|grass_bundle|stick|flint|chisel|clay|clay_bowl|raw_clay_bowl> [count]";
                    return true;
                }

                if (!TryParseHotbarItem(parts[1], out var item))
                {
                    response = $"Unknown item: {parts[1]}";
                    return true;
                }

                var count = 1;
                if (parts.Length >= 3)
                {
                    if (!TryParseCount(parts[2], out count))
                    {
                        response = "Invalid count. Usage: /give <item> [count]";
                        return true;
                    }

                    count = Mathf.Clamp(count, 1, 9999);
                }

                var remainder = GiveItems(context.Inventory, item, count);
                if (remainder >= count)
                {
                    response = "Inventory is full.";
                    return true;
                }

                if (remainder > 0)
                {
                    response = $"Added {count - remainder} of {item.WithCount(1).GetDisplayName()}. Could not add {remainder}.";
                    return true;
                }

                response = count > 1
                    ? $"Added {count} {item.WithCount(1).GetDisplayName()} to inventory."
                    : $"Added {item.GetDisplayName()} to inventory.";
                return true;

            case "clear":
                context.ClearLog?.Invoke();
                response = "Log cleared.";
                return true;

            default:
                response = $"Unknown command: {command}. Type /help";
                return true;
        }
    }

    private static bool TryParseCoord(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseCount(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    private static int GiveItems(CreativeInventory inventory, HotbarItem item, int count)
    {
        if (item.IsStackable)
        {
            return inventory.TryAddItem(item.WithCount(count));
        }

        var remainder = count;
        while (remainder > 0)
        {
            var before = remainder;
            remainder = inventory.TryAddItem(item.WithCount(remainder));
            if (remainder == before)
            {
                break;
            }
        }

        return remainder;
    }

    private static bool TryParseHotbarItem(string text, out HotbarItem item)
    {
        if (string.Equals(text, "dirt", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.FromBlock(VoxelBlockType.Dirt);
            return true;
        }

        if (string.Equals(text, "grass", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "grass_block", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.FromBlock(VoxelBlockType.GrassBlock);
            return true;
        }

        if (string.Equals(text, "dirt_slab", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "slab", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.FromBlock(VoxelBlockType.DirtSlab);
            return true;
        }

        if (string.Equals(text, "grass_bundle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "bundle", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.GrassBundle();
            return true;
        }

        if (string.Equals(text, "stick", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "sticks", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.Stick();
            return true;
        }

        if (string.Equals(text, "flint", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "flint_and_steel", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.Flint();
            return true;
        }

        if (string.Equals(text, "chisel", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.Chisel();
            return true;
        }

        if (string.Equals(text, "clay", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.Clay();
            return true;
        }

        if (string.Equals(text, "raw_clay_bowl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "raw_bowl", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.RawClayBowl();
            return true;
        }

        if (string.Equals(text, "clay_bowl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "bowl", StringComparison.OrdinalIgnoreCase))
        {
            item = HotbarItem.ClayBowl();
            return true;
        }

        item = default;
        return false;
    }
}
