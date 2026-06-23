using UnityEngine;

public static class WorldCommandNetworking
{
    public static WorldCommandMessage ToMessage(WorldCommand command)
    {
        return new WorldCommandMessage
        {
            Kind = (int)command.Kind,
            BlockX = command.BlockPosition.x,
            BlockY = command.BlockPosition.y,
            BlockZ = command.BlockPosition.z,
            FaceX = command.FaceNormal.x,
            FaceY = command.FaceNormal.y,
            FaceZ = command.FaceNormal.z,
            HitX = command.WorldHitPoint.x,
            HitY = command.WorldHitPoint.y,
            HitZ = command.WorldHitPoint.z,
            TargetX = command.TargetBlockPosition.x,
            TargetY = command.TargetBlockPosition.y,
            TargetZ = command.TargetBlockPosition.z,
            PickupAmount = command.PickupAmount,
            ChiselX = command.ChiselLocalPoint.x,
            ChiselY = command.ChiselLocalPoint.y,
            ChiselZ = command.ChiselLocalPoint.z,
            RecipeId = command.RecipeId ?? string.Empty,
            ClayU = command.ClayCellU,
            ClayV = command.ClayCellV,
            ToolMode = command.ClayToolModeValue
        };
    }

    public static WorldCommandResultMessage ToMessage(WorldCommandResult result)
    {
        return new WorldCommandResultMessage
        {
            Success = result.Success,
            Message = result.Message ?? string.Empty,
            HasClayWorksiteKey = result.HasClayWorksiteKey,
            ClayAnchorX = result.ClayWorksiteKey.AnchorBlock.x,
            ClayAnchorY = result.ClayWorksiteKey.AnchorBlock.y,
            ClayAnchorZ = result.ClayWorksiteKey.AnchorBlock.z,
            ClayFaceX = result.ClayWorksiteKey.FaceNormal.x,
            ClayFaceY = result.ClayWorksiteKey.FaceNormal.y,
            ClayFaceZ = result.ClayWorksiteKey.FaceNormal.z,
            HasClayEditResult = result.HasClayEditResult,
            ClayEditChanged = result.ClayEditResult.Changed,
            ClayLayerCompleted = result.ClayEditResult.LayerCompleted,
            ClayRecipeCompleted = result.ClayEditResult.RecipeCompleted
        };
    }

    public static bool IsHighFrequency(WorldCommand command)
    {
        return command.Kind is WorldCommandKind.ClayFormingAdd or WorldCommandKind.ClayFormingRemove;
    }

    public static string Describe(WorldCommand command)
    {
        return command.Kind switch
        {
            WorldCommandKind.PlaceBlock => $"PlaceBlock @ {command.TargetBlockPosition}",
            WorldCommandKind.BreakBlock => $"BreakBlock @ {command.BlockPosition}",
            WorldCommandKind.PlaceGroundItem => $"PlaceGroundItem @ {command.BlockPosition}",
            WorldCommandKind.PickupGroundItem => $"PickupGroundItem x{command.PickupAmount} @ {command.BlockPosition}",
            WorldCommandKind.UseItemOnAssembly => $"UseItemOnAssembly @ {command.BlockPosition}",
            WorldCommandKind.BreakCampfireAssembly => $"BreakCampfireAssembly @ {command.BlockPosition}",
            WorldCommandKind.PlaceClayWorksite => $"PlaceClayWorksite @ {command.BlockPosition}",
            WorldCommandKind.StartClayForming => $"StartClayForming '{command.RecipeId}' @ {command.BlockPosition}",
            WorldCommandKind.RemoveClayWorksite => $"RemoveClayWorksite @ {command.BlockPosition}",
            WorldCommandKind.ClayFormingAdd => $"ClayFormingAdd ({command.ClayCellU},{command.ClayCellV}) @ {command.BlockPosition}",
            WorldCommandKind.ClayFormingRemove => $"ClayFormingRemove ({command.ClayCellU},{command.ClayCellV}) @ {command.BlockPosition}",
            WorldCommandKind.SetClayFormingToolMode => $"SetClayToolMode {(ClayFormingToolMode)command.ClayToolModeValue} @ {command.BlockPosition}",
            WorldCommandKind.ChiselBegin => $"ChiselBegin @ {command.BlockPosition}",
            WorldCommandKind.ChiselRemove => $"ChiselRemove @ {command.BlockPosition}",
            WorldCommandKind.ChiselAdd => $"ChiselAdd @ {command.BlockPosition}",
            _ => command.Kind.ToString()
        };
    }
}
