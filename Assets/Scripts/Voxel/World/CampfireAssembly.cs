using UnityEngine;

public enum CampfireAssemblyStage : byte
{
    Foundation,
    Building,
    ReadyToLight
}

public readonly struct CampfireAssemblyState
{
    public readonly CampfireAssemblyStage Stage;
    public readonly int StickCount;
    public readonly int RequiredSticks;

    public CampfireAssemblyState(CampfireAssemblyStage stage, int stickCount, int requiredSticks)
    {
        Stage = stage;
        StickCount = stickCount;
        RequiredSticks = requiredSticks;
    }
}

public readonly struct CampfireAssemblySnapshot
{
    public readonly Vector3Int AnchorPosition;
    public readonly CampfireAssemblyState State;

    public CampfireAssemblySnapshot(Vector3Int anchorPosition, CampfireAssemblyState state)
    {
        AnchorPosition = anchorPosition;
        State = state;
    }
}

public sealed class CampfireAssembly
{
    public const int RequiredSticks = 3;

    public Vector3Int AnchorPosition { get; }
    public Vector3Int FoundationPosition { get; }

    public CampfireAssemblyStage Stage { get; private set; } = CampfireAssemblyStage.Foundation;
    public int StickCount { get; private set; }

    public CampfireAssembly(Vector3Int anchorPosition, Vector3Int foundationPosition)
    {
        AnchorPosition = anchorPosition;
        FoundationPosition = foundationPosition;
    }

    public CampfireAssemblyState Snapshot()
    {
        return new CampfireAssemblyState(Stage, StickCount, RequiredSticks);
    }

    public bool TryAddStick(out string message)
    {
        if (Stage == CampfireAssemblyStage.ReadyToLight)
        {
            message = "Campfire structure is complete. Use flint.";
            return false;
        }

        StickCount++;
        Stage = CampfireAssemblyStage.Building;
        if (StickCount >= RequiredSticks)
        {
            Stage = CampfireAssemblyStage.ReadyToLight;
            message = $"Campfire ready ({StickCount}/{RequiredSticks} sticks). Use flint.";
        }
        else
        {
            message = $"Added stick ({StickCount}/{RequiredSticks}).";
        }

        return true;
    }

    public bool CanLight() => Stage == CampfireAssemblyStage.ReadyToLight && StickCount >= RequiredSticks;
}
