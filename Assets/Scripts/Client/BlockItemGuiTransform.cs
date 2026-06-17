using UnityEngine;

/// <summary>
/// Minecraft display.gui pose for block items (rotation + scale from vanilla block models).
/// </summary>
public static class BlockItemGuiTransform
{
    public const float GuiRotationX = -30f;
    public const float GuiRotationY = 45f;
    public const float GuiScale = 0.625f;
    public const float SpinSpeed = 22f;

    public static Quaternion GetBaseRotation()
    {
        return Quaternion.Euler(GuiRotationX, GuiRotationY, 0f);
    }

    /// <summary>
    /// Selected-slot spin around the block's local vertical axis (not world Y).
    /// </summary>
    public static Quaternion GetRotation(float additionalYawDegrees)
    {
        var baseRotation = GetBaseRotation();
        if (Mathf.Approximately(additionalYawDegrees, 0f))
        {
            return baseRotation;
        }

        return baseRotation * Quaternion.AngleAxis(additionalYawDegrees, Vector3.up);
    }
}
