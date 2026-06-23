/// <summary>
/// Client → server intent. Never mutates world state directly.
/// </summary>
public interface IWorldCommandSender
{
    WorldCommandResult Send(WorldCommand command);
}
