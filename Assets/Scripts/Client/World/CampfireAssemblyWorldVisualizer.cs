using System.Collections.Generic;
using UnityEngine;

public sealed class CampfireAssemblyWorldVisualizer : MonoBehaviour
{
    private readonly List<CampfireAssemblySnapshot> snapshotBuffer = new();
    private readonly Dictionary<Vector3Int, AssemblyVisual> visuals = new();

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
        visualsRoot = new GameObject("CampfireAssemblyVisuals").transform;
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

        server.CopyCampfireAssemblySnapshots(snapshotBuffer);

        var seen = new HashSet<Vector3Int>();
        for (int i = 0; i < snapshotBuffer.Count; i++)
        {
            var snapshot = snapshotBuffer[i];
            seen.Add(snapshot.AnchorPosition);

            if (!visuals.TryGetValue(snapshot.AnchorPosition, out var visual) || visual == null)
            {
                visual = AssemblyVisual.Create(visualsRoot, snapshot.AnchorPosition);
                visuals[snapshot.AnchorPosition] = visual;
            }

            visual.Apply(snapshot.State);
        }

        var toRemove = new List<Vector3Int>();
        foreach (var pair in visuals)
        {
            if (!seen.Contains(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var anchor = toRemove[i];
            if (visuals.TryGetValue(anchor, out var visual) && visual != null)
            {
                visual.Destroy();
            }

            visuals.Remove(anchor);
        }
    }

    private sealed class AssemblyVisual
    {
        private readonly Transform root;
        private readonly PlaceholderPart grassPart;
        private readonly PlaceholderPart[] stickParts;

        private AssemblyVisual(Transform root, PlaceholderPart grassPart, PlaceholderPart[] stickParts)
        {
            this.root = root;
            this.grassPart = grassPart;
            this.stickParts = stickParts;
        }

        public static AssemblyVisual Create(Transform parent, Vector3Int anchorPosition)
        {
            var rootObject = new GameObject($"CampfireAssembly_{anchorPosition.x}_{anchorPosition.y}_{anchorPosition.z}");
            var rootTransform = rootObject.transform;
            rootTransform.SetParent(parent, false);
            rootTransform.position = anchorPosition;

            var collider = rootObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.36f, 0.36f, 0.36f);
            collider.center = new Vector3(0f, -0.25f, 0f);

            var grassMesh = ItemPreviewMeshBuilder.GetMesh(ItemKind.GrassBundle);
            var grassPart = PlaceholderPart.Create(rootTransform, "Grass", grassMesh, ItemKind.GrassBundle);

            var stickParts = new PlaceholderPart[CampfireAssembly.RequiredSticks];
            for (int i = 0; i < stickParts.Length; i++)
            {
                var stickMesh = ItemPreviewMeshBuilder.GetSingleStickMesh(i);
                stickParts[i] = PlaceholderPart.Create(rootTransform, $"Stick{i + 1}", stickMesh, ItemKind.Stick);
                stickParts[i].SetActive(false);
            }

            return new AssemblyVisual(rootTransform, grassPart, stickParts);
        }

        public void Apply(CampfireAssemblyState state)
        {
            grassPart.SetActive(true);

            for (int i = 0; i < stickParts.Length; i++)
            {
                stickParts[i].SetActive(i < state.StickCount);
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

    private sealed class PlaceholderPart
    {
        private readonly GameObject gameObject;
        private readonly MeshFilter meshFilter;
        private readonly MeshRenderer meshRenderer;

        private PlaceholderPart(GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            this.gameObject = gameObject;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
        }

        public static PlaceholderPart Create(Transform parent, string name, Mesh mesh, ItemKind itemKind)
        {
            var partObject = new GameObject(name);
            partObject.transform.SetParent(parent, false);

            var meshFilter = partObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = partObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = ItemWorldPlaceholderMaterials.Get(itemKind);
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(mesh);
            partObject.transform.localPosition = new Vector3(0f, -0.5f + groundOffset, 0f);

            return new PlaceholderPart(partObject, meshFilter, meshRenderer);
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
