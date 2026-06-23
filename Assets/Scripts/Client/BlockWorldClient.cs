using UnityEngine;

public sealed class BlockWorldClient : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 8f;
    [SerializeField] private BlockSelectionOutline selectionOutline;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private CreativeInventory creativeInventory;
    [SerializeField] private CreativeInventoryUI creativeInventoryUi;
    [SerializeField] private ClayFormingController clayFormingController;
    [SerializeField] private BlockEntityUiController blockEntityUi;

    private ILocalIntegratedServerConnection connection;
    private ChunkWorldPresenter worldPresenter;
    private readonly BlockWorldInputBindings input = new();
    private BlockWorldInteractor interactor;
    private BlockSelectionOutlineController outlineController;
    private CampfireAssemblyWorldVisualizer assemblyVisualizer;
    private GroundItemWorldVisualizer groundItemVisualizer;
    private HeldItemView heldItemView;
    private bool configured;

    public IGameServerConnection Connection => connection;

    public void Configure(
        ILocalIntegratedServerConnection serverConnection,
        Transform chunksRoot,
        Camera camera,
        CharacterController characterController,
        CreativeInventory inventory,
        BlockEntityUiController blockEntityUiController,
        CreativeInventoryUI inventoryUi,
        ClayFormingController clayForming,
        Material blockMaterial,
        BlockSelectionOutline outline = null)
    {
        if (configured)
        {
            return;
        }

        connection = serverConnection;
        playerCamera = camera;
        playerCharacterController = characterController;
        creativeInventory = inventory;
        blockEntityUi = blockEntityUiController;
        creativeInventoryUi = inventoryUi;
        clayFormingController = clayForming;
        selectionOutline = outline;

        var blockMaterialToUse = blockMaterial != null
            ? blockMaterial
            : connection.BuildClientBlockMaterial(out _);
        var terrainDrawer = chunksRoot.GetComponent<ChunkTerrainDrawer>();
        if (terrainDrawer == null)
        {
            terrainDrawer = chunksRoot.gameObject.AddComponent<ChunkTerrainDrawer>();
        }

        worldPresenter = new ChunkWorldPresenter(connection.Simulation, terrainDrawer);
        worldPresenter.Configure(connection.ChunkStreaming, blockMaterialToUse);
        connection.PrimeSpawnArea(connection.GetSpawnPosition());
        worldPresenter.FlushPendingMeshes();

        if (playerCharacterController != null)
        {
            playerCharacterController.GetComponent<FirstPersonCharacterController>()
                ?.ConfigureVoxelCollision(connection.Simulation);
        }

        BlockWorldTargeting.ConfigureVoxelWorld(connection.Simulation);

        if (GetComponent<FrameProfilerDriver>() == null)
        {
            gameObject.AddComponent<FrameProfilerDriver>();
        }

        outlineController = new BlockSelectionOutlineController(connection.Presentation, playerCamera, interactDistance);
        selectionOutline = outlineController.EnsureOutline(transform);
        outlineController.AttachOutline(selectionOutline);

        assemblyVisualizer = gameObject.AddComponent<CampfireAssemblyWorldVisualizer>();
        assemblyVisualizer.Configure(connection.Authority);

        groundItemVisualizer = gameObject.AddComponent<GroundItemWorldVisualizer>();
        groundItemVisualizer.Configure(connection.Authority);

        heldItemView = gameObject.AddComponent<HeldItemView>();
        heldItemView.Configure(playerCamera, creativeInventory);

        void OnWorldChanged()
        {
            outlineController.InvalidateCache();
            assemblyVisualizer.Sync();
            groundItemVisualizer.Sync();
        }

        if (clayFormingController != null)
        {
            clayFormingController.WorldPlacementChanged += OnWorldChanged;
        }

        interactor = new BlockWorldInteractor(
            connection,
            playerCamera,
            playerCharacterController,
            interactDistance,
            OnWorldChanged,
            blockEntityUi);

        input.Build();
        input.Enable();
        configured = true;
    }

    private void OnEnable()
    {
        if (configured)
        {
            input.Enable();
        }
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Update()
    {
        if (!configured || connection == null || playerCamera == null)
        {
            return;
        }

        if (playerCharacterController != null && worldPresenter != null)
        {
            using (RuntimeFrameProfiler.Begin("client.streaming"))
            {
                worldPresenter.UpdateStreaming(playerCharacterController.transform.position);
            }
        }

        clayFormingController?.UpdateContinuous();

        if (blockEntityUi != null && blockEntityUi.IsOpen)
        {
            UpdateOutline();
            return;
        }

        if (creativeInventoryUi != null && creativeInventoryUi.IsCreativePanelOpen)
        {
            UpdateOutline();
            return;
        }

        if (clayFormingController != null && clayFormingController.IsMenuBlockingInput)
        {
            UpdateOutline();
            return;
        }

        clayFormingController?.TryHandleShiftPlace();

        var suppressBreak = clayFormingController != null && clayFormingController.ShouldSuppressWorldBreak();
        var suppressPlace = clayFormingController != null && clayFormingController.ShouldSuppressWorldPlace();

        if (!suppressBreak && input.BreakAction.WasPressedThisFrame())
        {
            interactor.TryUseOrBreakBlock();
        }

        if (!suppressPlace && input.PlaceAction.WasPressedThisFrame())
        {
            interactor.TryUseSelectedItem();
        }

        UpdateOutline();
    }

    private void UpdateOutline()
    {
        using (RuntimeFrameProfiler.Begin("client.outline"))
        {
            outlineController.Update();
        }
    }

    public bool TryGetLookTargetInfo(out Vector3Int blockPosition, out Vector3 faceNormal, out BlockQueryResult blockInfo)
    {
        if (!configured || interactor == null)
        {
            blockPosition = default;
            faceNormal = default;
            blockInfo = default;
            return false;
        }

        return interactor.TryGetLookTargetInfo(out blockPosition, out faceNormal, out blockInfo);
    }

    public bool TryGetStreamingDiagnostics(out float estimatedBaseFrameMs, out float streamingBudgetMs)
    {
        if (worldPresenter == null)
        {
            estimatedBaseFrameMs = 0f;
            streamingBudgetMs = 0f;
            return false;
        }

        estimatedBaseFrameMs = worldPresenter.EstimatedBaseFrameMs;
        streamingBudgetMs = worldPresenter.LastStreamingBudgetMs;
        return true;
    }

    public bool TryGetBiomeAt(Vector3Int worldPosition, out BiomeDefinition biome, out ClimateSample climate)
    {
        if (connection == null)
        {
            biome = null;
            climate = default;
            return false;
        }

        return connection.Authority.TryGetBiomeAt(worldPosition, out biome, out climate);
    }
}
