using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ItemTransformPreviewMode
{
    Gui,
    FpHand,
    GroundStack
}

public sealed class ItemTransformPreviewBootstrap : MonoBehaviour
{
    private const float GuiOrthographicSize = 0.74f;
    private const float GuiCameraDistance = 3.2f;

    private readonly List<ItemJsonCatalogEntry> catalogEntries = new();

    private Camera previewCamera;
    private Transform previewRoot;
    private Transform handPivot;
    private Transform originPivot;
    private Transform meshRoot;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Light previewLight;

    private ItemJson itemJson;
    private string itemFilePath;
    private int selectedEntryIndex;
    private ItemTransformPreviewMode previewMode = ItemTransformPreviewMode.FpHand;
    private Vector3 translation;
    private Vector3 rotation;
    private Vector3 origin;
    private float scale = 1f;
    private float guiSpinDegrees;
    private int stackCount = 4;
    private bool dirty;
    private string statusMessage = "Select an item and adjust transforms.";
    private Vector2 scrollPosition;
    private bool contentReady;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        LoadContent();
        BuildPreviewStage();
        ReloadCatalog();
        SelectDefaultItem();
    }

    private void Update()
    {
        if (!contentReady || meshRenderer == null)
        {
            return;
        }

        ApplyPreviewTransform();
    }

    private void OnGUI()
    {
        const int panelWidth = 360;
        GUILayout.BeginArea(new Rect(12f, 12f, panelWidth, Screen.height - 24f), GUI.skin.box);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Item Transform Preview", EditorStylesBoldLabel());
        GUILayout.Space(6f);

        DrawItemSelector();
        GUILayout.Space(8f);
        DrawModeSelector();
        GUILayout.Space(8f);
        DrawTransformFields();
        GUILayout.Space(8f);
        DrawActions();
        GUILayout.Space(8f);
        DrawStatus();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        DrawHelpOverlay(panelWidth);
    }

    private void LoadContent()
    {
        ItemShapeMeshBuilder.ClearCache();
        var catalog = new ContentCatalog();
        JsonContentLoader.LoadAllPacks(catalog);
        catalog.BlockTextures.BuildAtlas();
        VanillaContentBootstrap.RegisterAll(catalog);
        contentReady = ItemRegistry.Active != null && ItemShapeRegistry.Active != null;
        if (!contentReady)
        {
            statusMessage = "Failed to load content packs.";
        }
    }

    private void BuildPreviewStage()
    {
        var cameraObject = new GameObject("PreviewCamera");
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.tag = "MainCamera";
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 20f;
        cameraObject.AddComponent<AudioListener>();

        var lightObject = new GameObject("PreviewLight");
        lightObject.transform.SetParent(cameraObject.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.1f;

        previewRoot = new GameObject("PreviewRoot").transform;
        handPivot = new GameObject("HandPivot").transform;
        handPivot.SetParent(previewRoot, false);
        originPivot = new GameObject("Origin").transform;
        originPivot.SetParent(handPivot, false);

        var meshObject = new GameObject("Mesh");
        meshObject.transform.SetParent(originPivot, false);
        meshRoot = meshObject.transform;
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        ApplyPreviewMode();
    }

    private void ReloadCatalog()
    {
        catalogEntries.Clear();
        catalogEntries.AddRange(ItemJsonFile.ScanItemFiles());
        if (catalogEntries.Count == 0)
        {
            statusMessage = "No item JSON files found under StreamingAssets/Content.";
        }
    }

    private void SelectDefaultItem()
    {
        if (catalogEntries.Count == 0)
        {
            return;
        }

        selectedEntryIndex = 0;
        for (int i = 0; i < catalogEntries.Count; i++)
        {
            if (catalogEntries[i].ContentId != null
                && catalogEntries[i].ContentId.IndexOf("stick", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                selectedEntryIndex = i;
                break;
            }
        }

        LoadSelectedItem();
    }

    private void LoadSelectedItem()
    {
        dirty = false;
        if (catalogEntries.Count == 0 || selectedEntryIndex < 0 || selectedEntryIndex >= catalogEntries.Count)
        {
            itemJson = null;
            itemFilePath = null;
            meshRenderer.enabled = false;
            return;
        }

        var entry = catalogEntries[selectedEntryIndex];
        itemFilePath = entry.FilePath;
        if (!ItemJsonFile.TryRead(itemFilePath, out itemJson, out var error))
        {
            statusMessage = error;
            meshRenderer.enabled = false;
            return;
        }

        LoadTransformFromJson();
        RefreshPreviewMesh();
        statusMessage = $"Loaded {entry.Label}";
    }

    private void LoadTransformFromJson()
    {
        if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            return;
        }

        var jsonTransform = GetJsonTransform(previewMode);
        if (jsonTransform == null)
        {
            translation = Vector3.zero;
            rotation = Vector3.zero;
            origin = Vector3.zero;
            scale = previewMode == ItemTransformPreviewMode.Gui ? 1.75f : 0.55f;
            return;
        }

        translation = ReadVector3(jsonTransform.translation);
        rotation = ReadVector3(jsonTransform.rotation);
        origin = ReadVector3(jsonTransform.origin);
        scale = jsonTransform.scale > 0f ? jsonTransform.scale : 1f;
    }

    private void RefreshPreviewMesh()
    {
        meshRenderer.enabled = false;
        if (itemJson == null || !contentReady)
        {
            return;
        }

        if (!ContentJsonParser.TryParseItem(itemJson, out var definition, out var error))
        {
            statusMessage = error;
            return;
        }

        if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            RefreshGroundStackPreview(definition);
            return;
        }

        Mesh mesh;
        Material material;
        if (!string.IsNullOrEmpty(definition.ShapeId.Name)
            && ItemShapeMeshBuilder.TryGetRenderData(definition.ShapeId, int.MaxValue, definition.RuntimeKind, out mesh, out material)
            && mesh != null
            && material != null)
        {
            meshFilter.sharedMesh = ItemShapeMeshBuilder.GetDisplayMesh(mesh);
            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = true;
            return;
        }

        mesh = ItemPreviewMeshBuilder.GetMesh(definition.RuntimeKind);
        material = ItemWorldPlaceholderMaterials.Get(definition.RuntimeKind);
        if (mesh != null && material != null)
        {
            meshFilter.sharedMesh = ItemShapeMeshBuilder.GetDisplayMesh(mesh);
            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = true;
            return;
        }

        statusMessage = "No preview mesh available for this item.";
    }

    private void RefreshGroundStackPreview(ItemDefinition definition)
    {
        if (!definition.GroundProfile.HasValue || !definition.GroundProfile.Value.HasStackingShape)
        {
            statusMessage = "Item has no groundPlacement.stackingShape.";
            return;
        }

        var profile = definition.GroundProfile.Value;
        var quantity = GroundStackMath.GetQuantityElements(stackCount, profile);
        if (quantity <= 0)
        {
            statusMessage = "Stack count must be greater than zero.";
            return;
        }

        if (!ItemShapeRegistry.Active.TryGet(profile.StackingShapeId, out var shapeDefinition)
            || !ItemShapeMeshBuilder.TryGetRenderData(
                profile.StackingShapeId,
                quantity,
                definition.RuntimeKind,
                out var mesh,
                out var material)
            || mesh == null
            || material == null)
        {
            statusMessage = $"Failed to build stacking shape {profile.StackingShapeId}.";
            return;
        }

        meshFilter.sharedMesh = ItemShapeMeshBuilder.GetGroundStackMesh(mesh, shapeDefinition);
        meshRenderer.sharedMaterial = material;
        meshRenderer.enabled = true;

        var modelCount = GroundStackMath.GetItemModelCount(stackCount, profile);
        statusMessage =
            $"Stack {stackCount} -> models {modelCount}, quantityElements {quantity}/{shapeDefinition.PartCount}";
    }

    private void ApplyPreviewMode()
    {
        if (previewCamera == null || previewRoot == null)
        {
            return;
        }

        if (previewMode == ItemTransformPreviewMode.FpHand)
        {
            previewRoot.SetParent(previewCamera.transform, false);
            previewRoot.localPosition = Vector3.zero;
            previewRoot.localRotation = Quaternion.identity;
            previewCamera.orthographic = false;
            previewCamera.fieldOfView = 70f;
            previewCamera.transform.position = new Vector3(0f, 1.6f, 0f);
            previewCamera.transform.rotation = Quaternion.identity;
        }
        else if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            previewRoot.SetParent(null, worldPositionStays: false);
            previewRoot.position = Vector3.zero;
            previewRoot.rotation = Quaternion.identity;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 0.55f;
            var cameraPosition = new Vector3(1.15f, 0.95f, -1.55f);
            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.rotation = Quaternion.LookRotation(-cameraPosition.normalized, Vector3.up);
        }
        else
        {
            previewRoot.SetParent(null, worldPositionStays: false);
            previewRoot.position = Vector3.zero;
            previewRoot.rotation = Quaternion.identity;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = GuiOrthographicSize;
            previewCamera.transform.position = new Vector3(0f, 0f, -GuiCameraDistance);
            previewCamera.transform.rotation = Quaternion.identity;
        }
    }

    private void ApplyPreviewTransform()
    {
        if (!meshRenderer.enabled)
        {
            return;
        }

        if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            handPivot.localPosition = Vector3.zero;
            handPivot.localRotation = Quaternion.identity;
            originPivot.localPosition = Vector3.zero;
            originPivot.localRotation = Quaternion.identity;
            meshRoot.localPosition = Vector3.zero;
            meshRoot.localRotation = Quaternion.identity;
            meshRoot.localScale = Vector3.one;
            return;
        }

        var displayTransform = new ItemDisplayTransform(translation, rotation, origin, scale);
        var spin = previewMode == ItemTransformPreviewMode.Gui ? guiSpinDegrees : 0f;
        displayTransform.ApplyToWrapper(handPivot, originPivot, meshRoot, spin);
    }

    private void DrawItemSelector()
    {
        GUILayout.Label("Item", EditorStylesBoldLabel());
        if (catalogEntries.Count == 0)
        {
            GUILayout.Label("No items found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(28f)))
        {
            selectedEntryIndex = (selectedEntryIndex - 1 + catalogEntries.Count) % catalogEntries.Count;
            LoadSelectedItem();
        }

        GUILayout.Label(catalogEntries[selectedEntryIndex].Label, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(28f)))
        {
            selectedEntryIndex = (selectedEntryIndex + 1) % catalogEntries.Count;
            LoadSelectedItem();
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Reload Item From Disk"))
        {
            LoadSelectedItem();
        }
    }

    private void DrawModeSelector()
    {
        GUILayout.Label("Preview Mode", EditorStylesBoldLabel());
        var modeIndex = GUILayout.Toolbar((int)previewMode, new[] { "GUI", "Hand", "Stack" });
        var newMode = (ItemTransformPreviewMode)modeIndex;
        if (newMode != previewMode)
        {
            previewMode = newMode;
            LoadTransformFromJson();
            ApplyPreviewMode();
            RefreshPreviewMesh();
        }

        if (previewMode == ItemTransformPreviewMode.Gui)
        {
            var spin = guiSpinDegrees;
            if (EditorGUILayoutSlider(ref spin, "Spin Preview", 0f, 360f))
            {
                guiSpinDegrees = spin;
            }
        }
        else if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            var count = (float)stackCount;
            if (EditorGUILayoutSlider(ref count, "Stack Count", 1f, 42f, 1f))
            {
                stackCount = Mathf.RoundToInt(count);
                RefreshPreviewMesh();
            }
        }
    }

    private void DrawTransformFields()
    {
        if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            GUILayout.Label("Ground Stack", EditorStylesBoldLabel());
            GUILayout.Label("Uses groundPlacement.stackingShape with quantityElements tessellation.");
            return;
        }

        GUILayout.Label(GetTransformPropertyName(), EditorStylesBoldLabel());
        DrawVector3Field(ref translation, "Translation", -2f, 2f, 0.01f);
        DrawVector3Field(ref rotation, "Rotation", -180f, 180f, 1f);
        DrawVector3Field(ref origin, "Origin", -1f, 1f, 0.01f);
        if (EditorGUILayoutSlider(ref scale, "Scale", 0.05f, 4f))
        {
            MarkDirty();
        }
    }

    private void DrawActions()
    {
        if (previewMode == ItemTransformPreviewMode.GroundStack)
        {
            if (GUILayout.Button("Reload Shapes From Disk"))
            {
                ItemShapeMeshBuilder.ClearCache();
                LoadContent();
                RefreshPreviewMesh();
            }

            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy JSON"))
        {
            GUIUtility.systemCopyBuffer = BuildTransformJsonSnippet();
            statusMessage = "Transform JSON copied to clipboard.";
        }

        if (GUILayout.Button("Reset"))
        {
            LoadTransformFromJson();
            dirty = false;
            statusMessage = "Transform reset to JSON values.";
        }

        GUILayout.EndHorizontal();

        GUI.enabled = dirty && !string.IsNullOrEmpty(itemFilePath);
        if (GUILayout.Button($"Save To {Path.GetFileName(itemFilePath ?? "item.json")}"))
        {
            SaveTransformToFile();
        }

        GUI.enabled = true;
    }

    private void DrawStatus()
    {
        GUILayout.Label("Status", EditorStylesBoldLabel());
        GUILayout.Label(statusMessage, GUI.skin.label);
        if (dirty)
        {
            GUILayout.Label("Unsaved changes", WarningLabelStyle());
        }
    }

    private void DrawHelpOverlay(int panelWidth)
    {
        var helpRect = new Rect(panelWidth + 24f, Screen.height - 72f, Screen.width - panelWidth - 36f, 56f);
        GUI.Label(helpRect, previewMode == ItemTransformPreviewMode.GroundStack
            ? "Stack mode previews groundPlacement.stackingShape. Stack Count drives quantityElements tessellation."
            : "Adjust values on the left. Save writes back to the item JSON on disk.\nSwitch mode to edit guiTransform or fpHandTransform separately.");
    }

    private void SaveTransformToFile()
    {
        if (itemJson == null || string.IsNullOrEmpty(itemFilePath))
        {
            statusMessage = "Nothing to save.";
            return;
        }

        var jsonTransform = CreateJsonTransform();
        if (previewMode == ItemTransformPreviewMode.Gui)
        {
            itemJson.guiTransform = jsonTransform;
        }
        else
        {
            itemJson.fpHandTransform = jsonTransform;
        }

        if (!ItemJsonFile.TryWrite(itemFilePath, itemJson, out var error))
        {
            statusMessage = $"Save failed: {error}";
            return;
        }

        dirty = false;
        statusMessage = $"Saved {GetTransformPropertyName()} to {itemFilePath}";
    }

    private ItemDisplayTransformJson CreateJsonTransform() =>
        new()
        {
            translation = ToVector3Json(translation),
            rotation = ToVector3Json(rotation),
            origin = ToVector3Json(origin),
            scale = scale
        };

    private string BuildTransformJsonSnippet()
    {
        return ItemJsonWriter.WriteTransformProperty(GetTransformPropertyName(), CreateJsonTransform());
    }

    private string GetTransformPropertyName() =>
        previewMode == ItemTransformPreviewMode.Gui ? "guiTransform" : "fpHandTransform";

    private ItemDisplayTransformJson GetJsonTransform(ItemTransformPreviewMode mode) =>
        mode == ItemTransformPreviewMode.Gui ? itemJson?.guiTransform : itemJson?.fpHandTransform;

    private static Vector3 ReadVector3(Vector3Json json) =>
        json == null ? Vector3.zero : new Vector3(json.x, json.y, json.z);

    private static Vector3Json ToVector3Json(Vector3 value) =>
        new() { x = value.x, y = value.y, z = value.z };

    private void DrawVector3Field(ref Vector3 value, string label, float min, float max, float step)
    {
        GUILayout.Label(label);
        var x = value.x;
        var y = value.y;
        var z = value.z;
        if (EditorGUILayoutSlider(ref x, "X", min, max, step)
            | EditorGUILayoutSlider(ref y, "Y", min, max, step)
            | EditorGUILayoutSlider(ref z, "Z", min, max, step))
        {
            value = new Vector3(x, y, z);
            MarkDirty();
        }
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    private static bool EditorGUILayoutSlider(ref float value, string label, float min, float max, float step = 0.01f)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(110f));
        var newValue = GUILayout.HorizontalSlider(value, min, max);
        newValue = step > 0f ? Mathf.Round(newValue / step) * step : newValue;
        GUILayout.Label(newValue.ToString("0.###"), GUILayout.Width(48f));
        GUILayout.EndHorizontal();
        if (!Mathf.Approximately(newValue, value))
        {
            value = newValue;
            return true;
        }

        return false;
    }

    private static GUIStyle EditorStylesBoldLabel()
    {
        var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        return style;
    }

    private static GUIStyle WarningLabelStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = new Color(1f, 0.82f, 0.2f);
        return style;
    }
}
