using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class BlockItemSlotPreview : MonoBehaviour
{
    private const int TextureSize = 96;

    private RawImage rawImage;
    private Text itemLabel;
    private Text stackCountLabel;
    private RenderTexture renderTexture;
    private VoxelBlockType? blockType;
    private ItemKind? previewItemKind;
    private float spinPhase;
    private float spinAngle;
    private bool enableSpin = true;
    private float lastRenderedAngle = float.NaN;

    public VoxelBlockType? BlockType => blockType;
    public ItemKind? PreviewItemKind => previewItemKind;
    public float SpinAngle => spinAngle;
    public RenderTexture TargetTexture => renderTexture;

    public bool ShouldRender()
    {
        if (!blockType.HasValue && !previewItemKind.HasValue)
        {
            return false;
        }

        if (enableSpin)
        {
            return true;
        }

        return float.IsNaN(lastRenderedAngle) || !Mathf.Approximately(lastRenderedAngle, spinAngle);
    }

    public void MarkRendered()
    {
        lastRenderedAngle = spinAngle;
    }

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        spinAngle = 0f;
        spinPhase = 0f;

        renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            antiAliasing = 1,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();

        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        rawImage.enabled = false;
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void Update()
    {
        if (!enableSpin || (!blockType.HasValue && !previewItemKind.HasValue))
        {
            return;
        }

        spinAngle = spinPhase + Time.time * BlockItemGuiTransform.SpinSpeed;
    }

    public void SetHotbarItem(HotbarItem item, bool spin = true)
    {
        if (item.IsEmpty)
        {
            SetBlockType(null);
            HideStackCount();
            return;
        }

        if (item.Kind == ItemKind.Block)
        {
            SetBlockType(item.BlockType, spin);
            ShowStackCount(item.Count);
            return;
        }

        if (ItemPreviewMeshBuilder.SupportsPreview(item.Kind))
        {
            SetItemPreview(item.Kind, spin);
            ShowStackCount(item.Count);
            return;
        }

        blockType = null;
        previewItemKind = null;
        lastRenderedAngle = float.NaN;
        rawImage.enabled = false;
        enableSpin = false;
        ShowItemLabel(item.WithCount(1).GetDisplayName(), GetItemLabelColor(item.Kind));
        ShowStackCount(item.Count);
    }

    public void SetItemPreview(ItemKind kind, bool spin = true)
    {
        blockType = null;
        previewItemKind = kind;
        lastRenderedAngle = float.NaN;
        HideItemLabel();
        rawImage.enabled = true;
        SetSpinning(spin);
    }

    public void SetBlockType(VoxelBlockType? type, bool spin = true)
    {
        blockType = type == VoxelBlockType.Air ? null : type;
        previewItemKind = null;
        lastRenderedAngle = float.NaN;
        HideItemLabel();
        HideStackCount();

        if (blockType.HasValue)
        {
            rawImage.enabled = true;
            SetSpinning(spin);
        }
        else
        {
            rawImage.enabled = false;
            enableSpin = false;
        }
    }

    public void SetSpinning(bool spin)
    {
        if (enableSpin == spin)
        {
            return;
        }

        if (spin)
        {
            spinPhase = spinAngle - Time.time * BlockItemGuiTransform.SpinSpeed;
        }
        else
        {
            spinAngle = 0f;
        }

        enableSpin = spin;
        lastRenderedAngle = float.NaN;
    }

    private void ShowItemLabel(string text, Color color)
    {
        EnsureItemLabel();
        itemLabel.text = text;
        itemLabel.color = color;
        itemLabel.enabled = true;
    }

    private void HideItemLabel()
    {
        if (itemLabel != null)
        {
            itemLabel.enabled = false;
        }
    }

    private void ShowStackCount(int count)
    {
        if (count <= 1)
        {
            HideStackCount();
            return;
        }

        EnsureStackCountLabel();
        stackCountLabel.text = count.ToString();
        stackCountLabel.enabled = true;
    }

    private void HideStackCount()
    {
        if (stackCountLabel != null)
        {
            stackCountLabel.enabled = false;
        }
    }

    private void EnsureStackCountLabel()
    {
        if (stackCountLabel != null)
        {
            return;
        }

        var labelObject = new GameObject("StackCountLabel");
        labelObject.transform.SetParent(transform, false);
        var rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-2f, 2f);
        rect.sizeDelta = new Vector2(28f, 16f);

        stackCountLabel = labelObject.AddComponent<Text>();
        stackCountLabel.alignment = TextAnchor.LowerRight;
        stackCountLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stackCountLabel.fontSize = 11;
        stackCountLabel.fontStyle = FontStyle.Bold;
        stackCountLabel.color = Color.white;
        stackCountLabel.raycastTarget = false;
    }

    private void EnsureItemLabel()
    {
        if (itemLabel != null)
        {
            return;
        }

        var labelObject = new GameObject("ItemLabel");
        labelObject.transform.SetParent(transform, false);
        var rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        itemLabel = labelObject.AddComponent<Text>();
        itemLabel.alignment = TextAnchor.MiddleCenter;
        itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemLabel.fontSize = 12;
        itemLabel.raycastTarget = false;
    }

    private static Color GetItemLabelColor(ItemKind kind)
    {
        return kind switch
        {
            ItemKind.GrassBundle => new Color(0.45f, 0.82f, 0.35f),
            ItemKind.Stick => new Color(0.72f, 0.52f, 0.28f),
            ItemKind.Flint => new Color(0.75f, 0.78f, 0.82f),
            ItemKind.Chisel => new Color(0.68f, 0.68f, 0.72f),
            ItemKind.Clay => new Color(0.82f, 0.55f, 0.35f),
            ItemKind.RawClayBowl => new Color(0.78f, 0.5f, 0.32f),
            ItemKind.ClayBowl => new Color(0.62f, 0.4f, 0.28f),
            _ => Color.white
        };
    }
}
