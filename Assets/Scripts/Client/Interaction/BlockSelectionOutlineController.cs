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
    private Vector3Int lastOutlinedFaceNormal = Vector3Int.zero;
    private int lastOutlinedStickCount = -1;
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
        lastOutlinedFaceNormal = Vector3Int.zero;
        lastOutlinedStickCount = -1;
    }

    public void Update()
    {
        if (selectionOutline == null)
        {
            return;
        }

        if (!BlockWorldTargeting.TryRaycastInteractTarget(playerCamera, interactDistance, out var target))
        {
            Hide();
            return;
        }

        var blockPosition = target.BlockPosition;
        var faceNormal = Vector3Int.RoundToInt(target.FaceNormal);

        if (TryShowStickStackOutline(target, blockPosition, faceNormal))
        {
            return;
        }

        if (target.IsGroundItem)
        {
            Hide();
            return;
        }

        if (outlineVisible
            && blockPosition == lastOutlinedBlock
            && faceNormal == lastOutlinedFaceNormal
            && lastOutlinedStickCount < 0)
        {
            return;
        }

        if (!server.TryGetOutlineSegments(blockPosition, faceOutlineSegments))
        {
            Hide();
            return;
        }

        lastOutlinedBlock = blockPosition;
        lastOutlinedFaceNormal = faceNormal;
        lastOutlinedStickCount = -1;
        outlineVisible = true;
        selectionOutline.ShowFaceOutline(blockPosition, faceOutlineSegments);
    }

    private bool TryShowStickStackOutline(
        WorldInteractTarget target,
        Vector3Int blockPosition,
        Vector3Int faceNormal)
    {
        int stickCount;
        var hasStickStack = target.GroundItemKey.HasValue
            ? server.TryGetStickStackCount(target.GroundItemKey.Value, out stickCount)
            : server.TryGetStickStackCount(blockPosition, faceNormal, out stickCount);

        if (!hasStickStack)
        {
            return false;
        }

        if (outlineVisible
            && blockPosition == lastOutlinedBlock
            && faceNormal == lastOutlinedFaceNormal
            && stickCount == lastOutlinedStickCount)
        {
            return true;
        }

        var built = target.GroundItemKey.HasValue
            ? server.TryBuildStickStackOutline(target.GroundItemKey.Value, faceOutlineSegments)
            : server.TryBuildStickStackOutline(blockPosition, faceNormal, faceOutlineSegments);

        if (!built)
        {
            Hide();
            return true;
        }

        lastOutlinedBlock = blockPosition;
        lastOutlinedFaceNormal = faceNormal;
        lastOutlinedStickCount = stickCount;
        outlineVisible = true;
        selectionOutline.ShowFaceOutline(blockPosition, faceOutlineSegments);
        return true;
    }

    private void Hide()
    {
        InvalidateCache();
        selectionOutline.Hide();
    }
}
