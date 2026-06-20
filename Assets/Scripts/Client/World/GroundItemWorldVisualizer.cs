using System.Collections.Generic;
using UnityEngine;

public sealed class GroundItemWorldVisualizer : MonoBehaviour
{
    private const int MaxVisibleSticks = StickStackLayout.Capacity;

    private readonly List<GroundItemSurfaceSnapshot> snapshotBuffer = new();
    private readonly Dictionary<GroundItemSurfaceKey, SurfaceVisual> visuals = new();

    private BlockWorldServer server;
    private Transform visualsRoot;
    private bool configured;

    public void Configure(BlockWorldServer worldServer)
    {
        if (configured)
        {
            return;
        }

        server = worldServer;
        visualsRoot = new GameObject("GroundItemVisuals").transform;
        visualsRoot.SetParent(transform, false);
        configured = true;
        Sync();
    }

    public void Sync()
    {
        if (!configured || server == null)
        {
            return;
        }

        server.CopyGroundItemSnapshots(snapshotBuffer);

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
        private readonly SlotVisual[] slots;

        private SurfaceVisual(Transform root, SlotVisual[] slots)
        {
            this.root = root;
            this.slots = slots;
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

            var slots = new SlotVisual[4];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = SlotVisual.Create(rootTransform, $"Slot{i}");
            }

            return new SurfaceVisual(rootTransform, slots);
        }

        public void Apply(GroundItemSurfaceSnapshot snapshot)
        {
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
            }
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
        private readonly Transform[] stickParts;
        private readonly MeshFilter[] stickMeshFilters;
        private readonly MeshRenderer[] stickMeshRenderers;

        private SlotVisual(
            Transform root,
            MeshFilter meshFilter,
            MeshRenderer meshRenderer,
            Transform[] stickParts,
            MeshFilter[] stickMeshFilters,
            MeshRenderer[] stickMeshRenderers)
        {
            this.root = root;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
            this.stickParts = stickParts;
            this.stickMeshFilters = stickMeshFilters;
            this.stickMeshRenderers = stickMeshRenderers;
        }

        public static SlotVisual Create(Transform parent, string name)
        {
            var partObject = new GameObject(name);
            partObject.transform.SetParent(parent, false);

            var meshFilter = partObject.AddComponent<MeshFilter>();
            var meshRenderer = partObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var stickParts = new Transform[MaxVisibleSticks];
            var stickMeshFilters = new MeshFilter[MaxVisibleSticks];
            var stickMeshRenderers = new MeshRenderer[MaxVisibleSticks];
            var stickMaterial = ItemWorldPlaceholderMaterials.Get(ItemKind.Stick);

            for (int i = 0; i < MaxVisibleSticks; i++)
            {
                var stickObject = new GameObject($"Stick{i}");
                stickObject.transform.SetParent(partObject.transform, false);

                var stickMeshFilter = stickObject.AddComponent<MeshFilter>();
                stickMeshFilter.sharedMesh = ItemPreviewMeshBuilder.GetGroundStickMesh();

                var stickMeshRenderer = stickObject.AddComponent<MeshRenderer>();
                stickMeshRenderer.sharedMaterial = stickMaterial;
                stickMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                stickMeshRenderer.receiveShadows = false;
                stickObject.SetActive(false);

                stickParts[i] = stickObject.transform;
                stickMeshFilters[i] = stickMeshFilter;
                stickMeshRenderers[i] = stickMeshRenderer;
            }

            return new SlotVisual(
                partObject.transform,
                meshFilter,
                meshRenderer,
                stickParts,
                stickMeshFilters,
                stickMeshRenderers);
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

            if (layout == GroundPlacementLayout.Stack && kind == ItemKind.Stick)
            {
                ApplyStickStack(count);
                return;
            }

            SetStickStackActive(false);
            meshRenderer.enabled = true;

            var mesh = ItemPreviewMeshBuilder.GetMesh(kind);
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = ItemWorldPlaceholderMaterials.Get(kind);

            var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(mesh);
            root.localPosition += root.up * groundOffset;

            root.localScale = layout == GroundPlacementLayout.Pile
                ? Vector3.one * (0.7f + Mathf.Min(count, 64) * 0.006f)
                : layout == GroundPlacementLayout.Dual
                    ? Vector3.one * 0.85f
                    : Vector3.one;
        }

        private void ApplyStickStack(int count)
        {
            meshRenderer.enabled = false;
            var visibleCount = Mathf.Clamp(count, 0, MaxVisibleSticks);
            var stickMaterial = ItemWorldPlaceholderMaterials.Get(ItemKind.Stick);
            var stickMesh = ItemPreviewMeshBuilder.GetGroundStickMesh();
            var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(stickMesh);

            for (int i = 0; i < MaxVisibleSticks; i++)
            {
                var active = i < visibleCount;
                stickParts[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                stickMeshFilters[i].sharedMesh = stickMesh;
                stickMeshRenderers[i].sharedMaterial = stickMaterial;

                StickStackLayout.GetPose(i, out var localOffset, out var localRotation);
                stickParts[i].localPosition = localOffset + Vector3.up * groundOffset;
                stickParts[i].localRotation = localRotation;
                stickParts[i].localScale = Vector3.one;
            }
        }

        private void SetStickStackActive(bool active)
        {
            for (int i = 0; i < MaxVisibleSticks; i++)
            {
                stickParts[i].gameObject.SetActive(active);
            }
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
