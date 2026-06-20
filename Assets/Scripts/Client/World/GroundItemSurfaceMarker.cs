using UnityEngine;

public sealed class GroundItemSurfaceMarker : MonoBehaviour
{
    public GroundItemSurfaceKey Key { get; private set; }

    public Vector3Int FoundationBlock => Key.FoundationBlock;

    public Vector3 FaceNormal => (Vector3)Key.FaceNormal;

    public void Configure(GroundItemSurfaceKey key)
    {
        Key = key;
    }
}
