using System;
using UnityEngine;

public sealed class PlayerInventoryState
{
    public const int HotbarSize = 9;

    private readonly HotbarItem[] hotbar = new HotbarItem[HotbarSize];
    private int selectedSlot;

    public event Action Changed;

    public int SelectedSlot => selectedSlot;

    public PlayerInventoryState()
    {
        hotbar[0] = HotbarItem.FromBlock(VoxelBlockType.Dirt);
    }

    public HotbarItem GetHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarSize)
        {
            return default;
        }

        return hotbar[index];
    }

    public bool TryGetSelectedItem(out HotbarItem item)
    {
        item = GetHotbarSlot(selectedSlot);
        return !item.IsEmpty;
    }

    public bool TryGetSelectedPlaceableBlock(out VoxelBlockType blockType)
    {
        blockType = VoxelBlockType.Air;
        if (!TryGetSelectedItem(out var item) || !item.IsPlaceableBlock)
        {
            return false;
        }

        blockType = item.BlockType;
        return true;
    }

    public void SelectSlot(int index)
    {
        var clamped = Mathf.Clamp(index, 0, HotbarSize - 1);
        if (selectedSlot == clamped)
        {
            return;
        }

        selectedSlot = clamped;
        Changed?.Invoke();
    }

    public void AssignToSlot(int index, HotbarItem item)
    {
        if (index < 0 || index >= HotbarSize)
        {
            return;
        }

        hotbar[index] = item.IsEmpty ? default : item.WithCount(Mathf.Min(item.Count, item.MaxStack));
        selectedSlot = index;
        Changed?.Invoke();
    }

    public void AssignToSelectedSlot(HotbarItem item)
    {
        AssignToSlot(selectedSlot, item);
    }

    public bool TryConsumeFromSelected(int amount, out HotbarItem consumed)
    {
        consumed = default;
        if (amount <= 0 || !TryGetSelectedItem(out var item) || item.Count < amount)
        {
            return false;
        }

        consumed = item.WithCount(amount);
        if (item.Count == amount)
        {
            hotbar[selectedSlot] = default;
        }
        else
        {
            hotbar[selectedSlot] = item.WithCount(item.Count - amount);
        }

        Changed?.Invoke();
        return true;
    }

    public int GetAddableAmount(HotbarItem item)
    {
        if (item.IsEmpty)
        {
            return 0;
        }

        if (!item.IsStackable)
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                if (hotbar[i].IsEmpty)
                {
                    return Mathf.Min(1, item.Count);
                }
            }

            return 0;
        }

        var space = 0;
        for (int i = 0; i < HotbarSize; i++)
        {
            var slot = hotbar[i];
            if (slot.CanStackWith(item))
            {
                space += slot.MaxStack - slot.Count;
            }
        }

        for (int i = 0; i < HotbarSize; i++)
        {
            if (hotbar[i].IsEmpty)
            {
                space += item.MaxStack;
            }
        }

        return Mathf.Min(item.Count, space);
    }

    public int TryAddItem(HotbarItem item)
    {
        if (item.IsEmpty)
        {
            return 0;
        }

        if (!item.IsStackable)
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                if (!hotbar[i].IsEmpty)
                {
                    continue;
                }

                hotbar[i] = item.WithCount(1);
                Changed?.Invoke();
                return item.Count > 1 ? item.Count - 1 : 0;
            }

            return item.Count;
        }

        var remaining = item.Count;
        remaining = TryMergeIntoSlot(selectedSlot, item, remaining);

        for (int i = 0; i < HotbarSize && remaining > 0; i++)
        {
            if (i == selectedSlot)
            {
                continue;
            }

            remaining = TryMergeIntoSlot(i, item, remaining);
        }

        remaining = TryFillEmptySlot(selectedSlot, item, remaining);
        for (int i = 0; i < HotbarSize && remaining > 0; i++)
        {
            if (i == selectedSlot)
            {
                continue;
            }

            remaining = TryFillEmptySlot(i, item, remaining);
        }

        if (remaining != item.Count)
        {
            Changed?.Invoke();
        }

        return remaining;
    }

    public bool TryAddToSelected(HotbarItem item)
    {
        return TryAddItem(item) == 0;
    }

    public void SwapHotbarSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= HotbarSize || toIndex < 0 || toIndex >= HotbarSize)
        {
            return;
        }

        if (fromIndex == toIndex)
        {
            selectedSlot = toIndex;
            Changed?.Invoke();
            return;
        }

        (hotbar[fromIndex], hotbar[toIndex]) = (hotbar[toIndex], hotbar[fromIndex]);
        selectedSlot = toIndex;
        Changed?.Invoke();
    }

    public void ScrollSelection(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        var next = (selectedSlot + delta) % HotbarSize;
        if (next < 0)
        {
            next += HotbarSize;
        }

        SelectSlot(next);
    }

    private int TryMergeIntoSlot(int index, HotbarItem item, int remaining)
    {
        var slot = hotbar[index];
        if (!slot.CanStackWith(item))
        {
            return remaining;
        }

        var canAdd = Mathf.Min(remaining, slot.MaxStack - slot.Count);
        if (canAdd <= 0)
        {
            return remaining;
        }

        hotbar[index] = slot.WithCount(slot.Count + canAdd);
        return remaining - canAdd;
    }

    private int TryFillEmptySlot(int index, HotbarItem item, int remaining)
    {
        if (remaining <= 0 || !hotbar[index].IsEmpty)
        {
            return remaining;
        }

        var add = Mathf.Min(remaining, item.MaxStack);
        hotbar[index] = item.WithCount(add);
        return remaining - add;
    }
}
