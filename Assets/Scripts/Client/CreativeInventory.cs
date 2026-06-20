using System;
using UnityEngine;

public sealed class CreativeInventory : MonoBehaviour
{
    public const int HotbarSize = PlayerInventoryState.HotbarSize;

    private PlayerInventoryState state;

    public event Action Changed;

    public PlayerInventoryState State => state;

    public int SelectedSlot => state?.SelectedSlot ?? 0;

    public static CreativeEntry[] GetCreativeEntries()
    {
        var registry = ItemRegistry.Active;
        if (registry == null)
        {
            return Array.Empty<CreativeEntry>();
        }

        var definitions = registry.CreativeEntries;
        var entries = new CreativeEntry[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            entries[i] = new CreativeEntry(definition.CreateStack(), definition.DisplayName);
        }

        return entries;
    }

    public void Bind(PlayerInventoryState inventoryState)
    {
        if (state != null)
        {
            state.Changed -= ForwardChanged;
        }

        state = inventoryState;
        if (state != null)
        {
            state.Changed += ForwardChanged;
            ForwardChanged();
        }
    }

    public HotbarItem GetHotbarSlot(int index)
    {
        return state?.GetHotbarSlot(index) ?? default;
    }

    public bool TryGetSelectedItem(out HotbarItem item)
    {
        if (state == null)
        {
            item = default;
            return false;
        }

        return state.TryGetSelectedItem(out item);
    }

    public bool TryGetSelectedPlaceableBlock(out VoxelBlockType blockType)
    {
        blockType = VoxelBlockType.Air;
        if (state == null)
        {
            return false;
        }

        return state.TryGetSelectedPlaceableBlock(out blockType);
    }

    public void SelectSlot(int index)
    {
        state?.SelectSlot(index);
    }

    public void AssignToSlot(int index, HotbarItem item)
    {
        state?.AssignToSlot(index, item);
    }

    public void AssignToSelectedSlot(HotbarItem item)
    {
        state?.AssignToSelectedSlot(item);
    }

    public bool TryConsumeFromSelected(int amount, out HotbarItem consumed)
    {
        consumed = default;
        if (state == null)
        {
            return false;
        }

        return state.TryConsumeFromSelected(amount, out consumed);
    }

    public int GetAddableAmount(HotbarItem item)
    {
        return state?.GetAddableAmount(item) ?? 0;
    }

    public int TryAddItem(HotbarItem item)
    {
        return state?.TryAddItem(item) ?? item.Count;
    }

    public bool TryAddToSelected(HotbarItem item)
    {
        return state != null && state.TryAddToSelected(item);
    }

    public void SwapHotbarSlots(int fromIndex, int toIndex)
    {
        state?.SwapHotbarSlots(fromIndex, toIndex);
    }

    public void ScrollSelection(int delta)
    {
        state?.ScrollSelection(delta);
    }

    private void OnDestroy()
    {
        if (state != null)
        {
            state.Changed -= ForwardChanged;
        }
    }

    private void ForwardChanged()
    {
        Changed?.Invoke();
    }
}
