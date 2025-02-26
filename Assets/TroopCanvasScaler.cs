using UnityEngine;
using Mapbox.Unity.Map;

[RequireComponent(typeof(Canvas))]
public class TroopCanvasScaler : MonoBehaviour
{
    private AbstractMap map;

    public void SetMap(AbstractMap map)
    {
        this.map = map;

        // If we also have a TroopScaler, initialize it
        var scaler = GetComponent<TroopScaleAdjuster>();
        if (scaler != null)
        {
            scaler.Initialize(map);
        }
    }

    [Tooltip("At this reference zoom, the canvas uses 'initialScale'.")]
    [SerializeField]
    private float referenceZoom = 16f;

    [Tooltip("Scale of the canvas at reference zoom.")]
    [SerializeField]
    private Vector3 initialScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Tooltip("How aggressively the canvas scales as you zoom away from referenceZoom.")]
    [SerializeField]
    private float scaleFactor = 0.1f;

    [Tooltip("Clamp min scale.")]
    [SerializeField]
    private float minScale = 0.005f;

    [Tooltip("Clamp max scale.")]
    [SerializeField]
    private float maxScale = 0.05f;

    private Canvas canvas;
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        transform.localScale = initialScale;
    }

    private void LateUpdate()
    {
        if (!map) return;

        float currentZoom = (float)map.Zoom;
        float zoomDiff = currentZoom - referenceZoom;

        // Example formula:
        // If zoomDiff is positive (zoom > referenceZoom), we want to shrink a bit
        // If zoomDiff is negative (zoom < referenceZoom), we want to enlarge a bit
        // scale = initialScale.x * (1 - (zoomDiff * scaleFactor)) ... or something
        float scaleMultiplier = 1f - (zoomDiff * scaleFactor);

        // clamp scaleMultiplier
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale / initialScale.x, maxScale / initialScale.x);

        // apply
        transform.localScale = initialScale * scaleMultiplier;
    }
}
