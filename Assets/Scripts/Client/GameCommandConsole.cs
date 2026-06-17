using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameCommandConsole : MonoBehaviour
{
    [SerializeField] private FirstPersonCharacterController playerController;
    [SerializeField] private CreativeInventory inventory;
    [SerializeField] private BlockWorldServer worldServer;
    [SerializeField] private CreativeInventoryUI creativeInventoryUi;

    private readonly GameCommandConsoleView view = new();
    private readonly GameCommandConsoleInput consoleInput = new();

    private bool isOpen;
    private bool configured;
    private int lastSubmitFrame = -1;

    public bool IsOpen => isOpen;

    public void Configure(
        Canvas hudCanvas,
        FirstPersonCharacterController player,
        CreativeInventory inventoryRef,
        BlockWorldServer server,
        CreativeInventoryUI inventoryUi)
    {
        if (configured)
        {
            return;
        }

        playerController = player;
        inventory = inventoryRef;
        worldServer = server;
        creativeInventoryUi = inventoryUi;

        consoleInput.Build(OnSubmitPerformed);
        view.Build(hudCanvas, OnInputEndEdit);
        SetOpen(false);
        consoleInput.Enable();
        configured = true;
    }

    private void OnEnable()
    {
        if (configured)
        {
            consoleInput.Enable();
        }
    }

    private void OnDisable()
    {
        consoleInput.Disable();
        if (isOpen)
        {
            isOpen = false;
            playerController?.SetGameplayCaptured(true);
        }
    }

    private void OnDestroy()
    {
        consoleInput.Dispose(OnSubmitPerformed);
    }

    public void Close() => SetOpen(false);

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!isOpen)
        {
            return;
        }

        SubmitLine(view.GetInputText());
    }

    private void OnInputEndEdit(string text)
    {
        if (!isOpen || !WasSubmitKeyPressed())
        {
            return;
        }

        SubmitLine(text);
    }

    private static bool WasSubmitKeyPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.enterKey.wasPressedThisFrame
               || keyboard.numpadEnterKey.wasPressedThisFrame
               || keyboard.enterKey.wasReleasedThisFrame
               || keyboard.numpadEnterKey.wasReleasedThisFrame;
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        if (consoleInput.ToggleAction.WasPressedThisFrame())
        {
            if (!isOpen)
            {
                if (IsCreativeInventoryOpen())
                {
                    return;
                }

                OpenWithPrefix(string.Empty);
            }
            else if (view.InputField == null || !view.InputField.isFocused)
            {
                SetOpen(false);
            }

            return;
        }

        if (Keyboard.current != null && Keyboard.current.slashKey.wasPressedThisFrame && !isOpen)
        {
            if (IsCreativeInventoryOpen())
            {
                return;
            }

            OpenWithPrefix("/");
            return;
        }

        if (!isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
            return;
        }

        if (view.InputField == null || !view.InputField.isFocused)
        {
            return;
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            view.SetInputText(consoleInput.NavigateHistory(-1));
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            view.SetInputText(consoleInput.NavigateHistory(1));
        }
    }

    private void SubmitLine(string text)
    {
        if (Time.frameCount == lastSubmitFrame)
        {
            return;
        }

        lastSubmitFrame = Time.frameCount;

        var line = text.Trim();
        view.ClearInput();
        consoleInput.ResetHistoryIndex();

        if (line.Length == 0)
        {
            view.FocusInput();
            return;
        }

        consoleInput.AddToHistory(line);
        view.AppendLog($"> {line}");

        var context = new GameCommandExecutor.Context
        {
            Player = playerController,
            Inventory = inventory,
            World = worldServer,
            ClearLog = view.ClearLog
        };

        if (GameCommandExecutor.TryExecute(line, context, out var response) && !string.IsNullOrEmpty(response))
        {
            view.AppendLog(response);
        }

        view.FocusInput();
    }

    private void OpenWithPrefix(string prefix)
    {
        SetOpen(true);
        view.SetInputText(prefix);
        view.FocusInput();
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        view.SetVisible(open);
        playerController?.SetGameplayCaptured(!open);

        if (open)
        {
            view.FocusInput();
        }
    }

    private bool IsCreativeInventoryOpen()
    {
        return creativeInventoryUi != null && creativeInventoryUi.IsCreativePanelOpen;
    }
}
