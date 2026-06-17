using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameCommandConsoleInput
{
    private readonly List<string> commandHistory = new();
    private int historyIndex;

    public InputAction ToggleAction { get; private set; }
    public InputAction SubmitAction { get; private set; }

    public void Build(System.Action<InputAction.CallbackContext> onSubmit)
    {
        ToggleAction = new InputAction("ToggleCommandConsole", InputActionType.Button, "<Keyboard>/t");
        SubmitAction = new InputAction("SubmitCommandConsole", InputActionType.Button);
        SubmitAction.AddBinding("<Keyboard>/enter");
        SubmitAction.AddBinding("<Keyboard>/numpadEnter");
        SubmitAction.performed += onSubmit;
    }

    public void Dispose(System.Action<InputAction.CallbackContext> onSubmit)
    {
        if (SubmitAction != null)
        {
            SubmitAction.performed -= onSubmit;
        }
    }

    public void Enable()
    {
        ToggleAction?.Enable();
        SubmitAction?.Enable();
    }

    public void Disable()
    {
        ToggleAction?.Disable();
        SubmitAction?.Disable();
    }

    public void AddToHistory(string line)
    {
        commandHistory.Add(line);
        historyIndex = commandHistory.Count;
    }

    public void ResetHistoryIndex()
    {
        historyIndex = commandHistory.Count;
    }

    public string NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0)
        {
            return string.Empty;
        }

        historyIndex = Mathf.Clamp(historyIndex + direction, 0, commandHistory.Count);
        return historyIndex < commandHistory.Count ? commandHistory[historyIndex] : string.Empty;
    }
}
