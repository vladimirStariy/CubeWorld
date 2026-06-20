using UnityEngine.InputSystem;

public sealed class BlockWorldInputBindings
{
    public InputAction PlaceAction { get; private set; }
    public InputAction BreakAction { get; private set; }

    public void Build()
    {
        PlaceAction = new InputAction("PlaceBlock", InputActionType.Button, "<Mouse>/rightButton");
        BreakAction = new InputAction("BreakBlock", InputActionType.Button, "<Mouse>/leftButton");
    }

    public void Enable()
    {
        PlaceAction?.Enable();
        BreakAction?.Enable();
    }

    public void Disable()
    {
        PlaceAction?.Disable();
        BreakAction?.Disable();
    }
}
