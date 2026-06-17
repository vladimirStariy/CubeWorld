using UnityEngine.InputSystem;

public sealed class CreativeInventoryInputBindings
{
    private readonly InputAction[] hotbarActions = new InputAction[CreativeInventory.HotbarSize];

    public InputAction ToggleCreativeAction { get; private set; }
    public InputAction ScrollAction { get; private set; }

    public void Build()
    {
        ToggleCreativeAction = new InputAction("ToggleCreativeInventory", InputActionType.Button, "<Keyboard>/e");
        ScrollAction = new InputAction("HotbarScroll", InputActionType.Value, "<Mouse>/scroll");

        for (int i = 0; i < hotbarActions.Length; i++)
        {
            var key = i == 8 ? "<Keyboard>/9" : $"<Keyboard>/{i + 1}";
            hotbarActions[i] = new InputAction($"HotbarSlot{i + 1}", InputActionType.Button, key);
        }
    }

    public void Enable()
    {
        ToggleCreativeAction?.Enable();
        ScrollAction?.Enable();
        for (int i = 0; i < hotbarActions.Length; i++)
        {
            hotbarActions[i]?.Enable();
        }
    }

    public void Disable()
    {
        ToggleCreativeAction?.Disable();
        ScrollAction?.Disable();
        for (int i = 0; i < hotbarActions.Length; i++)
        {
            hotbarActions[i]?.Disable();
        }
    }

    public bool WasHotbarSlotPressed(int index)
    {
        return index >= 0 && index < hotbarActions.Length && hotbarActions[index].WasPressedThisFrame();
    }
}
