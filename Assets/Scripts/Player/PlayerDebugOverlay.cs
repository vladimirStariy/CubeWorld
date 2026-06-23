using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class PlayerDebugOverlay : MonoBehaviour
{
    [SerializeField] private FirstPersonCharacterController playerController;
    [SerializeField] private BlockWorldClient worldClient;

    private InputAction toggleDebugAction;
    private Text debugText;
    private bool isVisible;
    private bool configured;
    private readonly System.Text.StringBuilder textBuilder = new(256);

    public void Configure(Canvas hudCanvas, FirstPersonCharacterController player, BlockWorldClient client)
    {
        if (configured)
        {
            return;
        }

        playerController = player;
        worldClient = client;

        SetupInput();
        CreateUi(hudCanvas);
        SetVisible(false);
        toggleDebugAction.Enable();
        configured = true;
    }

    private void OnEnable()
    {
        if (!configured)
        {
            return;
        }

        toggleDebugAction?.Enable();
    }

    private void OnDisable()
    {
        toggleDebugAction?.Disable();
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        if (toggleDebugAction.WasPressedThisFrame())
        {
            SetVisible(!isVisible);
        }

        if (!isVisible || playerController == null)
        {
            return;
        }

        var pos = Vector3Int.FloorToInt(playerController.transform.position);
        textBuilder.Clear();
        textBuilder.Append("XYZ: ").Append(pos.x).Append(' ').Append(pos.y).Append(' ').Append(pos.z);
        textBuilder.Append("\nFlight: ").Append(playerController.IsFlying ? "ON" : "off");

        if (worldClient != null && worldClient.TryGetBiomeAt(pos, out var biome, out var climate))
        {
            textBuilder.Append("\nBiome: ").Append(biome.DisplayName);
            textBuilder.Append(" (").Append(biome.Id).Append(')');
            textBuilder.Append("\nTemp: ").Append(climate.Temperature.ToString("0.0"));
        }

        if (worldClient != null && worldClient.TryGetLookTargetInfo(out var blockPos, out var faceNormal, out var blockInfo))
        {
            textBuilder.Append("\nBlock: ").Append(blockPos.x).Append(' ').Append(blockPos.y).Append(' ').Append(blockPos.z);
            textBuilder.Append("\nType: ").Append(GetBlockTypeLabel(blockInfo));
            if (blockInfo.IsChiseled)
            {
                var totalCells = blockInfo.MicroResolution * blockInfo.MicroResolution * blockInfo.MicroResolution;
                textBuilder.Append("\nMicro: ").Append(blockInfo.SolidMicroCells).Append(" / ").Append(totalCells);
            }

            textBuilder.Append("\nFace: ")
                .Append(faceNormal.x.ToString("0.##")).Append(' ')
                .Append(faceNormal.y.ToString("0.##")).Append(' ')
                .Append(faceNormal.z.ToString("0.##"));
        }
        else
        {
            textBuilder.Append("\nBlock: (none)");
        }

        RuntimeFrameProfiler.AppendReport(textBuilder);

        if (worldClient != null && worldClient.TryGetStreamingDiagnostics(out var baseMs, out var streamBudget))
        {
            textBuilder.Append("\nPacing base: ").Append(baseMs.ToString("0.0"));
            textBuilder.Append(" ms  stream budget: ").Append(streamBudget.ToString("0.0")).Append(" ms");
        }

        debugText.text = textBuilder.ToString();
    }

    private static string GetBlockTypeLabel(BlockQueryResult blockInfo)
    {
        if (blockInfo.IsChiseled)
        {
            return $"Chiseled {blockInfo.ChunkType}";
        }

        return blockInfo.ChunkType.ToString();
    }

    private void SetupInput()
    {
        toggleDebugAction = new InputAction("ToggleDebug", InputActionType.Button, "<Keyboard>/f3");
    }

    private void CreateUi(Canvas hudCanvas)
    {
        var existing = hudCanvas.GetComponentInChildren<PlayerDebugOverlayMarker>();
        if (existing != null)
        {
            debugText = existing.GetComponent<Text>();
            return;
        }

        var textObject = new GameObject("DebugOverlayText");
        textObject.transform.SetParent(hudCanvas.transform, false);
        textObject.AddComponent<PlayerDebugOverlayMarker>();

        debugText = textObject.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 20;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.color = new Color(1f, 1f, 1f, 0.9f);
        debugText.horizontalOverflow = HorizontalWrapMode.Overflow;
        debugText.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = debugText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -12f);
        rect.sizeDelta = new Vector2(420f, 360f);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        if (debugText != null)
        {
            debugText.enabled = visible;
        }
    }
}

public sealed class PlayerDebugOverlayMarker : MonoBehaviour
{
}
