using UnityEngine;
using UnityEngine.UI;

public sealed class CrosshairUI : MonoBehaviour
{
    [SerializeField] private float size = 8f;
    [SerializeField] private Color color = new(1f, 1f, 1f, 0.3f);

    private bool configured;

    public void Configure(Canvas hudCanvas)
    {
        if (configured)
        {
            return;
        }

        EnsureCrosshair(hudCanvas);
        configured = true;
    }

    private void EnsureCrosshair(Canvas hudCanvas)
    {
        if (hudCanvas.GetComponentInChildren<CrosshairMarker>() != null)
        {
            return;
        }

        var crosshairObject = new GameObject("Crosshair");
        crosshairObject.transform.SetParent(hudCanvas.transform, false);
        crosshairObject.AddComponent<CrosshairMarker>();

        var image = crosshairObject.AddComponent<Image>();
        image.color = color;

        var rectTransform = crosshairObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(size, size);
    }
}

public sealed class CrosshairMarker : MonoBehaviour
{
}
