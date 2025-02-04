using UnityEngine;
using System.Collections.Generic;
using Mapbox.Utils;
using Mapbox.Unity.Map;

public class POIManager : MonoBehaviour
{
    [Header("JSON Input")]
    [SerializeField] private TextAsset poiJson;

    [Header("Map Reference")]
    [SerializeField] private AbstractMap map;

    [Header("POI Prefab")]
    [SerializeField] private GameObject poiPrefab;

    [Header("Distance Settings (for optional collection)")]
    [Tooltip("Distance in Unity world units to collect a POI")]
    [SerializeField] private float collectionDistance = 10f;

    // Data parsed from the JSON (OSMDataModel.cs types)
    private RootObject _poiData;
    private List<Element> _allPOIs = new List<Element>();

    // All instantiated POIs
    private List<GameObject> _spawnedPOIs = new List<GameObject>();

    // Reference to player transform
    private Transform _playerTransform;

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
            Debug.Log($"Parsed {_allPOIs.Count} POIs.");
        }
        else
        {
            Debug.LogError("Failed to parse JSON or no POI elements found.");
        }
    }

    // --------------------------------------------------
    // 2. Get player reference in Start
    // --------------------------------------------------
    private void Start()
    {
        // Find the player's transform (assuming your player is tagged "Player")
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("No GameObject tagged 'Player' found in scene.");
        }
    }

    // --------------------------------------------------
    // 3. Subscribe to map events
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
    // Called when the map has completed initialization
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

    // --------------------------------------------------
    // Spawn POIs once the map is ready
    // --------------------------------------------------
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

        // Instantiate a prefab for each POI
        foreach (var poi in _allPOIs)
        {
            Vector2d latLon = new Vector2d(poi.lat, poi.lon);
            Vector3 worldPos = map.GeoToWorldPosition(latLon);

            //Debug.Log($"Spawning POI at lat/lon ({latLon.x}, {latLon.y}) => worldPos {worldPos}");

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
    // This is crucial if you're using "Extend to Camera Bounds" or
    // a dynamic approach that re-centers the map.
    // We recalc their world positions so they stay pinned to lat/lon.
    private void Map_OnUpdated()
    {
        foreach (var poiObject in _spawnedPOIs)
        {
            if (poiObject == null) continue;
            POIBehaviour poiBehaviour = poiObject.GetComponent<POIBehaviour>();
            if (poiBehaviour == null) continue;

            // Recalculate
            Vector3 newPos = map.GeoToWorldPosition(poiBehaviour.latLon);
            poiObject.transform.position = newPos;
        }
    }

    // --------------------------------------------------
    // 5. (Optional) Distance-based collection each frame
    // --------------------------------------------------
    private void Update()
    {
        // If you want to allow collecting when the player is close:
        
        if (_playerTransform == null) return;

        for (int i = _spawnedPOIs.Count - 1; i >= 0; i--)
        {
            float distance = Vector3.Distance(_playerTransform.position, _spawnedPOIs[i].transform.position);
            if (distance < collectionDistance)
            {
                // (Optional) Add points
                ScoreManager.Instance.AddPoints(10);

                // Remove the POI
                Destroy(_spawnedPOIs[i]);
                _spawnedPOIs.RemoveAt(i);
            }
        }
        
    }
}
