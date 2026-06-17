using UnityEngine;
using UnityEngine.UI;

public sealed class CreativeInventoryDragPresenter
{
    private readonly float slotSize;
    private BlockItemSlotPreview dragPreview;
    private bool dragActive;
    private HotbarItem dragItem;
    private int dragSourceHotbarIndex = -1;

    public bool IsDragActive => dragActive;
    public BlockItemSlotPreview DragPreview => dragPreview;

    public CreativeInventoryDragPresenter(float slotSize)
    {
        this.slotSize = slotSize;
    }

    public void Build(Transform parent)
    {
        var ghostObject = new GameObject("DragGhost");
        ghostObject.transform.SetParent(parent, false);

        var rect = ghostObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        ghostObject.AddComponent<RawImage>();
        dragPreview = ghostObject.AddComponent<BlockItemSlotPreview>();
        ghostObject.SetActive(false);
    }

    public void ShowGhost(HotbarItem item)
    {
        if (dragPreview == null)
        {
            return;
        }

        dragPreview.SetHotbarItem(item, spin: false);
        dragPreview.gameObject.SetActive(true);
        dragPreview.transform.SetAsLastSibling();
    }

    public void MoveGhost(Vector2 screenPosition)
    {
        if (dragPreview == null || !dragPreview.gameObject.activeSelf)
        {
            return;
        }

        var parent = dragPreview.transform.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out var localPoint);
        dragPreview.transform.localPosition = localPoint;
    }

    public void HideGhost()
    {
        if (dragPreview != null)
        {
            dragPreview.SetHotbarItem(default);
            dragPreview.gameObject.SetActive(false);
        }
    }

    public void BeginDrag(HotbarItem item, int sourceHotbarIndex)
    {
        dragActive = true;
        dragItem = item;
        dragSourceHotbarIndex = sourceHotbarIndex;
    }

    public void EndDrag()
    {
        dragActive = false;
        dragSourceHotbarIndex = -1;
    }

    public bool TryGetDragItem(out HotbarItem item, out int sourceHotbarIndex)
    {
        item = dragItem;
        sourceHotbarIndex = dragSourceHotbarIndex;
        return dragActive;
    }
}
