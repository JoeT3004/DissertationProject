using UnityEngine;
using Mapbox.Unity.Map;

public class TroopScaleAdjuster : MonoBehaviour
{
    [Tooltip("Base reference zoom level at which the troop has its 'initialScale'.")]
    [SerializeField] private float referenceZoom = 16f;

    [Tooltip("Minimum possible scale multiplier.")]
    [SerializeField] private float minScale = 0.1f;

    [Tooltip("Maximum possible scale multiplier.")]
    [SerializeField] private float maxScale = 2f;

    [Tooltip("Adjust how much the troop scales up/down as zoom changes.")]
    [SerializeField] private float scaleSensitivity = 0.1f;

    private AbstractMap map;
    private Vector3 initialScale;

    public void Initialize(AbstractMap mapRef)
    {
        map = mapRef;
        initialScale = transform.localScale; // The prefab’s default scale in editor
    }

    private void LateUpdate()
    {
        if (map == null) return; // If not initialized yet, do nothing

        float currentZoom = (float)map.Zoom; // Mapbox zoom
        // e.g. if currentZoom == referenceZoom, scaleMultiplier ~ 1

        // Calculate a linear difference from reference zoom
        // If you want a gentler or more aggressive formula, tweak the math below:
        float zoomDifference = currentZoom - referenceZoom;

        // We'll do a simple approach: scaleMultiplier = 1 / (1 + zoomDifference * scaleSensitivity)
        // This yields bigger scale if zoom < referenceZoom, smaller if zoom > referenceZoom
        // Adjust sign if you want the opposite, or try different formulas to suit your preference
        float scaleMultiplier = 1f / (1f + (zoomDifference * scaleSensitivity));

        // Clamp so it never becomes absurdly large or tiny
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale, maxScale);

        // Apply it
        transform.localScale = initialScale * scaleMultiplier;
    }
}
