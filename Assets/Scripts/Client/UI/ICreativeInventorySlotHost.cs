using UnityEngine;

public interface ICreativeInventorySlotHost
{
    bool IsDragActive { get; }

    void BeginDrag(HotbarItem item, int sourceHotbarIndex);
    void EndDrag();
    bool TryGetDragItem(out HotbarItem item, out int sourceHotbarIndex);
    void ShowDragGhost(HotbarItem item);
    void MoveDragGhost(Vector2 screenPosition);
    void HideDragGhost();
}
