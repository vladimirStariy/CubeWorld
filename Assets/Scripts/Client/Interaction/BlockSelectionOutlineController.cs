using System.Collections.Generic;
using UnityEngine;

public sealed class BlockSelectionOutlineController
{
    private readonly BlockWorldServer server;
    private readonly Camera playerCamera;
    private readonly float interactDistance;
    private readonly List<LineSegment> faceOutlineSegments = new();

    private BlockSelectionOutline selectionOutline;
    private Vector3Int lastOutlinedBlock = new(int.MinValue, int.MinValue, int.MinValue);
    private bool outlineVisible;

    public BlockSelectionOutlineController(BlockWorldServer server, Camera playerCamera, float interactDistance)
    {
        this.server = server;
        this.playerCamera = playerCamera;
        this.interactDistance = interactDistance;
    }

    public void AttachOutline(BlockSelectionOutline outline)
    {
        selectionOutline = outline;
    }

    public BlockSelectionOutline EnsureOutline(Transform parent)
    {
        if (selectionOutline != null)
        {
            return selectionOutline;
        }

        var outlineObject = new GameObject("BlockSelectionOutline");
        outlineObject.transform.SetParent(parent, false);
        selectionOutline = outlineObject.AddComponent<BlockSelectionOutline>();
        return selectionOutline;
    }

    public void InvalidateCache()
    {
        outlineVisible = false;
        lastOutlinedBlock = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    }

    public void Update()
    {
        if (selectionOutline == null)
        {
            return;
        }

        if (!BlockWorldTargeting.TryRaycastBlock(playerCamera, interactDistance, out var hit))
        {
            Hide();
            return;
        }

        var blockPosition = BlockWorldTargeting.GetHitBlockPosition(hit);
        if (outlineVisible && blockPosition == lastOutlinedBlock)
        {
            return;
        }

        if (!server.TryGetOutlineSegments(blockPosition, faceOutlineSegments))
        {
            Hide();
            return;
        }

        lastOutlinedBlock = blockPosition;
        outlineVisible = true;
        selectionOutline.ShowFaceOutline(blockPosition, faceOutlineSegments);
    }

    private void Hide()
    {
        InvalidateCache();
        selectionOutline.Hide();
    }
}
