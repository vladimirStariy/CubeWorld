using UnityEngine;
using UnityEngine.UI;

public sealed class CreativeInventoryUI : MonoBehaviour, ICreativeInventorySlotHost
{
    private const float SlotSize = 44f;
    private const float SlotSpacing = 4f;
    private const float HotbarSlotSpacing = 2f;
    private const float HotbarSlotBorder = 2f;
    private const float HotbarBottomPadding = 12f;
    private const int CreativeVisibleRows = 2;

    private CreativeInventory inventory;
    private FirstPersonCharacterController playerController;
    private GameCommandConsole commandConsole;
    private BlockInventoryPreviewRenderer previewRenderer;

    private readonly CreativeInventoryInputBindings input = new();
    private readonly CreativeInventoryDragPresenter drag = new(SlotSize);
    private CreativeHotbarView hotbarView;
    private CreativePanelView panelView;

    private bool isCreativePanelOpen;
    private bool configured;

    public bool IsCreativePanelOpen => isCreativePanelOpen;
    public bool IsDragActive => drag.IsDragActive;

    public void Configure(
        Canvas hudCanvas,
        CreativeInventory inventoryRef,
        FirstPersonCharacterController player,
        GameCommandConsole commandConsoleRef,
        Texture2D blockAtlas)
    {
        if (configured)
        {
            return;
        }

        inventory = inventoryRef;
        playerController = player;
        commandConsole = commandConsoleRef;

        input.Build();
        EnsurePreviewRenderer(blockAtlas);
        BuildUi(hudCanvas);
        RegisterAllPreviews();
        inventory.Changed += OnInventoryChanged;
        hotbarView.Refresh(inventory);
        SetCreativePanelOpen(false);
        input.Enable();
        configured = true;
    }

    private void OnDestroy()
    {
        if (!configured || inventory == null)
        {
            return;
        }

        inventory.Changed -= OnInventoryChanged;
    }

    private void OnEnable()
    {
        if (configured)
        {
            input.Enable();
        }
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        if (input.ToggleCreativeAction.WasPressedThisFrame())
        {
            if (!isCreativePanelOpen && commandConsole != null && commandConsole.IsOpen)
            {
                return;
            }

            SetCreativePanelOpen(!isCreativePanelOpen);
        }

        if (isCreativePanelOpen)
        {
            return;
        }

        var scroll = input.ScrollAction.ReadValue<Vector2>().y;
        if (scroll > 0.01f)
        {
            inventory.ScrollSelection(-1);
        }
        else if (scroll < -0.01f)
        {
            inventory.ScrollSelection(1);
        }

        for (int i = 0; i < CreativeInventory.HotbarSize; i++)
        {
            if (input.WasHotbarSlotPressed(i))
            {
                inventory.SelectSlot(i);
            }
        }
    }

    public void BeginDrag(HotbarItem item, int sourceHotbarIndex) => drag.BeginDrag(item, sourceHotbarIndex);
    public void EndDrag() => drag.EndDrag();
    public bool TryGetDragItem(out HotbarItem item, out int sourceHotbarIndex) => drag.TryGetDragItem(out item, out sourceHotbarIndex);
    public void ShowDragGhost(HotbarItem item) => drag.ShowGhost(item);
    public void MoveDragGhost(Vector2 screenPosition) => drag.MoveGhost(screenPosition);
    public void HideDragGhost() => drag.HideGhost();

    public void CloseCreativePanel()
    {
        if (isCreativePanelOpen)
        {
            SetCreativePanelOpen(false);
        }
    }

    private void OnInventoryChanged()
    {
        hotbarView.Refresh(inventory);
    }

    private void EnsurePreviewRenderer(Texture2D blockAtlas)
    {
        previewRenderer = GetComponent<BlockInventoryPreviewRenderer>();
        if (previewRenderer == null)
        {
            previewRenderer = gameObject.AddComponent<BlockInventoryPreviewRenderer>();
        }

        previewRenderer.Configure(blockAtlas);
    }

    private void RegisterAllPreviews()
    {
        if (previewRenderer == null)
        {
            return;
        }

        previewRenderer.RegisterAll(hotbarView.Previews);
        previewRenderer.RegisterAll(panelView.Previews);
        if (drag.DragPreview != null)
        {
            previewRenderer.Register(drag.DragPreview);
        }
    }

    private void BuildUi(Canvas hudCanvas)
    {
        var existingMarker = hudCanvas.GetComponentInChildren<CreativeInventoryUiMarker>(true);
        if (existingMarker != null)
        {
            Destroy(existingMarker.gameObject);
        }

        var root = new GameObject("CreativeInventoryUI");
        root.transform.SetParent(hudCanvas.transform, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<CreativeInventoryUiMarker>();

        hotbarView = new CreativeHotbarView(SlotSize, HotbarSlotSpacing, HotbarSlotBorder);
        hotbarView.Build(root.transform, HotbarBottomPadding, inventory, this);

        var hotbarGridWidth = CreativeInventory.HotbarSize * SlotSize
                              + (CreativeInventory.HotbarSize - 1) * HotbarSlotSpacing;
        var creativeViewportHeight = CreativeVisibleRows * SlotSize
                                     + (CreativeVisibleRows - 1) * SlotSpacing;
        panelView = new CreativePanelView(
            SlotSize,
            SlotSpacing,
            hotbarGridWidth,
            creativeViewportHeight,
            HotbarBottomPadding,
            SlotSize);
        panelView.Build(root.transform, inventory, this);

        drag.Build(root.transform);
    }

    private void SetCreativePanelOpen(bool open)
    {
        isCreativePanelOpen = open;
        panelView.SetVisible(open);
        playerController?.SetGameplayCaptured(!open);
    }
}

public sealed class CreativeInventoryUiMarker : MonoBehaviour
{
}
