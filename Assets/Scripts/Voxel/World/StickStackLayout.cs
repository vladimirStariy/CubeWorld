using UnityEngine;

public readonly struct StickStackPose
{
    public readonly Vector3 LocalPosition;
    public readonly Quaternion LocalRotation;

    public StickStackPose(Vector3 localPosition, Quaternion localRotation)
    {
        LocalPosition = localPosition;
        LocalRotation = localRotation;
    }
}

public static class StickStackLayout
{
    public const int Capacity = 16;
    public const int ShiftPickupAmount = 4;

    private const float StickThickness = 0.045f;

    private static readonly StickStackPose[] Poses = CreatePoses();
    private static Mesh bakedFullStackMesh;

    public static int PoseCount => Poses.Length;

    public static void GetPose(int index, out Vector3 localPosition, out Quaternion localRotation)
    {
        if (index < 0 || index >= Poses.Length)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return;
        }

        var pose = Poses[index];
        localPosition = pose.LocalPosition;
        localRotation = pose.LocalRotation;
    }

    public static Mesh GetBakedFullStackMesh()
    {
        if (bakedFullStackMesh != null)
        {
            return bakedFullStackMesh;
        }

        var stickMesh = ItemPreviewMeshBuilder.GetGroundStickMesh();
        if (stickMesh == null)
        {
            return null;
        }

        var groundOffset = ItemPreviewMeshBuilder.GetMeshGroundOffset(stickMesh);
        var combines = new CombineInstance[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            GetPose(i, out var position, out var rotation);
            combines[i] = new CombineInstance
            {
                mesh = stickMesh,
                transform = Matrix4x4.TRS(position + Vector3.up * groundOffset, rotation, Vector3.one)
            };
        }

        bakedFullStackMesh = new Mesh { name = "StickStack16" };
        bakedFullStackMesh.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);
        bakedFullStackMesh.RecalculateBounds();
        return bakedFullStackMesh;
    }

    private static StickStackPose[] CreatePoses()
    {
        var poses = new StickStackPose[Capacity];
        var index = 0;

        index = WriteLayer(poses, index, layerY: 0.000f, alongX: true, spread: 0.22f, count: 4);
        index = WriteLayer(poses, index, layerY: StickThickness, alongX: false, spread: 0.20f, count: 4);
        index = WriteLayer(poses, index, layerY: StickThickness * 2f, alongX: true, spread: 0.15f, count: 4);
        index = WriteLayer(poses, index, layerY: StickThickness * 3f, alongX: false, spread: 0.11f, count: 3);
        WriteLayer(poses, index, layerY: StickThickness * 4f, alongX: true, spread: 0f, count: 1);

        return poses;
    }

    private static int WriteLayer(
        StickStackPose[] poses,
        int startIndex,
        float layerY,
        bool alongX,
        float spread,
        int count)
    {
        var yaw = alongX ? 90f : 0f;
        if (count == 1)
        {
            poses[startIndex] = new StickStackPose(
                new Vector3(0f, layerY, 0f),
                Quaternion.Euler(0f, yaw, 0f));
            return startIndex + 1;
        }

        var step = spread * 2f / (count - 1);
        for (int i = 0; i < count; i++)
        {
            var offset = -spread + step * i;
            var position = alongX
                ? new Vector3(0f, layerY, offset)
                : new Vector3(offset, layerY, 0f);
            poses[startIndex + i] = new StickStackPose(position, Quaternion.Euler(0f, yaw, 0f));
        }

        return startIndex + count;
    }
}
