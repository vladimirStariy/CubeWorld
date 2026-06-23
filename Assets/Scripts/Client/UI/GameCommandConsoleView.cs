using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameCommandConsoleView
{
    private const int MaxLogLines = 16;
    private const float InputBarHeight = 28f;
    private const float LogPanelHeight = 120f;
    private const float BottomOffset = 60f;

    private readonly List<string> logLines = new();
    private RectTransform root;
    private Text logText;
    private InputField inputField;

    public RectTransform Root => root;
    public InputField InputField => inputField;

    public void Build(Canvas hudCanvas, UnityEngine.Events.UnityAction<string> onEndEdit)
    {
        if (root != null)
        {
            return;
        }

        var existing = hudCanvas.GetComponentInChildren<GameCommandConsoleMarker>();
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        var consoleObject = new GameObject("GameCommandConsoleUi");
        consoleObject.transform.SetParent(hudCanvas.transform, false);
        consoleObject.AddComponent<GameCommandConsoleMarker>();

        root = consoleObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var logPanel = HudUiFactory.CreatePanel(
            root,
            "LogPanel",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, BottomOffset + InputBarHeight));
        logPanel.sizeDelta = new Vector2(0f, LogPanelHeight);
        var logBackground = logPanel.gameObject.AddComponent<Image>();
        logBackground.color = new Color(0f, 0f, 0f, 0.45f);

        var logTextObject = new GameObject("LogText");
        logTextObject.transform.SetParent(logPanel, false);
        var logTextRect = logTextObject.AddComponent<RectTransform>();
        logTextRect.anchorMin = Vector2.zero;
        logTextRect.anchorMax = Vector2.one;
        logTextRect.offsetMin = new Vector2(8f, 6f);
        logTextRect.offsetMax = new Vector2(-8f, -6f);

        logText = logTextObject.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 14;
        logText.alignment = TextAnchor.LowerLeft;
        logText.color = new Color(1f, 1f, 1f, 0.92f);
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.supportRichText = false;

        var inputPanel = HudUiFactory.CreatePanel(
            root,
            "InputPanel",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, BottomOffset));
        inputPanel.sizeDelta = new Vector2(0f, InputBarHeight);
        var inputBackground = inputPanel.gameObject.AddComponent<Image>();
        inputBackground.color = new Color(0f, 0f, 0f, 0.72f);

        inputField = BuildInputField(inputPanel, onEndEdit);
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }
    }

    public void FocusInput()
    {
        if (inputField == null)
        {
            return;
        }

        inputField.ActivateInputField();
        inputField.Select();
    }

    public void ClearInput()
    {
        if (inputField != null)
        {
            inputField.text = string.Empty;
        }
    }

    public void SetInputText(string text)
    {
        if (inputField != null)
        {
            inputField.text = text;
            inputField.caretPosition = text.Length;
        }
    }

    public string GetInputText() => inputField != null ? inputField.text : string.Empty;

    public void AppendLog(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        logLines.Add(line);
        while (logLines.Count > MaxLogLines)
        {
            logLines.RemoveAt(0);
        }

        RefreshLogText();
    }

    public void ClearLog()
    {
        logLines.Clear();
        RefreshLogText();
    }

    private void RefreshLogText()
    {
        if (logText == null)
        {
            return;
        }

        if (logLines.Count == 0)
        {
            logText.text = string.Empty;
            return;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < logLines.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(logLines[i]);
        }

        logText.text = builder.ToString();
    }

    private static InputField BuildInputField(RectTransform inputPanel, UnityEngine.Events.UnityAction<string> onEndEdit)
    {
        var inputObject = new GameObject("Input");
        inputObject.transform.SetParent(inputPanel, false);
        var inputRect = inputObject.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = new Vector2(8f, 4f);
        inputRect.offsetMax = new Vector2(-8f, -4f);

        var placeholderObject = new GameObject("Placeholder");
        placeholderObject.transform.SetParent(inputObject.transform, false);
        var placeholderRect = placeholderObject.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        var placeholder = placeholderObject.AddComponent<Text>();
        placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.fontSize = 14;
        placeholder.text = "Enter command...";
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        placeholder.supportRichText = false;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(inputObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var inputText = textObject.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 14;
        inputText.color = Color.white;
        inputText.supportRichText = false;

        var inputField = inputObject.AddComponent<InputField>();
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.characterLimit = 256;
        inputField.onEndEdit.AddListener(onEndEdit);
        return inputField;
    }
}

public sealed class GameCommandConsoleMarker : MonoBehaviour
{
}
