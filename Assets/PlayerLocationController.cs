using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;

public class PlayerLocationController : MonoBehaviour
{
    public AbstractMap map; // Reference to the Mapbox map
    public GameObject playerMarkerPrefab; // Prefab representing the player's position
    public Button recenterButton; // UI Button to recenter the map

    private GameObject playerMarker;
    private ILocationProvider locationProvider;

    void Start()
    {
        locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;

        if (playerMarkerPrefab != null)
        {
            playerMarker = Instantiate(playerMarkerPrefab);
        }

        if (recenterButton != null)
        {
            recenterButton.onClick.AddListener(RecenterMap);
        }
    }

    void Update()
    {
        if (locationProvider == null || playerMarker == null)
            return;

        // Get the user's GPS location
        Vector2d gpsLocation = locationProvider.CurrentLocation.LatitudeLongitude;

        // Convert GPS to Unity world position
        Vector3 worldPosition = map.GeoToWorldPosition(gpsLocation, true);
        playerMarker.transform.position = worldPosition;
    }

    void RecenterMap()
    {
        if (locationProvider == null)
            return;

        Vector2d gpsLocation = locationProvider.CurrentLocation.LatitudeLongitude;
        map.UpdateMap(gpsLocation);
    }
}
