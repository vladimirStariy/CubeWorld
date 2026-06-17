using System;
using UnityEngine;

public sealed class CreativeInventory : MonoBehaviour
{
    public const int HotbarSize = 9;

    private static readonly CreativeEntry[] CreativeEntries =
    {
        new(HotbarItem.FromBlock(VoxelBlockType.Dirt)),
        new(HotbarItem.FromBlock(VoxelBlockType.GrassBlock)),
        new(HotbarItem.FromBlock(VoxelBlockType.DirtSlab)),
        new(HotbarItem.GrassBundle()),
        new(HotbarItem.Stick()),
        new(HotbarItem.Flint())
    };

    private readonly HotbarItem[] hotbar = new HotbarItem[HotbarSize];
    private int selectedSlot;

    public event Action Changed;

    public int SelectedSlot => selectedSlot;

    public static CreativeEntry[] GetCreativeEntries()
    {
        return CreativeEntries;
    }

    private void Awake()
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

        hotbar[index] = item;
        selectedSlot = index;
        Changed?.Invoke();
    }

    public void AssignToSelectedSlot(HotbarItem item)
    {
        AssignToSlot(selectedSlot, item);
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
}
