using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using Mapbox.Utils;

/// <summary>
/// Manages the spawning of Points of Interest (POIs) from a JSON file,
/// placing them on the map, and allowing the user to 'collect' them 
/// by being within a certain distance (collectionDistanceMeters).
/// </summary>
public class POIManager : MonoBehaviour
{
    [Header("JSON Input")]
    [SerializeField] private TextAsset poiJson;

    [Header("Map Reference")]
    [SerializeField] private AbstractMap map;

    [Header("POI Prefab")]
    [SerializeField] private GameObject poiPrefab;

    [Header("Collection Settings")]
    [Tooltip("Distance in METERS to collect a POI")]
    [SerializeField] private float collectionDistanceMeters = 10f;

    // Deserialized from poiJson (RootObject/Element from your OSMDataModel)
    private RootObject _poiData;
    private List<Element> _allPOIs = new List<Element>();

    // Instantiated POI GameObjects
    private List<GameObject> _spawnedPOIs = new List<GameObject>();

    // True once we spawn POIs the first time
    private bool _hasSpawnedPOIs = false;

    /// <summary>
    /// Parse the JSON once in Awake to fill _allPOIs.
    /// </summary>
    private void Awake()
    {
        if (poiJson == null)
        {
            Debug.LogError("poiJson is not assigned. Please assign a JSON file in the Inspector.");
            return;
        }

        string jsonString = poiJson.text;
        _poiData = JsonUtility.FromJson<RootObject>(jsonString);

        if (_poiData != null && _poiData.elements != null)
        {
            _allPOIs.AddRange(_poiData.elements);
            Debug.Log($"[POIManager] Parsed {_allPOIs.Count} POIs from JSON.");
        }
        else
        {
            Debug.LogError("[POIManager] Failed to parse JSON or no POI elements found.");
        }
    }

    private void OnEnable()
    {
        if (map != null)
        {
            map.OnInitialized += Map_OnInitialized;
            map.OnUpdated += Map_OnUpdated;
        }
        else
        {
            Debug.LogError("[POIManager] Map reference is missing!");
        }
    }

    private void OnDisable()
    {
        if (map != null)
        {
            map.OnInitialized -= Map_OnInitialized;
            map.OnUpdated -= Map_OnUpdated;
        }
    }

    /// <summary>
    /// Once the map is initialized, we spawn all POIs if we haven't yet.
    /// </summary>
    private void Map_OnInitialized()
    {
        if (!_hasSpawnedPOIs)
        {
            _hasSpawnedPOIs = true;
            Debug.Log("[POIManager] Map initialized. Spawning POIs...");
            SpawnAllPOIs();
        }
    }

    /// <summary>
    /// Called when the map is updated (pans, zooms, re-centers, etc.).
    /// Repositions each POI in world space.
    /// </summary>
    private void Map_OnUpdated()
    {
        foreach (var poiObject in _spawnedPOIs)
        {
            if (poiObject == null) continue;

            POIBehaviour poiBehaviour = poiObject.GetComponent<POIBehaviour>();
            if (poiBehaviour == null) continue;

            // Recalculate the correct position in Unity world space
            Vector3 newPos = map.GeoToWorldPosition(poiBehaviour.latLon);
            poiObject.transform.position = newPos;
        }
    }

    /// <summary>
    /// Instantiates POI prefabs at their lat/lon positions, stored in _allPOIs.
    /// </summary>
    private void SpawnAllPOIs()
    {
        if (_allPOIs.Count == 0)
        {
            Debug.LogWarning("[POIManager] No POIs to spawn.");
            return;
        }
        if (map == null)
        {
            Debug.LogError("[POIManager] AbstractMap reference is missing.");
            return;
        }

        foreach (var poi in _allPOIs)
        {
            Vector2d latLon = new Vector2d(poi.lat, poi.lon);
            Vector3 worldPos = map.GeoToWorldPosition(latLon);

            GameObject newPOI = Instantiate(poiPrefab, worldPos, Quaternion.identity);
            POIBehaviour poiBehaviour = newPOI.GetComponent<POIBehaviour>();
            if (poiBehaviour != null)
            {
                poiBehaviour.latLon = latLon;
                poiBehaviour.Id = poi.id;
                poiBehaviour.Amenity = poi.tags.amenity;
                poiBehaviour.Ref = poi.tags.@ref;
            }

            _spawnedPOIs.Add(newPOI);
        }
    }

    /// <summary>
    /// Each frame, we check the player's location vs. each POI to see if they're 
    /// within 'collectionDistanceMeters'. If so, we award points and remove the POI.
    /// </summary>
    private void Update()
    {
        var locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
        if (locationProvider == null) return;

        var currentLocation = locationProvider.CurrentLocation;
        double playerLat = currentLocation.LatitudeLongitude.x;
        double playerLon = currentLocation.LatitudeLongitude.y;
        Vector2d playerLatLon = new Vector2d(playerLat, playerLon);

        // Check each POI
        for (int i = _spawnedPOIs.Count - 1; i >= 0; i--)
        {
            GameObject poiObj = _spawnedPOIs[i];
            if (poiObj == null) continue;

            POIBehaviour poiBehaviour = poiObj.GetComponent<POIBehaviour>();
            if (poiBehaviour == null) continue;

            // Use GeoUtils instead of local Haversine
            double distMeters = GeoUtils.HaversineDistance(playerLatLon, poiBehaviour.latLon);

            if (distMeters < collectionDistanceMeters)
            {
                Debug.Log($"Collected POI {poiBehaviour.Id} at distance {distMeters:F1} m");

                // Award some points
                ScoreManager.Instance.AddPoints(10);

                // Remove the POI from scene and list
                Destroy(poiObj);
                _spawnedPOIs.RemoveAt(i);
            }
        }
    }
}
