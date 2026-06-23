using System.Collections.Generic;
using UnityEngine;

public sealed class GroundItemSurface
{
    public GroundItemSurfaceKey Key { get; }
    public GroundPlacementLayout Layout { get; }
    public ItemKind ItemKind { get; private set; }
    public int[] SlotCounts { get; }

    public GroundItemSurface(GroundItemSurfaceKey key, GroundPlacementLayout layout, int slotCount)
    {
        Key = key;
        Layout = layout;
        SlotCounts = new int[slotCount];
    }

    public bool IsEmpty
    {
        get
        {
            for (int i = 0; i < SlotCounts.Length; i++)
            {
                if (SlotCounts[i] > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void SetItemKind(ItemKind kind)
    {
        ItemKind = kind;
    }

    public GroundItemSurfaceSnapshot ToSnapshot()
    {
        var slots = new GroundItemSlotSnapshot[SlotCounts.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new GroundItemSlotSnapshot
            {
                Kind = SlotCounts[i] > 0 ? ItemKind : ItemKind.None,
                Count = SlotCounts[i]
            };
        }

        return new GroundItemSurfaceSnapshot
        {
            Key = Key,
            Layout = Layout,
            Slots = slots
        };
    }
}

public sealed class GroundItemStorage
{
    private readonly Dictionary<GroundItemSurfaceKey, GroundItemSurface> surfaces = new();
    private readonly System.Func<Vector3Int, bool> isFoundationSolid;

    public GroundItemStorage(System.Func<Vector3Int, bool> isFoundationSolid)
    {
        this.isFoundationSolid = isFoundationSolid;
    }

    public bool TryPlaceItem(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        HotbarItem item,
        out string message)
    {
        message = null;
        if (item.IsEmpty || item.Count <= 0)
        {
            message = "Nothing to place.";
            return false;
        }

        if (!GroundItemPlacementProfiles.TryGet(item.Kind, out var profile))
        {
            message = "This item cannot be placed on blocks.";
            return false;
        }

        if (!GroundItemPlacementMath.TryCreateSurfaceKey(hitBlock, faceNormal, profile, out var key, out message))
        {
            return false;
        }

        if (!isFoundationSolid(key.FoundationBlock))
        {
            message = "Need a solid block.";
            return false;
        }

        var slotIndex = GroundItemPlacementMath.ResolveSlotIndex(
            worldHitPoint,
            key.FoundationBlock,
            key.FaceNormal,
            profile.Layout);

        return TryAddToSurface(key, profile, slotIndex, item.Kind, 1, out message);
    }

    public bool TryPlaceCompletedItem(GroundItemSurfaceKey key, HotbarItem item, out string message)
    {
        message = null;
        if (item.IsEmpty)
        {
            return false;
        }

        if (!GroundItemPlacementProfiles.TryGet(item.Kind, out var profile))
        {
            message = "Cannot place this item.";
            return false;
        }

        if (!isFoundationSolid(key.FoundationBlock))
        {
            message = "Need a solid block.";
            return false;
        }

        return TryAddToSurface(key, profile, -1, item.Kind, item.Count, out message);
    }

    public bool TryProbeGroundPickup(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem probeItem)
    {
        probeItem = default;

        if (requestedAmount <= 0)
        {
            return false;
        }

        if (!TryResolveSurfaceKey(hitBlock, faceNormal, out var key, out var surface))
        {
            return false;
        }

        if (!GroundItemPlacementProfiles.TryGet(surface.ItemKind, out _))
        {
            return false;
        }

        var slotIndex = GroundItemPlacementMath.ResolveSlotIndex(
            worldHitPoint,
            key.FoundationBlock,
            key.FaceNormal,
            surface.Layout);

        if (slotIndex < 0 || slotIndex >= surface.SlotCounts.Length || surface.SlotCounts[slotIndex] <= 0)
        {
            return false;
        }

        var pickupAmount = Mathf.Min(requestedAmount, surface.SlotCounts[slotIndex]);
        probeItem = CreateStack(surface.ItemKind, pickupAmount);
        return true;
    }

    public bool TryPickupItem(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        int requestedAmount,
        out HotbarItem pickedItem,
        out string message)
    {
        pickedItem = default;
        message = null;

        if (requestedAmount <= 0)
        {
            message = "Nothing to pick up.";
            return false;
        }

        if (!TryResolveSurfaceKey(hitBlock, faceNormal, out var key, out var surface))
        {
            message = "Nothing to pick up here.";
            return false;
        }

        if (!GroundItemPlacementProfiles.TryGet(surface.ItemKind, out var profile))
        {
            return false;
        }

        var slotIndex = GroundItemPlacementMath.ResolveSlotIndex(
            worldHitPoint,
            key.FoundationBlock,
            key.FaceNormal,
            surface.Layout);

        if (slotIndex < 0 || slotIndex >= surface.SlotCounts.Length || surface.SlotCounts[slotIndex] <= 0)
        {
            message = "Nothing to pick up here.";
            return false;
        }

        var pickupAmount = Mathf.Min(requestedAmount, surface.SlotCounts[slotIndex]);
        surface.SlotCounts[slotIndex] -= pickupAmount;
        pickedItem = CreateStack(surface.ItemKind, pickupAmount);

        if (surface.IsEmpty)
        {
            surfaces.Remove(key);
        }

        message = surface.ItemKind == ItemKind.Stick
            ? $"Picked up {pickupAmount} {(pickupAmount == 1 ? "stick" : "sticks")}."
            : $"Picked up {pickedItem.GetDisplayName()}.";
        return true;
    }

    public void RemoveSurfacesOnBlock(Vector3Int blockPosition)
    {
        var toRemove = new List<GroundItemSurfaceKey>();
        foreach (var pair in surfaces)
        {
            if (pair.Key.FoundationBlock == blockPosition)
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            surfaces.Remove(toRemove[i]);
        }
    }

    public int ResolvePickupAmount(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        bool shiftHeld)
    {
        if (!shiftHeld || !TryResolveSurfaceKey(hitBlock, faceNormal, out _, out var surface))
        {
            return 1;
        }

        if (!GroundItemPlacementProfiles.TryGet(surface.ItemKind, out var profile))
        {
            return 1;
        }

        return profile.ShiftPickupAmount;
    }

    public void CopySnapshots(List<GroundItemSurfaceSnapshot> buffer)
    {
        buffer.Clear();
        foreach (var pair in surfaces)
        {
            buffer.Add(pair.Value.ToSnapshot());
        }
    }

    public bool TryGetStickStackCount(Vector3Int hitBlock, Vector3 faceNormal, out int stickCount)
    {
        stickCount = 0;
        if (!TryResolveSurfaceKeyStrict(hitBlock, faceNormal, out _, out var surface))
        {
            return false;
        }

        if (surface.Layout != GroundPlacementLayout.Stack || surface.ItemKind != ItemKind.Stick)
        {
            return false;
        }

        stickCount = surface.SlotCounts[0];
        return stickCount > 0;
    }

    public bool TryBuildStickStackOutline(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        List<LineSegment> segments)
    {
        segments.Clear();
        if (!TryGetStickStackCount(hitBlock, faceNormal, out var stickCount))
        {
            return false;
        }

        return TryBuildStickStackOutlineAtBlock(
            hitBlock,
            Vector3Int.RoundToInt(faceNormal),
            stickCount,
            segments);
    }

    public bool TryGetStickStackCount(GroundItemSurfaceKey key, out int stickCount)
    {
        stickCount = 0;
        if (!surfaces.TryGetValue(key, out var surface))
        {
            return false;
        }

        if (surface.Layout != GroundPlacementLayout.Stack || surface.ItemKind != ItemKind.Stick)
        {
            return false;
        }

        stickCount = surface.SlotCounts[0];
        return stickCount > 0;
    }

    public bool TryBuildStickStackOutline(
        GroundItemSurfaceKey key,
        List<LineSegment> segments)
    {
        segments.Clear();
        if (!TryGetStickStackCount(key, out var stickCount))
        {
            return false;
        }

        return TryBuildStickStackOutlineAtBlock(
            key.FoundationBlock,
            key.FaceNormal,
            stickCount,
            segments);
    }

    private static bool TryBuildStickStackOutlineAtBlock(
        Vector3Int hitBlock,
        Vector3Int faceNormal,
        int stickCount,
        List<LineSegment> segments)
    {
        if (!GroundItemPlacementProfiles.TryGet(ItemKind.Stick, out var profile))
        {
            return false;
        }

        var localSegments = new List<LineSegment>();
        GroundStackMath.BuildOutlineSegments(stickCount, profile, faceNormal, localSegments);
        if (localSegments.Count == 0)
        {
            return false;
        }

        var blockOrigin = (Vector3)hitBlock;
        for (int i = 0; i < localSegments.Count; i++)
        {
            var segment = localSegments[i];
            segments.Add(new LineSegment(blockOrigin + segment.From, blockOrigin + segment.To));
        }

        return true;
    }

    private bool TryResolveSurfaceKeyStrict(
        Vector3Int clickedBlock,
        Vector3 faceNormal,
        out GroundItemSurfaceKey key,
        out GroundItemSurface surface)
    {
        key = default;
        surface = null;

        var normal = Vector3Int.RoundToInt(faceNormal);
        if (!surfaces.TryGetValue(new GroundItemSurfaceKey(clickedBlock, normal), out surface))
        {
            return false;
        }

        key = surface.Key;
        return true;
    }

    public bool TryResolveSurfaceKey(
        Vector3Int clickedBlock,
        Vector3 faceNormal,
        out GroundItemSurfaceKey key,
        out GroundItemSurface surface)
    {
        key = default;
        surface = null;

        if (surfaces.TryGetValue(new GroundItemSurfaceKey(clickedBlock, Vector3Int.RoundToInt(faceNormal)), out surface))
        {
            key = surface.Key;
            return true;
        }

        var normal = Vector3Int.RoundToInt(faceNormal);
        if (normal == Vector3Int.up && surfaces.TryGetValue(new GroundItemSurfaceKey(clickedBlock, Vector3Int.up), out surface))
        {
            key = surface.Key;
            return true;
        }

        if (normal == Vector3Int.down)
        {
            var above = clickedBlock + Vector3Int.up;
            if (surfaces.TryGetValue(new GroundItemSurfaceKey(above, Vector3Int.up), out surface))
            {
                key = surface.Key;
                return true;
            }
        }

        foreach (var pair in surfaces)
        {
            if (pair.Key.FoundationBlock == clickedBlock)
            {
                key = pair.Key;
                surface = pair.Value;
                return true;
            }
        }

        return false;
    }

    private bool TryAddToSurface(
        GroundItemSurfaceKey key,
        GroundItemPlacementProfile profile,
        int slotIndex,
        ItemKind itemKind,
        int amount,
        out string message)
    {
        message = null;
        if (amount <= 0)
        {
            return false;
        }

        if (!surfaces.TryGetValue(key, out var surface))
        {
            surface = new GroundItemSurface(key, profile.Layout, profile.SlotCount);
            surface.SetItemKind(itemKind);
            surfaces[key] = surface;
        }
        else if (surface.Layout != profile.Layout || surface.ItemKind != itemKind)
        {
            message = "This surface already holds a different item.";
            return false;
        }

        if (profile.Layout is GroundPlacementLayout.Pile or GroundPlacementLayout.Stack)
        {
            slotIndex = 0;
        }
        else if (slotIndex < 0 || slotIndex >= surface.SlotCounts.Length || surface.SlotCounts[slotIndex] >= profile.MaxStackPerSlot)
        {
            slotIndex = FindFirstAvailableSlot(surface, profile);
            if (slotIndex < 0)
            {
                message = "This surface is full.";
                return false;
            }
        }

        var addAmount = Mathf.Min(amount, profile.MaxStackPerSlot - surface.SlotCounts[slotIndex]);
        if (addAmount <= 0)
        {
            message = "This surface is full.";
            return false;
        }

        surface.SlotCounts[slotIndex] += addAmount;
        message = $"Placed {CreateStack(itemKind, addAmount).GetDisplayName()}.";
        return true;
    }

    private static int FindFirstAvailableSlot(GroundItemSurface surface, GroundItemPlacementProfile profile)
    {
        for (int i = 0; i < surface.SlotCounts.Length; i++)
        {
            if (surface.SlotCounts[i] < profile.MaxStackPerSlot)
            {
                return i;
            }
        }

        return -1;
    }

    private static HotbarItem CreateStack(ItemKind kind, int count)
    {
        if (ItemRegistry.Active != null)
        {
            return ItemRegistry.Active.CreateStack(kind, count);
        }

        return kind switch
        {
            ItemKind.Stick => HotbarItem.Stick(count),
            ItemKind.Flint => HotbarItem.Flint(count),
            ItemKind.Clay => HotbarItem.Clay(count),
            ItemKind.RawClayBowl => HotbarItem.RawClayBowl(count),
            ItemKind.ClayBowl => HotbarItem.ClayBowl(count),
            _ => new HotbarItem(kind, count: count)
        };
    }
}
