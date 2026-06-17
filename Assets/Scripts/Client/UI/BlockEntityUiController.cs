using UnityEngine;
using UnityEngine.UI;

public sealed class BlockEntityUiController : MonoBehaviour
{
    private const int MaxActionButtons = 4;

    private BlockWorldServer worldServer;
    private FirstPersonCharacterController playerController;
    private BlockEntityUiRegistry registry;
    private RectTransform root;
    private Text titleText;
    private Text statusText;
    private Text bodyText;
    private Button[] actionButtons;
    private Text[] actionButtonLabels;
    private Button closeButton;
    private bool configured;
    private bool isOpen;
    private Vector3Int currentBlock;
    private IBlockEntityUiProvider currentProvider;
    private string currentStatus;

    public bool IsOpen => isOpen;

    public void Configure(
        Canvas hudCanvas,
        BlockWorldServer server,
        FirstPersonCharacterController player,
        BlockEntityUiRegistry uiRegistry)
    {
        if (configured)
        {
            return;
        }

        worldServer = server;
        playerController = player;
        registry = uiRegistry;
        BuildUi(hudCanvas);
        SetOpen(false);
        configured = true;
    }

    public bool TryOpen(Vector3Int blockPosition)
    {
        if (!configured || registry == null || !registry.TryGetProvider(blockPosition, worldServer, out var provider))
        {
            return false;
        }

        currentProvider = provider;
        currentBlock = blockPosition;
        currentStatus = null;
        if (!RebuildState())
        {
            return false;
        }

        SetOpen(true);
        return true;
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
            return;
        }

        if (!RebuildState())
        {
            SetOpen(false);
        }
    }

    private bool RebuildState()
    {
        if (currentProvider == null || !currentProvider.TryBuildState(currentBlock, worldServer, currentStatus, out var state))
        {
            return false;
        }

        ApplyState(state);
        return true;
    }

    private void ApplyState(BlockEntityUiState state)
    {
        if (titleText != null)
        {
            titleText.text = state?.Title ?? string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = state?.Body ?? string.Empty;
        }

        if (statusText != null)
        {
            statusText.text = state?.Status ?? string.Empty;
        }

        var actions = state?.Actions;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            var hasAction = actions != null && i < actions.Length;
            actionButtons[i].gameObject.SetActive(hasAction);
            if (!hasAction)
            {
                continue;
            }

            var action = actions[i];
            actionButtonLabels[i].text = action.Label;
            actionButtons[i].onClick.RemoveAllListeners();
            actionButtons[i].onClick.AddListener(() => OnActionClicked(action.Id));
        }
    }

    private void OnActionClicked(string actionId)
    {
        if (currentProvider == null)
        {
            return;
        }

        if (currentProvider.TryHandleAction(currentBlock, worldServer, actionId, out var message))
        {
            currentStatus = message;
            RebuildState();
        }
    }

    private void BuildUi(Canvas hudCanvas)
    {
        var existing = hudCanvas.GetComponentInChildren<BlockEntityUiMarker>(true);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        var rootObject = new GameObject("BlockEntityUI");
        rootObject.transform.SetParent(hudCanvas.transform, false);
        rootObject.AddComponent<BlockEntityUiMarker>();

        root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(340f, 260f);

        var bg = rootObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.86f);

        titleText = HudUiFactory.CreateText(root, "Block Entity", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), 22, TextAnchor.UpperCenter);
        bodyText = HudUiFactory.CreateText(root, string.Empty, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), 16, TextAnchor.UpperLeft);
        statusText = HudUiFactory.CreateText(root, string.Empty, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), 14, TextAnchor.LowerCenter);

        actionButtons = new Button[MaxActionButtons];
        actionButtonLabels = new Text[MaxActionButtons];
        for (int i = 0; i < MaxActionButtons; i++)
        {
            var x = -120f + i * 80f;
            var tuple = CreateButton($"Action{i + 1}", new Vector2(x, -58f), null);
            actionButtons[i] = tuple.button;
            actionButtonLabels[i] = tuple.label;
            actionButtons[i].gameObject.SetActive(false);
        }

        closeButton = CreateButton("Close", new Vector2(0f, -100f), () => SetOpen(false)).button;
    }

    private (Button button, Text label) CreateButton(string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(label.Replace(" ", string.Empty));
        buttonObject.transform.SetParent(root, false);

        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(74f, 30f);
        rect.anchoredPosition = anchoredPosition;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        var button = buttonObject.AddComponent<Button>();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var text = HudUiFactory.CreateText(rect, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, 12, TextAnchor.MiddleCenter);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return (button, text);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        if (root != null)
        {
            root.gameObject.SetActive(open);
        }

        playerController?.SetGameplayCaptured(!open);
    }
}

public sealed class BlockEntityUiMarker : MonoBehaviour
{
}
