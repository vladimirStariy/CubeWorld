using UnityEngine;

public sealed class BlockWorldClient : MonoBehaviour
{
    [SerializeField] private BlockWorldServer server;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 8f;
    [SerializeField] private BlockSelectionOutline selectionOutline;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private CreativeInventory creativeInventory;
    [SerializeField] private BlockEntityUiController blockEntityUi;

    private readonly BlockWorldInputBindings input = new();
    private BlockWorldInteractor interactor;
    private BlockSelectionOutlineController outlineController;
    private CampfireAssemblyWorldVisualizer assemblyVisualizer;
    private bool configured;

    public void Configure(
        BlockWorldServer worldServer,
        Camera camera,
        CharacterController characterController,
        CreativeInventory inventory,
        BlockEntityUiController blockEntityUiController,
        BlockSelectionOutline outline = null)
    {
        if (configured)
        {
            return;
        }

        server = worldServer;
        playerCamera = camera;
        playerCharacterController = characterController;
        creativeInventory = inventory;
        blockEntityUi = blockEntityUiController;
        selectionOutline = outline;

        outlineController = new BlockSelectionOutlineController(server, playerCamera, interactDistance);
        selectionOutline = outlineController.EnsureOutline(transform);
        outlineController.AttachOutline(selectionOutline);

        assemblyVisualizer = gameObject.AddComponent<CampfireAssemblyWorldVisualizer>();
        assemblyVisualizer.Configure(server);

        void OnWorldChanged()
        {
            outlineController.InvalidateCache();
            assemblyVisualizer.Sync();
        }

        interactor = new BlockWorldInteractor(
            server,
            playerCamera,
            playerCharacterController,
            creativeInventory,
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
        if (!configured || server == null || playerCamera == null)
        {
            return;
        }

        if (blockEntityUi != null && blockEntityUi.IsOpen)
        {
            outlineController.Update();
            return;
        }

        if (input.PlaceAction.WasPressedThisFrame())
        {
            interactor.TryUseSelectedItem();
        }

        if (input.BreakAction.WasPressedThisFrame())
        {
            interactor.TryUseOrBreakBlock();
        }

        if (input.ChiselAction.WasPressedThisFrame())
        {
            interactor.TryChiselBlock();
        }

        outlineController.Update();
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
}
