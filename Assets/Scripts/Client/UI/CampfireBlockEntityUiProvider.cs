using UnityEngine;

public sealed class CampfireBlockEntityUiProvider : IBlockEntityUiProvider
{
    private static readonly BlockEntityUiActionDef[] Actions =
    {
        new("add_input", "Add Input"),
        new("add_fuel", "Add Fuel"),
        new("take_output", "Take Output")
    };

    public bool CanOpen(Vector3Int blockPosition, IWorldAuthority authority)
    {
        return authority != null && authority.TryGetCampfireState(blockPosition, out _);
    }

    public bool TryBuildState(Vector3Int blockPosition, IWorldAuthority authority, string lastStatus, out BlockEntityUiState state)
    {
        state = null;
        if (authority == null || !authority.TryGetCampfireState(blockPosition, out var campfire))
        {
            return false;
        }

        state = new BlockEntityUiState
        {
            Title = "Campfire",
            Body =
                $"Input:  {campfire.InputCount}\n" +
                $"Fuel:   {campfire.FuelCount}\n" +
                $"Output: {campfire.OutputCount}\n" +
                $"Lit:    {(campfire.IsLit ? "Yes" : "No")}\n" +
                $"Burn:   {campfire.BurnTimeRemaining:0.0}s\n" +
                $"Cook:   {campfire.CookProgress:0.0}s / 4.0s",
            Status = string.IsNullOrWhiteSpace(lastStatus) ? "Campfire opened." : lastStatus,
            Actions = Actions
        };
        return true;
    }

    public bool TryHandleAction(Vector3Int blockPosition, IWorldAuthority authority, string actionId, out string statusMessage)
    {
        statusMessage = "Unknown action.";
        if (authority == null)
        {
            return false;
        }

        CampfireInteraction interaction;
        switch (actionId)
        {
            case "add_input":
                interaction = CampfireInteraction.AddInput;
                break;
            case "add_fuel":
                interaction = CampfireInteraction.AddFuel;
                break;
            case "take_output":
                interaction = CampfireInteraction.TakeOutput;
                break;
            default:
                return false;
        }

        authority.TryInteractCampfire(blockPosition, interaction, out _, out statusMessage);
        return true;
    }
}
