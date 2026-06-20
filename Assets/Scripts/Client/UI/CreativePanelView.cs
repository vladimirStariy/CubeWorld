using UnityEngine;
using UnityEngine.UI;

public sealed class CreativePanelView
{
    private readonly float slotSize;
    private readonly float slotSpacing;
    private readonly float gridWidth;
    private readonly float viewportHeight;
    private readonly float hotbarBottomPadding;
    private readonly float hotbarSlotSize;

    public RectTransform Root { get; private set; }
    public BlockItemSlotPreview[] Previews { get; private set; }

    public CreativePanelView(
        float slotSize,
        float slotSpacing,
        float gridWidth,
        float viewportHeight,
        float hotbarBottomPadding,
        float hotbarSlotSize)
    {
        this.slotSize = slotSize;
        this.slotSpacing = slotSpacing;
        this.gridWidth = gridWidth;
        this.viewportHeight = viewportHeight;
        this.hotbarBottomPadding = hotbarBottomPadding;
        this.hotbarSlotSize = hotbarSlotSize;
    }

    public void Build(Transform parent, CreativeInventory inventory, ICreativeInventorySlotHost slotHost)
    {
        var columnsPerRow = CreativeInventory.HotbarSize;
        var panelWidth = gridWidth + 24f;
        var panelHeight = viewportHeight + 48f;

        Root = HudUiFactory.CreatePanel(
            parent,
            "CreativePanel",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, hotbarBottomPadding + hotbarSlotSize + 16f));
        Root.sizeDelta = new Vector2(panelWidth, panelHeight);

        var creativeBackground = Root.gameObject.AddComponent<Image>();
        creativeBackground.color = new Color(0f, 0f, 0f, 0.75f);

        HudUiFactory.CreateText(
            Root,
            "Creative",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            18,
            TextAnchor.UpperCenter);

        var scrollObject = new GameObject("CreativeScroll");
        scrollObject.transform.SetParent(Root, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -36f);

        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        var contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        var creativeEntries = CreativeInventory.GetCreativeEntries();
        var rowCount = Mathf.CeilToInt(creativeEntries.Length / (float)columnsPerRow);
        var contentHeight = rowCount * slotSize + Mathf.Max(0, rowCount - 1) * slotSpacing;
        contentRect.sizeDelta = new Vector2(gridWidth, contentHeight);

        Previews = new BlockItemSlotPreview[creativeEntries.Length];
        for (int i = 0; i < creativeEntries.Length; i++)
        {
            var entry = creativeEntries[i];
            var column = i % columnsPerRow;
            var row = i / columnsPerRow;
            var slot = CreateCreativeSlot(
                contentObject.transform,
                $"CreativeItem_{entry.Label}",
                column,
                row);
            Previews[i] = slot.preview;
            slot.preview.SetHotbarItem(entry.Item);

            var slotBehaviour = slot.root.gameObject.AddComponent<CreativeInventorySlot>();
            slotBehaviour.Configure(slotHost, inventory, creativeSource: true, slotIndex: i, item: entry.Item);
        }
    }

    public void SetVisible(bool visible)
    {
        if (Root != null)
        {
            Root.gameObject.SetActive(visible);
        }
    }

    private (RectTransform root, BlockItemSlotPreview preview) CreateCreativeSlot(
        Transform parent,
        string name,
        int column,
        int row)
    {
        var slotObject = new GameObject(name);
        slotObject.transform.SetParent(parent, false);

        var rect = slotObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(
            column * (slotSize + slotSpacing),
            -row * (slotSize + slotSpacing));

        var background = slotObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);

        iconObject.AddComponent<RawImage>();
        var preview = iconObject.AddComponent<BlockItemSlotPreview>();

        return (rect, preview);
    }
}
