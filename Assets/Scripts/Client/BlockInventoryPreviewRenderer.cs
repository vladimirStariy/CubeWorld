using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

public sealed class BlockInventoryPreviewRenderer : MonoBehaviour
{
    private const float OrthographicSize = 0.74f;
    private const float CameraDistance = 3.2f;

    private readonly List<BlockItemSlotPreview> previews = new();

    private Transform stageRoot;
    private Transform cubePivot;
    private Transform cubeOriginPivot;
    private Transform cubeTransform;
    private MeshFilter cubeFilter;
    private MeshRenderer cubeRenderer;
    private Camera previewCamera;
    private Material previewMaterial;
    private Material itemPreviewMaterial;
    private Texture2D blockAtlasTexture;

    public void Configure(Texture2D atlas)
    {
        blockAtlasTexture = atlas;
        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
            previewMaterial = null;
        }

        if (itemPreviewMaterial != null)
        {
            Destroy(itemPreviewMaterial);
            itemPreviewMaterial = null;
        }
    }

    public void Register(BlockItemSlotPreview preview)
    {
        if (preview != null && !previews.Contains(preview))
        {
            previews.Add(preview);
        }
    }

    public void RegisterAll(IEnumerable<BlockItemSlotPreview> slotPreviews)
    {
        foreach (var preview in slotPreviews)
        {
            Register(preview);
        }
    }

    private void Awake()
    {
        BuildStage();
    }

    private void OnDestroy()
    {
        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
            previewMaterial = null;
        }

        if (itemPreviewMaterial != null)
        {
            Destroy(itemPreviewMaterial);
            itemPreviewMaterial = null;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RenderAtEndOfFrame());
    }

    private IEnumerator RenderAtEndOfFrame()
    {
        var wait = new WaitForEndOfFrame();
        while (enabled)
        {
            yield return wait;
            RenderAllPreviews();
        }
    }

    private void RenderAllPreviews()
    {
        using (RuntimeFrameProfiler.Begin("ui.hotbarPreviews"))
        {
            for (int i = 0; i < previews.Count; i++)
            {
                var preview = previews[i];
                if (preview == null || !preview.isActiveAndEnabled || !preview.ShouldRender())
                {
                    continue;
                }

                RenderPreview(preview);
                preview.MarkRendered();
            }
        }
    }

    private void BuildStage()
    {
        stageRoot = new GameObject("PreviewStage").transform;
        stageRoot.SetParent(transform, false);
        stageRoot.localPosition = Vector3.zero;

        cubePivot = new GameObject("Pivot").transform;
        cubePivot.SetParent(stageRoot, false);

        cubeOriginPivot = new GameObject("Origin").transform;
        cubeOriginPivot.SetParent(cubePivot, false);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "PreviewCube";
        cube.transform.SetParent(cubeOriginPivot, false);
        cubeTransform = cube.transform;
        cubeTransform.localScale = Vector3.one * BlockItemGuiTransform.GuiScale;
        Destroy(cube.GetComponent<Collider>());
        cubeFilter = cube.GetComponent<MeshFilter>();
        cubeRenderer = cube.GetComponent<MeshRenderer>();
        cubeRenderer.enabled = false;
        cubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cubeRenderer.receiveShadows = false;

        var lightObject = new GameObject("PreviewLight");
        lightObject.transform.SetParent(stageRoot, false);
        lightObject.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;

        var cameraObject = new GameObject("PreviewCamera");
        cameraObject.transform.SetParent(stageRoot, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = OrthographicSize;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 8f;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.enabled = false;
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
        cameraObject.transform.localRotation = Quaternion.identity;

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        var urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderShadows = false;
        urpData.renderPostProcessing = false;
#endif

        stageRoot.position = new Vector3(0f, -500f, 0f);
    }

    private void RenderPreview(BlockItemSlotPreview preview)
    {
        if (preview.PreviewItemKind.HasValue)
        {
            RenderItemPreview(preview, preview.PreviewItemKind.Value);
            return;
        }

        if (!preview.BlockType.HasValue)
        {
            return;
        }

        var blockType = preview.BlockType.Value;
        var material = GetPreviewMaterial();
        if (material == null)
        {
            return;
        }

        cubePivot.localRotation = BlockItemGuiTransform.GetRotation(preview.SpinAngle);
        cubeFilter.sharedMesh = BlockPreviewMeshBuilder.GetMesh(blockType);
        cubeRenderer.sharedMaterial = material;

        cubePivot.localPosition = Vector3.zero;
        cubeOriginPivot.localPosition = Vector3.zero;
        cubeOriginPivot.localRotation = Quaternion.identity;
        cubeTransform.localScale = Vector3.one * ItemDisplayTransform.BlockGuiDefault.Scale;
        cubeTransform.localPosition = Vector3.zero;
        cubeTransform.localRotation = Quaternion.identity;

        RenderToPreviewTexture(preview);
    }

    private void RenderItemPreview(BlockItemSlotPreview preview, ItemKind itemKind)
    {
        Mesh mesh;
        Material material;
        if (ItemRegistry.Active != null
            && ItemRegistry.Active.TryGet(itemKind, VoxelBlockType.Air, out var definition)
            && !string.IsNullOrEmpty(definition.ShapeId.Name)
            && ItemShapeMeshBuilder.TryGetRenderData(definition.ShapeId, int.MaxValue, itemKind, out mesh, out material)
            && mesh != null
            && material != null)
        {
            cubeFilter.sharedMesh = ItemShapeMeshBuilder.GetDisplayMesh(mesh);
            cubeRenderer.sharedMaterial = material;
            RenderItemMeshPreview(preview, itemKind);
            return;
        }

        mesh = ItemPreviewMeshBuilder.GetMesh(itemKind);
        material = GetItemPreviewMaterial(itemKind);
        if (mesh == null || material == null)
        {
            return;
        }

        cubeFilter.sharedMesh = ItemShapeMeshBuilder.GetDisplayMesh(mesh);
        cubeRenderer.sharedMaterial = material;
        RenderItemMeshPreview(preview, itemKind);
    }

    private void RenderItemMeshPreview(BlockItemSlotPreview preview, ItemKind itemKind)
    {
        var item = new HotbarItem(itemKind);
        var displayTransform = ItemDisplayTransforms.ResolveGui(item);
        displayTransform.ApplyToWrapper(cubePivot, cubeOriginPivot, cubeTransform, preview.SpinAngle);

        RenderToPreviewTexture(preview);
    }

    private void RenderToPreviewTexture(BlockItemSlotPreview preview)
    {
        var previousTarget = previewCamera.targetTexture;
        try
        {
            cubeRenderer.enabled = true;
            previewCamera.targetTexture = preview.TargetTexture;
            previewCamera.Render();
        }
        finally
        {
            cubeRenderer.enabled = false;
            previewCamera.targetTexture = previousTarget;
        }
    }

    private Material GetPreviewMaterial()
    {
        if (previewMaterial != null)
        {
            return previewMaterial;
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            return null;
        }

        if (blockAtlasTexture == null)
        {
            return null;
        }

        previewMaterial = new Material(shader);
        blockAtlasTexture.wrapMode = TextureWrapMode.Clamp;
        blockAtlasTexture.filterMode = FilterMode.Point;
        previewMaterial.SetTexture("_BaseMap", blockAtlasTexture);
        previewMaterial.SetTexture("_MainTex", blockAtlasTexture);
        BlockTextureLibrary.ApplyFullAtlasToMaterial(previewMaterial);
        previewMaterial.SetColor("_BaseColor", Color.white);
        return previewMaterial;
    }

    private Material GetItemPreviewMaterial(ItemKind itemKind)
    {
        if (itemPreviewMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return null;
            }

            itemPreviewMaterial = new Material(shader);
        }

        var color = ItemPreviewMeshBuilder.GetPreviewColor(itemKind);
        itemPreviewMaterial.SetColor("_BaseColor", color);
        itemPreviewMaterial.SetColor("_Color", color);
        return itemPreviewMaterial;
    }
}
