using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class VoxelGameBootstrap : MonoBehaviour
{
    [SerializeField] private BlockWorldServer worldServer;
    [SerializeField] private BlockWorldClient worldClient;
    [SerializeField] private FirstPersonCharacterController playerController;
    [SerializeField] private CrosshairUI crosshairUi;
    [SerializeField] private CreativeInventory creativeInventory;
    [SerializeField] private CreativeInventoryUI creativeInventoryUi;
    [SerializeField] private GameCommandConsole gameCommandConsole;
    [SerializeField] private PlayerDebugOverlay playerDebugOverlay;
    [SerializeField] private BlockEntityUiController blockEntityUi;
    [SerializeField] private Camera playerCamera;

    private bool wired;

    private void Start()
    {
        if (wired)
        {
            return;
        }

        var composition = new VoxelGameWiring();
        composition.Wire(new VoxelGameReferences
        {
            BootstrapTransform = transform,
            HudRoot = GetComponentInChildren<GameHudRoot>(true),
            WorldServer = worldServer,
            WorldClient = worldClient,
            PlayerController = playerController,
            CreativeInventory = creativeInventory,
            CreativeInventoryUi = creativeInventoryUi,
            CommandConsole = gameCommandConsole,
            CrosshairUi = crosshairUi,
            DebugOverlay = playerDebugOverlay,
            BlockEntityUi = blockEntityUi,
            PlayerCamera = playerCamera
        });

        wired = true;
    }
}
