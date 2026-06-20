public interface IWorldAuthority
{
    PlayerInventoryState PlayerInventory { get; }

    ContentCatalog ContentCatalog { get; }

    WorldCommandResult ExecuteCommand(WorldCommand command);
}
