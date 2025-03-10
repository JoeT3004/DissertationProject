using UnityEngine;
using Mapbox.Unity.Map;

/// <summary>
/// Keeps POIs at a fixed scale even if their parent or the map is scaled. 
/// Also optionally hides them if zoom < minZoomToDisplay.
/// </summary>
public class POIFixedScale : MonoBehaviour
{
    private Vector3 initialScale;
    private AbstractMap _map;

    [SerializeField]
    [Tooltip("Hide the POI if the map's zoom is below this value.")]
    private float minZoomToDisplay = 12.0f;

    private bool _isVisible = true;

    private void Start()
    {
        // Cache the original scale
        initialScale = transform.localScale;

        // Find the map in the scene
        _map = FindObjectOfType<AbstractMap>();
    }

    private void Update()
    {
        if (_map != null)
        {
            bool shouldShow = _map.Zoom >= minZoomToDisplay;
            if (shouldShow != _isVisible)
            {
                SetVisibility(shouldShow);
                _isVisible = shouldShow;
            }
        }

        // If the POI is parented under a scaling object, 
        // scale inversely to keep it the same size
        if (_isVisible && transform.parent != null)
        {
            float inverseScaleFactor = 1f / transform.parent.lossyScale.x;
            transform.localScale = initialScale * inverseScaleFactor;
        }
        else if (!_isVisible)
        {
            // Hide => do nothing
        }
        else
        {
            // If no parent, just keep initial scale
            transform.localScale = initialScale;
        }
    }

    /// <summary>
    /// Enables or disables all renderers in the POI, effectively hiding or showing it.
    /// </summary>
    private void SetVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}
