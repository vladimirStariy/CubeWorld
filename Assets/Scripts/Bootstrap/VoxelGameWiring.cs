using UnityEngine;

public sealed class VoxelGameWiring
{
    public LocalIntegratedGameSession Session { get; private set; }
    public BlockWorldServer WorldServer => Session?.Server;
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

        Session = references.Session ?? CreateSession(references.WorldServer, parent);
        Session.StartWorld();

        PlayerController = Require(references.PlayerController, () => VoxelGameObjectFactory.CreatePlayer(parent));
        PlayerController.transform.position = Session.GetSpawnPosition();
        PlayerCamera = references.PlayerCamera != null
            ? references.PlayerCamera
            : PlayerController.GetComponentInChildren<Camera>();

        CreativeInventory = Require(references.CreativeInventory, () => VoxelGameObjectFactory.CreateCreativeInventory(parent));
        CreativeInventory.Bind(Session.Connection.PlayerInventory);
        CommandConsole = Require(references.CommandConsole, () => VoxelGameObjectFactory.CreateCommandConsole(parent));
        CreativeInventoryUi = Require(references.CreativeInventoryUi, () => VoxelGameObjectFactory.CreateCreativeInventoryUi(parent));
        ClayFormingController = Require(references.ClayFormingController, () => VoxelGameObjectFactory.CreateClayFormingController(parent));
        WorldClient = Require(references.WorldClient, () => VoxelGameObjectFactory.CreateWorldClient(parent));

        var chunksRoot = new GameObject("Chunks").transform;
        chunksRoot.SetParent(WorldClient.transform, false);

        CrosshairUi = Require(references.CrosshairUi, () => VoxelGameObjectFactory.CreateCrosshair(parent));
        DebugOverlay = Require(references.DebugOverlay, () => VoxelGameObjectFactory.CreateDebugOverlay(parent));
        BlockEntityUi = Require(references.BlockEntityUi, () => VoxelGameObjectFactory.CreateBlockEntityUi(parent));

        var characterController = PlayerController.GetComponent<CharacterController>();
        var blockMaterial = Session.Connection.BuildClientBlockMaterial(out var blockAtlas);
        var fluidMaterial = Session.Connection.BuildClientFluidMaterial(out _);

        var blockEntityUiRegistry = new BlockEntityUiRegistry();
        blockEntityUiRegistry.Register(new CampfireBlockEntityUiProvider());
        BlockEntityUi.Configure(HudRoot.Canvas, Session.Connection.Authority, PlayerController, blockEntityUiRegistry);
        CreativeInventoryUi.Configure(HudRoot.Canvas, CreativeInventory, PlayerController, CommandConsole, blockAtlas);
        ClayFormingController.Configure(Session.Connection, PlayerCamera, CreativeInventory, CreativeInventoryUi, PlayerController, HudRoot.Canvas);
        WorldClient.Configure(Session.Connection, chunksRoot, PlayerCamera, characterController, CreativeInventory, BlockEntityUi, CreativeInventoryUi, ClayFormingController, blockMaterial, null, fluidMaterial);
        CommandConsole.Configure(HudRoot.Canvas, PlayerController, CreativeInventory, Session.Connection, CreativeInventoryUi);
        CrosshairUi.Configure(HudRoot.Canvas);
        DebugOverlay.Configure(HudRoot.Canvas, PlayerController, WorldClient);
        SessionDebugLog.WriteIntegratedSessionReady(Session, WorldClient);
    }

    private static LocalIntegratedGameSession CreateSession(BlockWorldServer existingServer, Transform parent)
    {
        if (existingServer != null)
        {
            return new LocalIntegratedGameSession(existingServer);
        }

        return LocalIntegratedGameSession.Create(parent);
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
    public LocalIntegratedGameSession Session;
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
