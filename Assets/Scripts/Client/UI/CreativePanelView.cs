using UnityEngine;
using UnityEngine.UI;

public sealed class CreativePanelView
{
    private readonly float slotSize;
    private readonly float slotSpacing;
    private readonly float panelWidth;
    private readonly float panelHeight;
    private readonly float hotbarBottomPadding;
    private readonly float hotbarSlotSize;

    public RectTransform Root { get; private set; }
    public BlockItemSlotPreview[] Previews { get; private set; }

    public CreativePanelView(
        float slotSize,
        float slotSpacing,
        float panelWidth,
        float panelHeight,
        float hotbarBottomPadding,
        float hotbarSlotSize)
    {
        this.slotSize = slotSize;
        this.slotSpacing = slotSpacing;
        this.panelWidth = panelWidth;
        this.panelHeight = panelHeight;
        this.hotbarBottomPadding = hotbarBottomPadding;
        this.hotbarSlotSize = hotbarSlotSize;
    }

    public void Build(Transform parent, CreativeInventory inventory, ICreativeInventorySlotHost slotHost)
    {
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

        var itemsRoot = new GameObject("CreativeItems");
        itemsRoot.transform.SetParent(Root, false);
        var itemsRect = itemsRoot.AddComponent<RectTransform>();
        itemsRect.anchorMin = new Vector2(0f, 0f);
        itemsRect.anchorMax = new Vector2(1f, 1f);
        itemsRect.offsetMin = new Vector2(12f, 12f);
        itemsRect.offsetMax = new Vector2(-12f, -36f);

        var creativeEntries = CreativeInventory.GetCreativeEntries();
        Previews = new BlockItemSlotPreview[creativeEntries.Length];
        for (int i = 0; i < creativeEntries.Length; i++)
        {
            var entry = creativeEntries[i];
            var slot = CreateCreativeSlot(itemsRoot.transform, $"CreativeItem_{entry.Label}", i);
            Previews[i] = slot.preview;
            slot.preview.SetHotbarItem(entry.Item);

            var slotBehaviour = slot.root.gameObject.AddComponent<CreativeInventorySlot>();
            slotBehaviour.Configure(slotHost, inventory, creativeSource: true, slotIndex: -1, item: entry.Item);
        }
    }

    public void SetVisible(bool visible)
    {
        if (Root != null)
        {
            Root.gameObject.SetActive(visible);
        }
    }

    private (RectTransform root, BlockItemSlotPreview preview) CreateCreativeSlot(Transform parent, string name, int index)
    {
        var slotObject = new GameObject(name);
        slotObject.transform.SetParent(parent, false);

        var rect = slotObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(index * (slotSize + slotSpacing), 0f);

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
