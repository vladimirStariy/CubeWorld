using UnityEngine.InputSystem;

public static class InputModifiers
{
    public static bool IsShiftHeld()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }
}
