using UnityEngine;
using Mapbox.Unity.Map;

public class POIFixedScale : MonoBehaviour
{
    private Vector3 initialScale;
    private AbstractMap _map;

    // Set the minimum zoom at which the POI should be visible.
    [SerializeField] private float minZoomToDisplay = 12.0f;
    private bool _isVisible = true;

    private void Start()
    {
        // Cache the original scale.
        initialScale = transform.localScale;
        // Find the map in the scene. Alternatively, you could assign this manually.
        _map = FindObjectOfType<AbstractMap>();
    }

    private void Update()
    {
        if (_map != null)
        {
            // Determine if the POI should be visible based on the current zoom.
            bool shouldShow = _map.Zoom >= minZoomToDisplay;
            if (shouldShow != _isVisible)
            {
                SetVisibility(shouldShow);
                _isVisible = shouldShow;
            }
        }

        // If the POI is still parented to a scaling object, adjust its scale inversely.
        if (transform.parent != null)
        {
            // Only update the scale when visible.
            if (_isVisible)
            {
                float inverseScaleFactor = 1f / transform.parent.lossyScale.x;
                transform.localScale = initialScale * inverseScaleFactor;
            }
        }
        else
        {
            transform.localScale = initialScale;
        }
    }

    /// <summary>
    /// Enables or disables all renderers in the POI so that it can be shown or hidden.
    /// </summary>
    /// <param name="visible">True to show, false to hide.</param>
    private void SetVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}
