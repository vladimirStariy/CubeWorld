using UnityEngine;

public static class VoxelGameObjectFactory
{
    public static BlockWorldServer CreateWorldServer(Transform parent)
    {
        var serverObject = new GameObject("BlockWorldServer");
        serverObject.transform.SetParent(parent, false);
        return serverObject.AddComponent<BlockWorldServer>();
    }

    public static FirstPersonCharacterController CreatePlayer(Transform parent)
    {
        var playerObject = new GameObject("Player");
        playerObject.transform.SetParent(parent, false);
        playerObject.transform.position = new Vector3(50f, 3f, 50f);

        var controller = playerObject.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.stepOffset = 0.15f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.025f;
        controller.minMoveDistance = 0f;

        var player = playerObject.AddComponent<FirstPersonCharacterController>();

        var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
        {
            playerObject.layer = ignoreRaycastLayer;
        }

        return player;
    }

    public static CreativeInventory CreateCreativeInventory(Transform parent)
    {
        var inventoryObject = new GameObject("CreativeInventory");
        inventoryObject.transform.SetParent(parent, false);
        return inventoryObject.AddComponent<CreativeInventory>();
    }

    public static CreativeInventoryUI CreateCreativeInventoryUi(Transform parent)
    {
        var uiObject = new GameObject("CreativeInventoryUI");
        uiObject.transform.SetParent(parent, false);
        return uiObject.AddComponent<CreativeInventoryUI>();
    }

    public static GameCommandConsole CreateCommandConsole(Transform parent)
    {
        var consoleObject = new GameObject("GameCommandConsole");
        consoleObject.transform.SetParent(parent, false);
        return consoleObject.AddComponent<GameCommandConsole>();
    }

    public static BlockWorldClient CreateWorldClient(Transform parent)
    {
        var clientObject = new GameObject("BlockWorldClient");
        clientObject.transform.SetParent(parent, false);
        return clientObject.AddComponent<BlockWorldClient>();
    }

    public static CrosshairUI CreateCrosshair(Transform parent)
    {
        var crosshairObject = new GameObject("CrosshairUI");
        crosshairObject.transform.SetParent(parent, false);
        return crosshairObject.AddComponent<CrosshairUI>();
    }

    public static PlayerDebugOverlay CreateDebugOverlay(Transform parent)
    {
        var debugObject = new GameObject("PlayerDebugOverlay");
        debugObject.transform.SetParent(parent, false);
        return debugObject.AddComponent<PlayerDebugOverlay>();
    }

    public static BlockEntityUiController CreateBlockEntityUi(Transform parent)
    {
        var uiObject = new GameObject("BlockEntityUI");
        uiObject.transform.SetParent(parent, false);
        return uiObject.AddComponent<BlockEntityUiController>();
    }

    public static GameHudRoot CreateHudRoot(Transform parent)
    {
        var hudObject = new GameObject("HUD");
        hudObject.transform.SetParent(parent, false);
        return hudObject.AddComponent<GameHudRoot>();
    }
}
