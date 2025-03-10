using UnityEngine;
using Mapbox.Unity.Map;

/// <summary>
/// Adjusts the scale of a canvas (e.g., a troop's floating UI) based on the map zoom level.
/// This keeps the UI from becoming too large/small as the camera zooms.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TroopCanvasScaler : MonoBehaviour
{
    //At this reference zoom, the canvas uses 'initialScale'
    [SerializeField] private float referenceZoom = 16f;

    //Scale of the canvas at reference zoom
    [SerializeField] private Vector3 initialScale = new Vector3(0.01f, 0.01f, 0.01f);

    //How aggressively the canvas scales as you zoom away from referenceZoom
    [SerializeField] private float scaleFactor = 0.1f;

    //Clamp min scale
    [SerializeField] private float minScale = 0.005f;

    //Clamp max scale
    [SerializeField] private float maxScale = 0.05f;

    private AbstractMap map;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        transform.localScale = initialScale;
    }

    /// <summary>
    /// Used by TroopController to inject the map reference if needed.
    /// </summary>
    public void SetMap(AbstractMap map)
    {
        this.map = map;
        var scaler = GetComponent<TroopScaleAdjuster>();
        if (scaler != null)
        {
            scaler.Initialize(map);
        }
    }

    /// <summary>
    /// Called every frame; calculates a scale multiplier based on the difference 
    /// between the current zoom and the reference zoom, then clamps the scale.
    /// </summary>
    private void LateUpdate()
    {
        if (!map) return;

        float currentZoom = (float)map.Zoom;
        float zoomDiff = currentZoom - referenceZoom;

        // For each zoom step above reference, scale down slightly.
        // For each zoom step below reference, scale up slightly.
        float scaleMultiplier = 1f - (zoomDiff * scaleFactor);
        scaleMultiplier = Mathf.Clamp(scaleMultiplier,
            minScale / initialScale.x,
            maxScale / initialScale.x);

        transform.localScale = initialScale * scaleMultiplier;
    }
}
