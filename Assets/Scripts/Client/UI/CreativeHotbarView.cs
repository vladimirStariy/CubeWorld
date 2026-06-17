using UnityEngine;
using UnityEngine.UI;

public sealed class CreativeHotbarView
{
    private static readonly Color FillColor = new Color(0.216f, 0.216f, 0.216f, 1f);
    private static readonly Color BorderColor = new Color(0.541f, 0.541f, 0.541f, 1f);
    private static readonly Color SelectedBorderColor = Color.white;

    private readonly float slotSize;
    private readonly float slotSpacing;
    private readonly float slotBorder;

    private Image[] borders;
    private Image[] fills;

    public RectTransform Root { get; private set; }
    public BlockItemSlotPreview[] Previews { get; private set; }

    public CreativeHotbarView(float slotSize, float slotSpacing, float slotBorder)
    {
        this.slotSize = slotSize;
        this.slotSpacing = slotSpacing;
        this.slotBorder = slotBorder;
    }

    public void Build(
        Transform parent,
        float bottomPadding,
        CreativeInventory inventory,
        ICreativeInventorySlotHost slotHost)
    {
        Root = HudUiFactory.CreatePanel(
            parent,
            "Hotbar",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, bottomPadding));

        var totalWidth = CreativeInventory.HotbarSize * slotSize + (CreativeInventory.HotbarSize - 1) * slotSpacing;
        Root.sizeDelta = new Vector2(totalWidth, slotSize);

        borders = new Image[CreativeInventory.HotbarSize];
        fills = new Image[CreativeInventory.HotbarSize];
        Previews = new BlockItemSlotPreview[CreativeInventory.HotbarSize];

        for (int i = 0; i < CreativeInventory.HotbarSize; i++)
        {
            var slot = CreateHotbarSlot(Root, $"HotbarSlot{i + 1}", i);
            borders[i] = slot.border;
            fills[i] = slot.fill;
            Previews[i] = slot.preview;

            var slotBehaviour = slot.root.gameObject.AddComponent<CreativeInventorySlot>();
            slotBehaviour.Configure(slotHost, inventory, creativeSource: false, slotIndex: i, item: default);
        }
    }

    public void Refresh(CreativeInventory inventory)
    {
        if (borders == null)
        {
            return;
        }

        for (int i = 0; i < borders.Length; i++)
        {
            var isSelected = i == inventory.SelectedSlot;
            borders[i].color = isSelected ? SelectedBorderColor : BorderColor;
            fills[i].color = FillColor;

            var item = inventory.GetHotbarSlot(i);
            Previews[i].SetHotbarItem(item.IsEmpty ? default : item, spin: isSelected);
        }
    }

    private (RectTransform root, Image border, Image fill, BlockItemSlotPreview preview) CreateHotbarSlot(
        Transform parent,
        string name,
        int index)
    {
        var slotObject = new GameObject(name);
        slotObject.transform.SetParent(parent, false);

        var rect = slotObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(index * (slotSize + slotSpacing), 0f);

        var border = slotObject.AddComponent<Image>();
        border.color = BorderColor;

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(slotObject.transform, false);
        var fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(slotBorder, slotBorder);
        fillRect.offsetMax = new Vector2(-slotBorder, -slotBorder);

        var fill = fillObject.AddComponent<Image>();
        fill.color = FillColor;
        fill.raycastTarget = false;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        var iconInset = slotBorder + 2f;
        iconRect.offsetMin = new Vector2(iconInset, iconInset);
        iconRect.offsetMax = new Vector2(-iconInset, -iconInset);

        iconObject.AddComponent<RawImage>();
        var preview = iconObject.AddComponent<BlockItemSlotPreview>();

        return (rect, border, fill, preview);
    }
}
