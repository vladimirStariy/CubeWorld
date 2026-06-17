using UnityEngine;

public enum CampfireInteraction
{
    AddInput,
    AddFuel,
    TakeOutput
}

public readonly struct CampfireState
{
    public readonly int InputCount;
    public readonly int FuelCount;
    public readonly int OutputCount;
    public readonly float BurnTimeRemaining;
    public readonly float CookProgress;
    public readonly bool IsLit;

    public CampfireState(
        int inputCount,
        int fuelCount,
        int outputCount,
        float burnTimeRemaining,
        float cookProgress)
    {
        InputCount = inputCount;
        FuelCount = fuelCount;
        OutputCount = outputCount;
        BurnTimeRemaining = burnTimeRemaining;
        CookProgress = cookProgress;
        IsLit = burnTimeRemaining > 0f;
    }
}

public sealed class CampfireBlockEntity
{
    private const int MaxStack = 64;
    private const float FuelBurnDurationSeconds = 8f;
    private const float CookDurationSeconds = 4f;

    private int inputCount;
    private int fuelCount;
    private int outputCount;
    private float burnTimeRemaining;
    private float cookProgress;

    public CampfireState Snapshot()
    {
        return new CampfireState(inputCount, fuelCount, outputCount, burnTimeRemaining, cookProgress);
    }

    public void StartLit(float burnDurationSeconds)
    {
        burnTimeRemaining = Mathf.Max(burnTimeRemaining, burnDurationSeconds);
    }

    public bool TryInteract(CampfireInteraction interaction, out string message)
    {
        switch (interaction)
        {
            case CampfireInteraction.AddInput:
                if (inputCount >= MaxStack)
                {
                    message = "Input is full.";
                    return false;
                }

                inputCount++;
                message = "Added raw item.";
                return true;

            case CampfireInteraction.AddFuel:
                if (fuelCount >= MaxStack)
                {
                    message = "Fuel is full.";
                    return false;
                }

                fuelCount++;
                message = "Added fuel.";
                return true;

            case CampfireInteraction.TakeOutput:
                if (outputCount <= 0)
                {
                    message = "No cooked output.";
                    return false;
                }

                outputCount--;
                message = "Took cooked item.";
                return true;

            default:
                message = "Unsupported interaction.";
                return false;
        }
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (burnTimeRemaining > 0f)
        {
            burnTimeRemaining = Mathf.Max(0f, burnTimeRemaining - deltaTime);
        }

        if (burnTimeRemaining <= 0f && fuelCount > 0 && inputCount > 0)
        {
            fuelCount--;
            burnTimeRemaining = FuelBurnDurationSeconds;
        }

        if (burnTimeRemaining <= 0f || inputCount <= 0)
        {
            cookProgress = 0f;
            return;
        }

        if (outputCount >= MaxStack)
        {
            return;
        }

        cookProgress += deltaTime;
        while (cookProgress >= CookDurationSeconds && inputCount > 0 && outputCount < MaxStack)
        {
            cookProgress -= CookDurationSeconds;
            inputCount--;
            outputCount++;
        }
    }
}
