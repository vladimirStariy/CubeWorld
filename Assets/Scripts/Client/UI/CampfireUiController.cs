using UnityEngine;
using UnityEngine.UI;

public sealed class CampfireUiController : MonoBehaviour
{
    private BlockWorldServer worldServer;
    private FirstPersonCharacterController playerController;
    private RectTransform root;
    private Text titleText;
    private Text statusText;
    private Text valuesText;
    private Button addInputButton;
    private Button addFuelButton;
    private Button takeOutputButton;
    private Button closeButton;
    private bool configured;
    private bool isOpen;
    private Vector3Int currentCampfire;

    public bool IsOpen => isOpen;

    public void Configure(Canvas hudCanvas, BlockWorldServer server, FirstPersonCharacterController player)
    {
        if (configured)
        {
            return;
        }

        worldServer = server;
        playerController = player;
        BuildUi(hudCanvas);
        SetOpen(false);
        configured = true;
    }

    public bool TryOpen(Vector3Int blockPosition)
    {
        if (!configured || worldServer == null || !worldServer.TryGetCampfireState(blockPosition, out var state))
        {
            return false;
        }

        currentCampfire = blockPosition;
        UpdateStateText(state, "Campfire opened.");
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

        if (!worldServer.TryGetCampfireState(currentCampfire, out var state))
        {
            SetOpen(false);
            return;
        }

        UpdateStateText(state, statusText != null ? statusText.text : string.Empty);
    }

    private void BuildUi(Canvas hudCanvas)
    {
        var existing = hudCanvas.GetComponentInChildren<CampfireUiMarker>(true);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        var rootObject = new GameObject("CampfireUI");
        rootObject.transform.SetParent(hudCanvas.transform, false);
        rootObject.AddComponent<CampfireUiMarker>();

        root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(320f, 240f);

        var bg = rootObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.86f);

        titleText = HudUiFactory.CreateText(root, "Campfire", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), 22, TextAnchor.UpperCenter);
        valuesText = HudUiFactory.CreateText(root, string.Empty, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), 16, TextAnchor.UpperLeft);
        statusText = HudUiFactory.CreateText(root, string.Empty, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), 14, TextAnchor.LowerCenter);

        addInputButton = CreateButton("Add Input", new Vector2(-100f, -52f), () => Interact(CampfireInteraction.AddInput));
        addFuelButton = CreateButton("Add Fuel", new Vector2(0f, -52f), () => Interact(CampfireInteraction.AddFuel));
        takeOutputButton = CreateButton("Take Output", new Vector2(100f, -52f), () => Interact(CampfireInteraction.TakeOutput));
        closeButton = CreateButton("Close", new Vector2(0f, -94f), () => SetOpen(false));
    }

    private Button CreateButton(string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(label.Replace(" ", string.Empty));
        buttonObject.transform.SetParent(root, false);

        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(90f, 30f);
        rect.anchoredPosition = anchoredPosition;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = HudUiFactory.CreateText(rect, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, 13, TextAnchor.MiddleCenter);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private void Interact(CampfireInteraction interaction)
    {
        if (!worldServer.TryInteractCampfire(currentCampfire, interaction, out var state, out var message))
        {
            UpdateStateText(state, message);
            return;
        }

        UpdateStateText(state, message);
    }

    private void UpdateStateText(CampfireState state, string status)
    {
        if (valuesText != null)
        {
            valuesText.text =
                $"Input:  {state.InputCount}\n" +
                $"Fuel:   {state.FuelCount}\n" +
                $"Output: {state.OutputCount}\n" +
                $"Lit:    {(state.IsLit ? "Yes" : "No")}\n" +
                $"Burn:   {state.BurnTimeRemaining:0.0}s\n" +
                $"Cook:   {state.CookProgress:0.0}s / 4.0s";
        }

        if (statusText != null)
        {
            statusText.text = status;
        }
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

public sealed class CampfireUiMarker : MonoBehaviour
{
}
