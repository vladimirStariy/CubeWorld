using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BlockWorldInputBindings
{
    public InputAction PlaceAction { get; private set; }
    public InputAction BreakAction { get; private set; }
    public InputAction ChiselAction { get; private set; }

    public void Build()
    {
        PlaceAction = new InputAction("PlaceBlock", InputActionType.Button, "<Mouse>/rightButton");
        BreakAction = new InputAction("BreakBlock", InputActionType.Button, "<Mouse>/leftButton");
        ChiselAction = new InputAction("ChiselBlock", InputActionType.Button, "<Mouse>/middleButton");
    }

    public void Enable()
    {
        PlaceAction?.Enable();
        BreakAction?.Enable();
        ChiselAction?.Enable();
    }

    public void Disable()
    {
        PlaceAction?.Disable();
        BreakAction?.Disable();
        ChiselAction?.Disable();
    }
}
