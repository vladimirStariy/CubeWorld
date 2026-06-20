using UnityEngine;

public enum WorldCommandKind
{
    PlaceBlock,
    BreakBlock,
    PlaceGroundItem,
    PickupGroundItem,
    UseItemOnAssembly,
    BreakCampfireAssembly,
    PlaceClayWorksite,
    ChiselBegin,
    ChiselRemove,
    ChiselAdd
}

public readonly struct WorldCommand
{
    public WorldCommandKind Kind { get; }
    public Vector3Int BlockPosition { get; }
    public Vector3 FaceNormal { get; }
    public Vector3 WorldHitPoint { get; }
    public Vector3Int TargetBlockPosition { get; }
    public int PickupAmount { get; }
    public Vector3 ChiselLocalPoint { get; }

    private WorldCommand(
        WorldCommandKind kind,
        Vector3Int blockPosition,
        Vector3 faceNormal,
        Vector3 worldHitPoint,
        Vector3Int targetBlockPosition,
        int pickupAmount,
        Vector3 chiselLocalPoint)
    {
        Kind = kind;
        BlockPosition = blockPosition;
        FaceNormal = faceNormal;
        WorldHitPoint = worldHitPoint;
        TargetBlockPosition = targetBlockPosition;
        PickupAmount = pickupAmount;
        ChiselLocalPoint = chiselLocalPoint;
    }

    public static WorldCommand PlaceBlock(Vector3Int targetBlockPosition) =>
        new(WorldCommandKind.PlaceBlock, default, default, default, targetBlockPosition, 0, default);

    public static WorldCommand BreakBlock(Vector3Int blockPosition) =>
        new(WorldCommandKind.BreakBlock, blockPosition, default, default, default, 0, default);

    public static WorldCommand PlaceGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint) =>
        new(WorldCommandKind.PlaceGroundItem, hitBlock, faceNormal, worldHitPoint, default, 0, default);

    public static WorldCommand PickupGroundItem(Vector3Int hitBlock, Vector3 faceNormal, Vector3 worldHitPoint, int amount) =>
        new(WorldCommandKind.PickupGroundItem, hitBlock, faceNormal, worldHitPoint, default, amount, default);

    public static WorldCommand UseItemOnAssembly(Vector3Int hitBlock, Vector3 faceNormal) =>
        new(WorldCommandKind.UseItemOnAssembly, hitBlock, faceNormal, default, default, 0, default);

    public static WorldCommand BreakCampfireAssembly(Vector3Int hitBlock, Vector3 faceNormal) =>
        new(WorldCommandKind.BreakCampfireAssembly, hitBlock, faceNormal, default, default, 0, default);

    public static WorldCommand PlaceClayWorksite(Vector3Int anchorBlock, Vector3Int faceNormal) =>
        new(WorldCommandKind.PlaceClayWorksite, anchorBlock, faceNormal, default, default, 0, default);

    public static WorldCommand ChiselBegin(Vector3Int blockPosition) =>
        new(WorldCommandKind.ChiselBegin, blockPosition, default, default, default, 0, default);

    public static WorldCommand ChiselRemove(Vector3Int blockPosition, Vector3 localPoint) =>
        new(WorldCommandKind.ChiselRemove, blockPosition, default, default, default, 0, localPoint);

    public static WorldCommand ChiselAdd(Vector3Int blockPosition, Vector3 localPoint) =>
        new(WorldCommandKind.ChiselAdd, blockPosition, default, default, default, 0, localPoint);
}

public readonly struct WorldCommandResult
{
    public bool Success { get; }
    public string Message { get; }
    public ClayWorksiteKey ClayWorksiteKey { get; }
    public bool HasClayWorksiteKey { get; }

    public WorldCommandResult(bool success, string message, ClayWorksiteKey clayWorksiteKey = default, bool hasClayWorksiteKey = false)
    {
        Success = success;
        Message = message;
        ClayWorksiteKey = clayWorksiteKey;
        HasClayWorksiteKey = hasClayWorksiteKey;
    }

    public static WorldCommandResult Ok(string message = null) => new(true, message);

    public static WorldCommandResult Fail(string message) => new(false, message);

    public static WorldCommandResult ClayWorksitePlaced(ClayWorksiteKey key, string message) =>
        new(true, message, key, true);
}
