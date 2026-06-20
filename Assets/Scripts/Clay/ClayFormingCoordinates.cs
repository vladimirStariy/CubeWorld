using UnityEngine;

public static class ClayFormingCoordinates
{
    public static int NormalToFaceIndex(Vector3Int normal)
    {
        return VoxelConstants.NormalToFaceIndex((Vector3)normal);
    }

    public static void GetFaceAxes(int faceIndex, out Vector3 axisU, out Vector3 axisV)
    {
        switch (faceIndex)
        {
            case 0:
                axisU = Vector3.up;
                axisV = Vector3.forward;
                break;
            case 1:
                axisU = Vector3.up;
                axisV = Vector3.back;
                break;
            case 2:
                axisU = Vector3.right;
                axisV = Vector3.back;
                break;
            case 3:
                axisU = Vector3.right;
                axisV = Vector3.forward;
                break;
            case 4:
                axisU = Vector3.right;
                axisV = Vector3.up;
                break;
            default:
                axisU = Vector3.right;
                axisV = Vector3.up;
                break;
        }
    }

    public static Vector3 GetFaceOriginWorld(Vector3Int anchorBlock, int faceIndex)
    {
        return (Vector3)anchorBlock + VoxelConstants.FaceVertices[faceIndex][0];
    }

    public static Vector3 GetVoxelCenterWorld(
        Vector3Int anchorBlock,
        Vector3Int faceNormal,
        int layer,
        int u,
        int v)
    {
        var faceIndex = NormalToFaceIndex(faceNormal);
        var normal = (Vector3)faceNormal;
        GetFaceAxes(faceIndex, out var axisU, out var axisV);
        var origin = GetFaceOriginWorld(anchorBlock, faceIndex);
        var step = ClayFormingConstants.VoxelSize;
        return origin
               + axisU * ((u + 0.5f) * step)
               + axisV * ((v + 0.5f) * step)
               + normal * ((layer + 0.5f) * step);
    }

    public static bool TryWorldPointToCell(
        Vector3 worldPoint,
        Vector3Int anchorBlock,
        Vector3Int faceNormal,
        int layer,
        out int u,
        out int v)
    {
        u = -1;
        v = -1;

        var faceIndex = NormalToFaceIndex(faceNormal);
        var normal = (Vector3)faceNormal;
        GetFaceAxes(faceIndex, out var axisU, out var axisV);
        var origin = GetFaceOriginWorld(anchorBlock, faceIndex);
        var layerOrigin = origin + normal * (layer * ClayFormingConstants.VoxelSize);
        var offset = worldPoint - layerOrigin;

        var uCoord = Vector3.Dot(offset, axisU);
        var vCoord = Vector3.Dot(offset, axisV);
        if (uCoord < 0f || vCoord < 0f || uCoord >= 1f || vCoord >= 1f)
        {
            return false;
        }

        u = Mathf.Clamp(Mathf.FloorToInt(uCoord / ClayFormingConstants.VoxelSize), 0, ClayFormingConstants.GridSize - 1);
        v = Mathf.Clamp(Mathf.FloorToInt(vCoord / ClayFormingConstants.VoxelSize), 0, ClayFormingConstants.GridSize - 1);
        return true;
    }
}
