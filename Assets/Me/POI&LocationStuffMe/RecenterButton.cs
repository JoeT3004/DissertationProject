using System.Collections;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using Mapbox.Utils;

/// <summary>
/// Simple script for a UI button that re-centers the map on the player's current location.
/// Optionally does so automatically after a short delay on startup.
/// </summary>
public class RecenterButton : MonoBehaviour
{
    [SerializeField] private AbstractMap _map;
    private ILocationProvider _locationProvider;

    private void Start()
    {
        if (LocationProviderFactory.Instance != null)
        {
            _locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
        }
        else
        {
            Debug.LogWarning("[RecenterButton] No LocationProviderFactory.Instance available in Start");
        }

        // Optionally recenter once after delay
        StartCoroutine(InvokeRecenterAfterDelay());
    }

    private IEnumerator InvokeRecenterAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        OnRecenterButtonPressed();
    }

    /// <summary>
    /// Called by the UI Button's OnClick() event to center the map on the user's location.
    /// </summary>
    public void OnRecenterButtonPressed()
    {
        if (_locationProvider == null)
        {
            Debug.Log("[RecenterButton] LocationProvider is NULL!");
            return;
        }

        var location = _locationProvider.CurrentLocation;
        Debug.Log($"[RecenterButton] Current lat/lon: {location.LatitudeLongitude}, ServiceEnabled? {location.IsLocationServiceEnabled}");

        if (location.IsLocationServiceEnabled && location.LatitudeLongitude != Vector2d.zero)
        {
            _map.UpdateMap(location.LatitudeLongitude, _map.Zoom);
        }
        else
        {
            Debug.LogWarning("[RecenterButton] No valid location or service is disabled.");
        }
    }
}
