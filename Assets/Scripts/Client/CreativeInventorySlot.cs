using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CreativeInventorySlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler
{
    private ICreativeInventorySlotHost slotHost;
    [SerializeField] private CreativeInventory inventory;
    [SerializeField] private bool isCreativeSource;
    [SerializeField] private int hotbarIndex = -1;
    [SerializeField] private HotbarItem creativeItem;

    public void Configure(
        ICreativeInventorySlotHost host,
        CreativeInventory inventoryState,
        bool creativeSource,
        int slotIndex,
        HotbarItem item)
    {
        slotHost = host;
        inventory = inventoryState;
        isCreativeSource = creativeSource;
        hotbarIndex = slotIndex;
        creativeItem = item;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotHost.IsDragActive)
        {
            return;
        }

        if (isCreativeSource)
        {
            var hotbarSlot = hotbarIndex >= 0
                ? hotbarIndex % CreativeInventory.HotbarSize
                : inventory.SelectedSlot;
            if (inventory.TryAddItem(creativeItem) > 0)
            {
                inventory.AssignToSlot(hotbarSlot, creativeItem);
            }

            return;
        }

        if (hotbarIndex >= 0)
        {
            inventory.SelectSlot(hotbarIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryGetDragItem(out var item))
        {
            return;
        }

        var sourceIndex = isCreativeSource ? -1 : hotbarIndex;
        slotHost.BeginDrag(item, sourceIndex);
        slotHost.ShowDragGhost(item);
        slotHost.MoveDragGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!slotHost.IsDragActive)
        {
            return;
        }

        slotHost.MoveDragGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!slotHost.IsDragActive)
        {
            return;
        }

        slotHost.HideDragGhost();
        slotHost.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!slotHost.TryGetDragItem(out var item, out var sourceHotbarIndex) || hotbarIndex < 0)
        {
            return;
        }

        if (sourceHotbarIndex < 0)
        {
            inventory.AssignToSlot(hotbarIndex, item);
            return;
        }

        inventory.SwapHotbarSlots(sourceHotbarIndex, hotbarIndex);
    }

    private bool TryGetDragItem(out HotbarItem item)
    {
        if (isCreativeSource)
        {
            item = creativeItem;
            return !item.IsEmpty;
        }

        if (hotbarIndex < 0)
        {
            item = default;
            return false;
        }

        item = inventory.GetHotbarSlot(hotbarIndex);
        return !item.IsEmpty;
    }
}
