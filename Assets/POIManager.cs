using UnityEngine;
using System.Collections.Generic;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;

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

    // Data parsed from the JSON (OSMDataModel.cs types)
    private RootObject _poiData;
    private List<Element> _allPOIs = new List<Element>();

    // All instantiated POIs
    private List<GameObject> _spawnedPOIs = new List<GameObject>();

    // Flag so we spawn POIs once after map init
    private bool _hasSpawnedPOIs = false;

    // --------------------------------------------------
    // 1. Parse JSON in Awake
    // --------------------------------------------------
    private void Awake()
    {
        if (poiJson == null)
        {
            Debug.LogError("poiJson is not assigned. Please assign a JSON file in the Inspector.");
            return;
        }

        // Deserialize JSON
        string jsonString = poiJson.text;
        _poiData = JsonUtility.FromJson<RootObject>(jsonString);

        if (_poiData != null && _poiData.elements != null)
        {
            _allPOIs.AddRange(_poiData.elements);
            Debug.Log($"Parsed {_allPOIs.Count} POIs from JSON.");
        }
        else
        {
            Debug.LogError("Failed to parse JSON or no POI elements found.");
        }
    }

    // --------------------------------------------------
    // 2. Subscribe to map events
    // --------------------------------------------------
    private void OnEnable()
    {
        if (map != null)
        {
            // Called once when map finishes its initial load
            map.OnInitialized += Map_OnInitialized;

            // Called whenever the map re-centers or updates
            map.OnUpdated += Map_OnUpdated;
        }
        else
        {
            Debug.LogError("Map reference is missing in POIManager!");
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

    // --------------------------------------------------
    // 3. Spawn POIs once the map has completed initialization
    // --------------------------------------------------
    private void Map_OnInitialized()
    {
        if (!_hasSpawnedPOIs)
        {
            _hasSpawnedPOIs = true;
            Debug.Log("Map initialized. Spawning POIs now...");
            SpawnAllPOIs();
        }
    }

    private void SpawnAllPOIs()
    {
        if (_allPOIs.Count == 0)
        {
            Debug.LogWarning("No POIs to spawn.");
            return;
        }

        if (map == null)
        {
            Debug.LogError("AbstractMap reference is missing.");
            return;
        }

        foreach (var poi in _allPOIs)
        {
            Vector2d latLon = new Vector2d(poi.lat, poi.lon);

            // Convert lat/lon to an initial world position
            Vector3 worldPos = map.GeoToWorldPosition(latLon);
            //Debug.Log($"Spawning POI: lat/lon {latLon} => worldPos {worldPos}");

            // Create the POI object in the scene
            GameObject newPOI = Instantiate(poiPrefab, worldPos, Quaternion.identity);

            // Store the lat/lon & any data in the POIBehaviour script
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

    // --------------------------------------------------
    // 4. Reposition POIs on every map update
    // --------------------------------------------------
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

    // --------------------------------------------------
    // 5. Distance-based collection using GEO DISTANCE
    // --------------------------------------------------
    private void Update()
    {
        // 1. Get the player's current lat/lon from the default Location Provider
        var locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
        if (locationProvider == null) return;

        //implementation of this varies on mapbox version
        var currentLocation = locationProvider.CurrentLocation;
        double playerLat = currentLocation.LatitudeLongitude.x;
        double playerLon = currentLocation.LatitudeLongitude.y;
        Vector2d playerLatLon = new Vector2d(playerLat, playerLon);


        // 2. Check each POI for real-world distance
        for (int i = _spawnedPOIs.Count - 1; i >= 0; i--)
        {
            GameObject poiObj = _spawnedPOIs[i];
            if (poiObj == null) continue;

            POIBehaviour poiBehaviour = poiObj.GetComponent<POIBehaviour>();
            if (poiBehaviour == null) continue;

            double distMeters = GetHaversineDistance(playerLatLon, poiBehaviour.latLon);
            //Debug.Log($"Distance to POI {poiBehaviour.Id} = {distMeters} m");

            // If within collectionDistanceMeters, collect it
            if (distMeters < collectionDistanceMeters)
            {
                Debug.Log($"Collected POI {poiBehaviour.Id} at distance {distMeters} m");
                
                // (Optional) Score
                ScoreManager.Instance.AddPoints(10);

                // Remove the POI
                Destroy(poiObj);
                _spawnedPOIs.RemoveAt(i);
            }
        }
    }

    // --------------------------------------------------
    // 6. Haversine distance in meters
    // --------------------------------------------------
    private double GetHaversineDistance(Vector2d coord1, Vector2d coord2)
    {
        // Earth radius in meters
        const double R = 6371000.0;

        double lat1Rad = coord1.x * Mathf.Deg2Rad;
        double lon1Rad = coord1.y * Mathf.Deg2Rad;
        double lat2Rad = coord2.x * Mathf.Deg2Rad;
        double lon2Rad = coord2.y * Mathf.Deg2Rad;

        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;

        double a = Mathf.Sin((float)(dLat / 2.0)) * Mathf.Sin((float)(dLat / 2.0)) +
                   Mathf.Cos((float)lat1Rad) * Mathf.Cos((float)lat2Rad) *
                   Mathf.Sin((float)(dLon / 2.0)) * Mathf.Sin((float)(dLon / 2.0));

        double c = 2.0 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1.0 - a)));

        // Distance in meters
        double distance = R * c;
        return distance;
    }
}
