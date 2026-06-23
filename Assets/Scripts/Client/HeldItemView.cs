using UnityEngine;

public sealed class HeldItemView : MonoBehaviour
{
    private Transform handPivot;
    private Transform originPivot;
    private Transform meshRoot;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Camera playerCamera;
    private CreativeInventory inventory;
    private ItemKind? visibleKind;
    private bool configured;

    public void Configure(Camera camera, CreativeInventory creativeInventory)
    {
        if (configured)
        {
            return;
        }

        playerCamera = camera;
        inventory = creativeInventory;

        var root = new GameObject("HeldItemView");
        root.transform.SetParent(camera.transform, false);

        handPivot = new GameObject("HandPivot").transform;
        handPivot.SetParent(root.transform, false);

        originPivot = new GameObject("Origin").transform;
        originPivot.SetParent(handPivot, false);

        var meshObject = new GameObject("Mesh");
        meshObject.transform.SetParent(originPivot, false);
        meshRoot = meshObject.transform;
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        var ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer >= 0)
        {
            root.layer = ignoreLayer;
            handPivot.gameObject.layer = ignoreLayer;
            originPivot.gameObject.layer = ignoreLayer;
            meshObject.layer = ignoreLayer;
        }

        configured = true;
        Hide();
    }

    private void LateUpdate()
    {
        if (!configured || inventory == null)
        {
            return;
        }

        if (!inventory.TryGetSelectedItem(out var item) || !ShouldShow(item))
        {
            Hide();
            return;
        }

        if (visibleKind.HasValue && visibleKind.Value == item.Kind && meshRenderer.enabled)
        {
            return;
        }

        Show(item);
    }

    private static bool ShouldShow(HotbarItem item)
    {
        if (item.IsEmpty || item.Kind == ItemKind.Block)
        {
            return false;
        }

        return ItemDisplayTransforms.TryResolveHand(item, out _);
    }

    private void Show(HotbarItem item)
    {
        if (!ItemDisplayTransforms.TryResolveHand(item, out var handTransform))
        {
            Hide();
            return;
        }

        if (!TryResolveRenderData(item, out var mesh, out var material))
        {
            Hide();
            return;
        }

        meshFilter.sharedMesh = ItemShapeMeshBuilder.GetDisplayMesh(mesh);
        meshRenderer.sharedMaterial = material;
        handTransform.ApplyToWrapper(handPivot, originPivot, meshRoot, spinDegrees: 0f);
        meshRenderer.enabled = true;
        visibleKind = item.Kind;
    }

    private void Hide()
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        visibleKind = null;
    }

    private static bool TryResolveRenderData(HotbarItem item, out Mesh mesh, out Material material)
    {
        mesh = null;
        material = null;

        if (ItemRegistry.Active != null
            && ItemRegistry.Active.TryGet(item, out var definition)
            && !string.IsNullOrEmpty(definition.ShapeId.Name)
            && ItemShapeMeshBuilder.TryGetRenderData(definition.ShapeId, int.MaxValue, item.Kind, out mesh, out material)
            && mesh != null
            && material != null)
        {
            return true;
        }

        mesh = ItemPreviewMeshBuilder.GetMesh(item.Kind);
        material = ItemWorldPlaceholderMaterials.Get(item.Kind);
        return mesh != null && material != null;
    }
}
