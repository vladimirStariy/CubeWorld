using System.Collections.Generic;

public static class BlockShapeOcclusion
{
    public static bool IsNeighborOccludingFace(VoxelBlockType neighborType, int faceFromCurrentToNeighbor)
    {
        if (neighborType == VoxelBlockType.Air)
        {
            return false;
        }

        var shape = BlockShapeLibrary.Get(neighborType);
        if (shape == null)
        {
            return true;
        }

        switch (shape.OcclusionMode)
        {
            case BlockOcclusionMode.None:
                return false;
            case BlockOcclusionMode.Full:
                return true;
            case BlockOcclusionMode.Boxes:
                var neighborFace = VoxelConstants.OppositeFace[faceFromCurrentToNeighbor];
                return CoversFace(shape.OcclusionBoxes, neighborFace);
            default:
                return true;
        }
    }

    private static bool CoversFace(IReadOnlyList<BlockOcclusionBox> boxes, int faceIndex)
    {
        if (boxes == null || boxes.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            if (!BoxProjectsFullFaceCoverage(boxes[i], faceIndex))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool BoxProjectsFullFaceCoverage(BlockOcclusionBox box, int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return box.MaxX == 16
                       && box.MinY <= 0 && box.MaxY >= 16
                       && box.MinZ <= 0 && box.MaxZ >= 16;
            case 1:
                return box.MinX == 0
                       && box.MinY <= 0 && box.MaxY >= 16
                       && box.MinZ <= 0 && box.MaxZ >= 16;
            case 2:
                return box.MaxY == 16
                       && box.MinX <= 0 && box.MaxX >= 16
                       && box.MinZ <= 0 && box.MaxZ >= 16;
            case 3:
                return box.MinY == 0
                       && box.MinX <= 0 && box.MaxX >= 16
                       && box.MinZ <= 0 && box.MaxZ >= 16;
            case 4:
                return box.MaxZ == 16
                       && box.MinX <= 0 && box.MaxX >= 16
                       && box.MinY <= 0 && box.MaxY >= 16;
            case 5:
                return box.MinZ == 0
                       && box.MinX <= 0 && box.MaxX >= 16
                       && box.MinY <= 0 && box.MaxY >= 16;
            default:
                return false;
        }
    }
}
