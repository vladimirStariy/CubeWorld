using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ClayFormingController : MonoBehaviour
{
    private const float EditCooldownSeconds = 0.05f;
    private const float InteractDistance = 8f;

    private static readonly ClayFormingToolMode[] ToolCycle =
    {
        ClayFormingToolMode.Brush1,
        ClayFormingToolMode.Brush2,
        ClayFormingToolMode.Brush3
    };

    private IWorldAuthority authority;
    private IGameServerConnection connection;
    private Camera playerCamera;
    private CreativeInventory inventory;
    private CreativeInventoryUI creativeInventoryUi;
    private FirstPersonCharacterController playerController;
    private ClayFormingWorldVisualizer visualizer;
    private readonly ClayFormingRecipeSelectUi recipeMenu = new();
    private readonly BlockWorldInputBindings input = new();
    private readonly List<ClayWorksiteSnapshot> snapshotBuffer = new();

    private ClayWorksiteKey pendingWorksiteKey;
    private bool hasPendingWorksite;
    private float nextEditTime;
    private bool configured;

    public bool IsMenuBlockingInput => recipeMenu.IsOpen;

    public void Configure(
        IGameServerConnection serverConnection,
        Camera camera,
        CreativeInventory inventoryState,
        CreativeInventoryUI inventoryUi,
        FirstPersonCharacterController player,
        Canvas hudCanvas)
    {
        if (configured)
        {
            return;
        }

        connection = serverConnection;
        authority = serverConnection.Authority;
        playerCamera = camera;
        inventory = inventoryState;
        creativeInventoryUi = inventoryUi;
        playerController = player;

        visualizer = gameObject.AddComponent<ClayFormingWorldVisualizer>();
        visualizer.Configure(transform);

        var uiRoot = new GameObject("ClayFormingUI");
        uiRoot.transform.SetParent(hudCanvas.transform, false);
        var uiRect = uiRoot.AddComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.offsetMin = Vector2.zero;
        uiRect.offsetMax = Vector2.zero;

        recipeMenu.Build(uiRoot.transform);
        recipeMenu.RecipeSelected += OnRecipeSelected;
        recipeMenu.Closed += OnRecipeMenuClosed;

        input.Build();
        input.Enable();
        configured = true;
    }

    private void OnDisable() => input.Disable();

    private void OnEnable()
    {
        if (configured)
        {
            input.Enable();
        }
    }

    public void UpdateContinuous()
    {
        if (!configured || connection == null || authority == null)
        {
            return;
        }

        authority.CopyClayWorksiteSnapshots(snapshotBuffer);
        visualizer.SyncAll(snapshotBuffer);

        if (creativeInventoryUi != null && creativeInventoryUi.IsCreativePanelOpen)
        {
            return;
        }

        if (recipeMenu.IsOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && recipeMenu.IsOpen)
        {
            CloseRecipeMenu();
        }

        HandleSculptHold();
    }

    public bool TryHandleShiftPlace()
    {
        if (!configured || !IsClaySelected() || !InputModifiers.IsShiftHeld() || !input.PlaceAction.WasPressedThisFrame())
        {
            return false;
        }

        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, InteractDistance, out var hit))
        {
            return false;
        }

        var anchor = BlockWorldTargeting.GetHitBlockPosition(hit);
        var faceNormal = Vector3Int.RoundToInt(hit.normal);

        if (authority.TryFindClayWorksite(anchor, faceNormal, out var existing))
        {
            if (existing.HasSession)
            {
                return true;
            }

            pendingWorksiteKey = existing.Key;
            hasPendingWorksite = true;
            OpenRecipeMenu();
            return true;
        }

        var result = connection.ExecuteCommand(WorldCommand.PlaceClayWorksite(anchor, faceNormal));
        if (!result.Success)
        {
            Debug.Log(result.Message);
            return true;
        }

        WorldPlacementChanged?.Invoke();
        pendingWorksiteKey = result.ClayWorksiteKey;
        hasPendingWorksite = true;
        OpenRecipeMenu();
        return true;
    }

    private void OpenRecipeMenu()
    {
        recipeMenu.SetOpen(true);
        playerController?.SetGameplayCaptured(false);
    }

    private void CloseRecipeMenu()
    {
        recipeMenu.SetOpen(false);
        playerController?.SetGameplayCaptured(true);
    }

    public bool ShouldSuppressWorldBreak()
    {
        return IsClaySelected() && TryGetTargetWorksite(out _, out var worksite) && worksite.HasSession;
    }

    public bool ShouldSuppressWorldPlace()
    {
        if (!IsClaySelected())
        {
            return false;
        }

        if (InputModifiers.IsShiftHeld())
        {
            return TryGetTargetWorksite(out _, out _);
        }

        return TryGetTargetWorksite(out _, out var worksite) && worksite.HasSession;
    }

    private void HandleSculptHold()
    {
        if (!IsClaySelected() || !TryGetTargetWorksite(out var key, out var worksite) || !worksite.HasSession)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CycleToolMode(key, worksite.Session.ToolMode);
        }

        if (!TryGetTargetCell(worksite.Session, out var u, out var v) || Time.time < nextEditTime)
        {
            return;
        }

        if (input.PlaceAction.IsPressed())
        {
            if (TryClayEditCommand(WorldCommand.ClayFormingAdd(key, u, v)))
            {
                nextEditTime = Time.time + EditCooldownSeconds;
            }
        }
        else if (input.BreakAction.IsPressed())
        {
            if (TryClayEditCommand(WorldCommand.ClayFormingRemove(key, u, v)))
            {
                nextEditTime = Time.time + EditCooldownSeconds;
            }
        }
    }

    private bool TryClayEditCommand(WorldCommand command)
    {
        var result = connection.ExecuteCommand(command);
        if (!result.Success || !result.HasClayEditResult || !result.ClayEditResult.Changed)
        {
            return false;
        }

        HandleEditResult(result.ClayEditResult);
        return true;
    }

    private void CycleToolMode(ClayWorksiteKey key, ClayFormingToolMode current)
    {
        var index = 0;
        for (int i = 0; i < ToolCycle.Length; i++)
        {
            if (ToolCycle[i] == current)
            {
                index = (i + 1) % ToolCycle.Length;
                break;
            }
        }

        var next = ToolCycle[index];
        connection.ExecuteCommand(WorldCommand.SetClayFormingToolMode(key, next));
        Debug.Log($"Clay tool: {next}");
    }

    private void HandleEditResult(ClayFormingEditResult result)
    {
        if (result.RecipeCompleted && !result.OutputItem.IsEmpty)
        {
            Debug.Log($"Clay forming complete: {result.OutputItem.GetDisplayName()} placed.");
            WorldPlacementChanged?.Invoke();
        }
    }

    public event System.Action WorldPlacementChanged;

    private void OnRecipeSelected(string recipeId)
    {
        if (!hasPendingWorksite)
        {
            return;
        }

        var key = pendingWorksiteKey;
        hasPendingWorksite = false;
        CloseRecipeMenu();

        var startResult = connection.ExecuteCommand(WorldCommand.StartClayForming(key, recipeId));
        if (!startResult.Success)
        {
            Debug.Log(startResult.Message);
            connection.ExecuteCommand(WorldCommand.RemoveClayWorksite(key));
        }
        else
        {
            Debug.Log($"Forming {recipeId}. Carve the clay pad to match outlines. RMB = add, LMB = remove. F = brush.");
        }
    }

    private void OnRecipeMenuClosed()
    {
        playerController?.SetGameplayCaptured(true);

        if (!hasPendingWorksite)
        {
            return;
        }

        connection.ExecuteCommand(WorldCommand.RemoveClayWorksite(pendingWorksiteKey));
        hasPendingWorksite = false;
    }

    private bool TryGetTargetWorksite(out ClayWorksiteKey key, out ClayWorksite worksite)
    {
        key = default;
        worksite = null;
        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, InteractDistance, out var hit))
        {
            return false;
        }

        var anchor = BlockWorldTargeting.GetHitBlockPosition(hit);
        var faceNormal = Vector3Int.RoundToInt(hit.normal);
        if (!authority.TryFindClayWorksite(anchor, faceNormal, out worksite))
        {
            return false;
        }

        key = worksite.Key;
        return true;
    }

    private bool TryGetTargetCell(ClayFormingSession session, out int u, out int v)
    {
        u = -1;
        v = -1;
        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, InteractDistance, out var hit))
        {
            return false;
        }

        return ClayFormingCoordinates.TryWorldPointToCell(
            hit.point,
            session.AnchorBlock,
            session.FaceNormal,
            session.CurrentWorldLayer,
            out u,
            out v);
    }

    private bool IsClaySelected()
    {
        return inventory != null
               && inventory.TryGetSelectedItem(out var item)
               && item.IsClay;
    }
}
