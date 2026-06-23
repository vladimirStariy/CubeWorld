using System.Collections.Generic;
using UnityEngine;

public sealed class GroundItemWorldVisualizer : MonoBehaviour
{
    private readonly List<GroundItemSurfaceSnapshot> snapshotBuffer = new();
    private readonly Dictionary<GroundItemSurfaceKey, SurfaceVisual> visuals = new();

    private IWorldAuthority authority;
    private Transform visualsRoot;
    private bool configured;

    public void Configure(IWorldAuthority worldAuthority)
    {
        if (configured)
        {
            return;
        }

        authority = worldAuthority;
        visualsRoot = new GameObject("GroundItemVisuals").transform;
        visualsRoot.SetParent(transform, false);
        configured = true;
        Sync();
    }

    public void Sync()
    {
        if (!configured || authority == null)
        {
            return;
        }

        authority.CopyGroundItemSnapshots(snapshotBuffer);

        var seen = new HashSet<GroundItemSurfaceKey>();
        for (int i = 0; i < snapshotBuffer.Count; i++)
        {
            var snapshot = snapshotBuffer[i];
            seen.Add(snapshot.Key);

            if (!visuals.TryGetValue(snapshot.Key, out var visual) || visual == null)
            {
                visual = SurfaceVisual.Create(visualsRoot, snapshot.Key);
                visuals[snapshot.Key] = visual;
            }

            visual.Apply(snapshot);
        }

        var toRemove = new List<GroundItemSurfaceKey>();
        foreach (var pair in visuals)
        {
            if (!seen.Contains(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var key = toRemove[i];
            if (visuals.TryGetValue(key, out var visual) && visual != null)
            {
                visual.Destroy();
            }

            visuals.Remove(key);
        }
    }

    private sealed class SurfaceVisual
    {
        private readonly Transform root;
        private readonly BoxCollider collider;
        private readonly SlotVisual[] slots;
        private readonly GroundItemSurfaceKey key;

        private SurfaceVisual(Transform root, BoxCollider collider, SlotVisual[] slots, GroundItemSurfaceKey key)
        {
            this.root = root;
            this.collider = collider;
            this.slots = slots;
            this.key = key;
        }

        public static SurfaceVisual Create(Transform parent, GroundItemSurfaceKey key)
        {
            var rootObject = new GameObject($"GroundItems_{key.FoundationBlock.x}_{key.FoundationBlock.y}_{key.FoundationBlock.z}");
            var rootTransform = rootObject.transform;
            rootTransform.SetParent(parent, false);
            rootTransform.position = key.FoundationBlock;

            var collider = rootObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.92f, 0.2f, 0.92f);
            collider.center = GroundItemPlacementMath.GetSlotLocalPosition(key.FaceNormal, GroundPlacementLayout.Single, 0);

            rootObject.AddComponent<GroundItemSurfaceMarker>().Configure(key);

            var slots = new SlotVisual[4];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = SlotVisual.Create(rootTransform, $"Slot{i}");
            }

            return new SurfaceVisual(rootTransform, collider, slots, key);
        }

        public void Apply(GroundItemSurfaceSnapshot snapshot)
        {
            GroundItemPlacementProfile? stackProfile = null;
            var stackCount = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (i >= snapshot.Slots.Length)
                {
                    slots[i].SetActive(false);
                    continue;
                }

                var slot = snapshot.Slots[i];
                if (slot.Count <= 0 || slot.Kind == ItemKind.None)
                {
                    slots[i].SetActive(false);
                    continue;
                }

                slots[i].SetActive(true);
                slots[i].Apply(
                    slot.Kind,
                    slot.Count,
                    snapshot.Key.FaceNormal,
                    snapshot.Layout,
                    i);

                if (snapshot.Layout == GroundPlacementLayout.Stack
                    && GroundItemPlacementProfiles.TryGet(slot.Kind, out var profile)
                    && profile.HasStackingShape
                    && i == 0)
                {
                    stackProfile = profile;
                    stackCount = slot.Count;
                }
            }

            UpdateCollider(stackProfile, stackCount, snapshot.Layout);
        }

        private void UpdateCollider(GroundItemPlacementProfile? stackProfile, int stackCount, GroundPlacementLayout layout)
        {
            var slotOrigin = GroundItemPlacementMath.GetSlotLocalPosition(key.FaceNormal, layout, 0);

            if (!stackProfile.HasValue || stackCount <= 0)
            {
                collider.size = new Vector3(0.92f, 0.2f, 0.92f);
                collider.center = slotOrigin;
                return;
            }

            var profile = stackProfile.Value;
            var height = GroundStackMath.GetStackHeight(stackCount, profile);
            collider.size = new Vector3(1f, Mathf.Max(height, 0.08f), 1f);
            collider.center = slotOrigin + (Vector3)key.FaceNormal * (height * 0.5f);
        }

        public void Destroy()
        {
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }
        }
    }

    private sealed class SlotVisual
    {
        private readonly Transform root;
        private readonly MeshFilter meshFilter;
        private readonly MeshRenderer meshRenderer;

        private SlotVisual(Transform root, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            this.root = root;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
        }

        public static SlotVisual Create(Transform parent, string name)
        {
            var partObject = new GameObject(name);
            partObject.transform.SetParent(parent, false);

            var meshFilter = partObject.AddComponent<MeshFilter>();
            var meshRenderer = partObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return new SlotVisual(partObject.transform, meshFilter, meshRenderer);
        }

        public void Apply(
            ItemKind kind,
            int count,
            Vector3Int faceNormal,
            GroundPlacementLayout layout,
            int slotIndex)
        {
            root.localPosition = GroundItemPlacementMath.GetSlotLocalPosition(faceNormal, layout, slotIndex);
            root.localRotation = GetFaceRotation(faceNormal);
            root.localScale = Vector3.one;
            meshRenderer.enabled = true;

            if (layout == GroundPlacementLayout.Stack
                && GroundItemPlacementProfiles.TryGet(kind, out var profile)
                && profile.HasStackingShape)
            {
                ApplyShapeStack(kind, count, profile);
                return;
            }

            if (!TryResolveItemRenderData(kind, out var mesh, out var material))
            {
                meshRenderer.enabled = false;
                return;
            }

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;

            var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(mesh);
            root.localPosition += root.up * groundOffset;

            if (layout == GroundPlacementLayout.Pile)
            {
                root.localScale = Vector3.one * (0.7f + Mathf.Min(count, 64) * 0.006f);
            }
            else if (layout == GroundPlacementLayout.Dual)
            {
                root.localScale = Vector3.one * 0.85f;
            }
        }

        private void ApplyShapeStack(ItemKind kind, int count, GroundItemPlacementProfile profile)
        {
            var quantity = GroundStackMath.GetQuantityElements(count, profile);
            if (!ItemShapeRegistry.Active.TryGet(profile.StackingShapeId, out var shapeDefinition)
                || !ItemShapeMeshBuilder.TryGetRenderData(profile.StackingShapeId, quantity, kind, out var mesh, out var material)
                || mesh == null
                || material == null)
            {
                meshRenderer.enabled = false;
                return;
            }

            meshFilter.sharedMesh = ItemShapeMeshBuilder.GetGroundStackMesh(mesh, shapeDefinition);
            meshRenderer.sharedMaterial = material;
        }

        private static bool TryResolveItemRenderData(ItemKind kind, out Mesh mesh, out Material material)
        {
            mesh = null;
            material = null;

            if (ItemRegistry.Active != null
                && ItemRegistry.Active.TryGet(kind, VoxelBlockType.Air, out var definition)
                && !string.IsNullOrEmpty(definition.ShapeId.Name)
                && ItemShapeMeshBuilder.TryGetRenderData(definition.ShapeId, int.MaxValue, kind, out mesh, out material)
                && mesh != null
                && material != null)
            {
                return true;
            }

            mesh = ItemPreviewMeshBuilder.GetMesh(kind);
            material = ItemWorldPlaceholderMaterials.Get(kind);
            return mesh != null && material != null;
        }

        public void SetActive(bool active)
        {
            root.gameObject.SetActive(active);
        }

        private static Quaternion GetFaceRotation(Vector3Int faceNormal)
        {
            return faceNormal switch
            {
                var n when n == Vector3Int.up => Quaternion.identity,
                var n when n == Vector3Int.down => Quaternion.Euler(180f, 0f, 0f),
                var n when n == Vector3Int.right => Quaternion.Euler(0f, 0f, -90f),
                var n when n == Vector3Int.left => Quaternion.Euler(0f, 0f, 90f),
                var n when n == new Vector3Int(0, 0, 1) => Quaternion.Euler(90f, 0f, 0f),
                _ => Quaternion.Euler(-90f, 0f, 0f)
            };
        }
    }
}
