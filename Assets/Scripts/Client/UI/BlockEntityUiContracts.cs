using UnityEngine;

public readonly struct BlockEntityUiActionDef
{
    public readonly string Id;
    public readonly string Label;

    public BlockEntityUiActionDef(string id, string label)
    {
        Id = id;
        Label = label;
    }
}

public sealed class BlockEntityUiState
{
    public string Title;
    public string Body;
    public string Status;
    public BlockEntityUiActionDef[] Actions;
}

public interface IBlockEntityUiProvider
{
    bool CanOpen(Vector3Int blockPosition, BlockWorldServer server);
    bool TryBuildState(Vector3Int blockPosition, BlockWorldServer server, string lastStatus, out BlockEntityUiState state);
    bool TryHandleAction(Vector3Int blockPosition, BlockWorldServer server, string actionId, out string statusMessage);
}
