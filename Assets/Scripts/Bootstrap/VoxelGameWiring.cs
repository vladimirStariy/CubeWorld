using UnityEngine;

public sealed class VoxelGameWiring
{
    public BlockWorldServer WorldServer { get; private set; }
    public BlockWorldClient WorldClient { get; private set; }
    public FirstPersonCharacterController PlayerController { get; private set; }
    public CreativeInventory CreativeInventory { get; private set; }
    public CreativeInventoryUI CreativeInventoryUi { get; private set; }
    public GameCommandConsole CommandConsole { get; private set; }
    public CrosshairUI CrosshairUi { get; private set; }
    public PlayerDebugOverlay DebugOverlay { get; private set; }
    public BlockEntityUiController BlockEntityUi { get; private set; }
    public ClayFormingController ClayFormingController { get; private set; }
    public GameHudRoot HudRoot { get; private set; }
    public Camera PlayerCamera { get; private set; }

    public void Wire(VoxelGameReferences references)
    {
        var parent = references.BootstrapTransform;

        HudRoot = references.HudRoot != null
            ? references.HudRoot
            : VoxelGameObjectFactory.CreateHudRoot(parent);

        WorldServer = Require(references.WorldServer, () => VoxelGameObjectFactory.CreateWorldServer(parent));
        PlayerController = Require(references.PlayerController, () => VoxelGameObjectFactory.CreatePlayer(parent));
        PlayerCamera = references.PlayerCamera != null
            ? references.PlayerCamera
            : PlayerController.GetComponentInChildren<Camera>();

        CreativeInventory = Require(references.CreativeInventory, () => VoxelGameObjectFactory.CreateCreativeInventory(parent));
        CommandConsole = Require(references.CommandConsole, () => VoxelGameObjectFactory.CreateCommandConsole(parent));
        CreativeInventoryUi = Require(references.CreativeInventoryUi, () => VoxelGameObjectFactory.CreateCreativeInventoryUi(parent));
        ClayFormingController = Require(references.ClayFormingController, () => VoxelGameObjectFactory.CreateClayFormingController(parent));
        WorldClient = Require(references.WorldClient, () => VoxelGameObjectFactory.CreateWorldClient(parent));
        CrosshairUi = Require(references.CrosshairUi, () => VoxelGameObjectFactory.CreateCrosshair(parent));
        DebugOverlay = Require(references.DebugOverlay, () => VoxelGameObjectFactory.CreateDebugOverlay(parent));
        BlockEntityUi = Require(references.BlockEntityUi, () => VoxelGameObjectFactory.CreateBlockEntityUi(parent));

        var characterController = PlayerController.GetComponent<CharacterController>();
        var blockAtlas = WorldServer.BlockAtlasTexture;

        var blockEntityUiRegistry = new BlockEntityUiRegistry();
        blockEntityUiRegistry.Register(new CampfireBlockEntityUiProvider());
        BlockEntityUi.Configure(HudRoot.Canvas, WorldServer, PlayerController, blockEntityUiRegistry);
        CreativeInventoryUi.Configure(HudRoot.Canvas, CreativeInventory, PlayerController, CommandConsole, blockAtlas);
        ClayFormingController.Configure(WorldServer, PlayerCamera, CreativeInventory, CreativeInventoryUi, PlayerController, HudRoot.Canvas);
        WorldClient.Configure(WorldServer, PlayerCamera, characterController, CreativeInventory, BlockEntityUi, CreativeInventoryUi, ClayFormingController);
        CommandConsole.Configure(HudRoot.Canvas, PlayerController, CreativeInventory, WorldServer, CreativeInventoryUi);
        CrosshairUi.Configure(HudRoot.Canvas);
        DebugOverlay.Configure(HudRoot.Canvas, PlayerController, WorldClient);
    }

    private static T Require<T>(T existing, System.Func<T> create) where T : Object
    {
        return existing != null ? existing : create();
    }
}

public sealed class VoxelGameReferences
{
    public Transform BootstrapTransform;
    public GameHudRoot HudRoot;
    public BlockWorldServer WorldServer;
    public BlockWorldClient WorldClient;
    public FirstPersonCharacterController PlayerController;
    public CreativeInventory CreativeInventory;
    public CreativeInventoryUI CreativeInventoryUi;
    public GameCommandConsole CommandConsole;
    public CrosshairUI CrosshairUi;
    public PlayerDebugOverlay DebugOverlay;
    public BlockEntityUiController BlockEntityUi;
    public ClayFormingController ClayFormingController;
    public Camera PlayerCamera;
}
