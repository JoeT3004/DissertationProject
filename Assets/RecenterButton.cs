using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using Mapbox.Utils;

public class RecenterButton : MonoBehaviour
{
    [SerializeField]
    private AbstractMap _map;
    private ILocationProvider _locationProvider;

    void Start()
    {
        if (LocationProviderFactory.Instance != null)
        {
            _locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
        }
        else
        {
            Debug.LogWarning("No LocationProviderFactory.Instance available in Start");
        }
    }


    // This method can be called by your UI Button's OnClick event
    public void OnRecenterButtonPressed()
    {
        if (_locationProvider == null)
        {
            Debug.Log("LocationProvider is NULL!");
            return;
        }

        var location = _locationProvider.CurrentLocation;
        Debug.Log($"Current lat/lon: {location.LatitudeLongitude}  ServiceEnabled? {location.IsLocationServiceEnabled}");

        if (location.IsLocationServiceEnabled && location.LatitudeLongitude != Vector2d.zero)
        {
            _map.UpdateMap(location.LatitudeLongitude, _map.Zoom);
        }
        else
        {
            Debug.LogWarning("No valid location or service disabled.");
        }
    }

}
