using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Firebase.Database;
using Firebase.Extensions; // for ContinueWithOnMainThread

public class BaseManager : MonoBehaviour
{
    [Header("Map & Marker Setup")]
    [SerializeField] private AbstractMap map;             // Assign your Mapbox AbstractMap here
    [SerializeField] private GameObject baseMarkerPrefab; // Prefab for the base marker

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;      // Panel shown if no base is found
    [SerializeField] private Button placeBaseButton;      // Button to start placing a base (optional)

    private bool hasBase = false;
    private bool isPlacingBase = false;

    private string playerId;
    private Vector2d baseCoordinates;
    private GameObject currentBaseMarker;

    private void Awake()
    {
        if (map == null)
        {
            Debug.LogError("BaseManager: The 'map' reference is NOT assigned! " +
                           "Please assign an AbstractMap in the Inspector.");
        }
    }


    private void Start()
    {

        DebugCheckReferences(); // <--- Add this

        // Retrieve or create a local user ID
        playerId = RetrieveOrCreatePlayerId();

        // Fetch existing base data from Firebase
        FetchBaseFromFirebase();
    }

    private void DebugCheckReferences()
    {
        if (map == null)
            Debug.LogError("BaseManager: 'map' is null! Please assign it in the Inspector.");
        else
            Debug.Log("BaseManager: map is assigned to '" + map.gameObject.name + "'.");

        if (baseMarkerPrefab == null)
            Debug.LogError("BaseManager: 'baseMarkerPrefab' is null! Assign a valid prefab in the Inspector.");
        else
            Debug.Log("BaseManager: baseMarkerPrefab is assigned to '" + baseMarkerPrefab.name + "'.");
    }

    private void Update()
    {
        // Only check for input if we are in "placing base" mode
        if (!isPlacingBase) return;

        // 1. Touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Place a base on the "Began" phase
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 screenPos = touch.position;
                TryPlaceBaseAtScreenPosition(screenPos);
            }
        }
        // 2. Mouse input for desktop/Editor
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 screenPos = Input.mousePosition;
            TryPlaceBaseAtScreenPosition(screenPos);
        }
    }

    // LateUpdate ensures the marker is repositioned if the map moves (pan/zoom)
    private void LateUpdate()
    {
        if (hasBase && currentBaseMarker != null)
        {
            // Correct method: Convert geo coordinates to Unity world coordinates
            currentBaseMarker.transform.position = map.GeoToWorldPosition(baseCoordinates, true);
        }
    }


    /// <summary>
    /// Attempts to place the base at a given screen position (mouse/touch).
    /// </summary>
    private void TryPlaceBaseAtScreenPosition(Vector2 screenPos)
    {
        Vector2d latLon;
        bool success = ScreenPositionToLatLon(screenPos, out latLon);

        if (success)
        {
            ConfirmBaseLocation(latLon);
            isPlacingBase = false;  // Stop placing mode
            Debug.Log("Base placed at: " + latLon);
        }
        else
        {
            Debug.LogWarning("Failed to get lat/lon from screen position. " +
                             "Check that the map is at y=0 or adjust the plane accordingly.");
        }
    }

    /// <summary>
    /// Called by the "Place Base" button on the prompt panel.
    /// Allows the user to click/tap once to set their base.
    /// </summary>
    public void StartPlacingBase()
    {
        promptPanel.SetActive(false);
        isPlacingBase = true;
        Debug.Log("Now in base-placing mode. Tap/click the map to place your base.");
    }

    /// <summary>
    /// Finalizes the lat/lon choice, spawns the marker, and saves to Firebase.
    /// </summary>
    public void ConfirmBaseLocation(Vector2d location)
    {
        hasBase = true;
        baseCoordinates = location;

        // Spawn or move the marker
        PlaceBaseMarker(location);

        // Save to Firebase
        SaveBaseToFirebase(location);

        Debug.Log($"Base confirmed at lat: {location.x}, lon: {location.y}");
    }

    private void PlaceBaseMarker(Vector2d coords)
    {
        // Convert the lat/lon to a Unity world position
        Vector3 worldPos = map.GeoToWorldPosition(coords, true);

        // If there is no marker yet, instantiate it
        if (currentBaseMarker == null)
        {
            currentBaseMarker = Instantiate(baseMarkerPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            // If we already have a marker, just reposition it
            currentBaseMarker.transform.position = worldPos;
        }
    }


    private void SaveBaseToFirebase(Vector2d coords)
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("Firebase DB reference not ready. Cannot save base yet.");
            return;
        }

        FirebaseInit.DBReference.Child("bases").Child(playerId).Child("latitude").SetValueAsync(coords.x);
        FirebaseInit.DBReference.Child("bases").Child(playerId).Child("longitude").SetValueAsync(coords.y);

        Debug.Log("Base saved to Firebase at coords: " + coords);
    }

    /// <summary>
    /// Called when user presses the "Base" tab (e.g., via a TabBar button).
    /// Centers the map on the base if it exists.
    /// </summary>
    public void ShowBaseOnMap()
    {
        if (hasBase)
        {
            map.SetCenterLatitudeLongitude(baseCoordinates);
            map.UpdateMap();
        }
        else
        {
            promptPanel.SetActive(true);
        }
    }

    public void HidePromptPanel()
    {
        promptPanel.SetActive(false);
    }

    // -- Utility & Firebase methods below -----------------------------------

    private string RetrieveOrCreatePlayerId()
    {
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
            return newId;
        }
        else
        {
            return PlayerPrefs.GetString("playerId");
        }
    }

    private void FetchBaseFromFirebase()
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("Firebase not initialized yet. Delaying base fetch.");
            return;
        }

        FirebaseInit.DBReference.Child("bases").Child(playerId).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsFaulted && task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists && snapshot.HasChildren)
                    {
                        // We have a base
                        hasBase = true;

                        double lat = double.Parse(snapshot.Child("latitude").Value.ToString());
                        double lon = double.Parse(snapshot.Child("longitude").Value.ToString());
                        baseCoordinates = new Vector2d(lat, lon);

                        PlaceBaseMarker(baseCoordinates);
                    }
                    else
                    {
                        // No base found
                        hasBase = false;
                        promptPanel.SetActive(true);
                    }
                }
                else
                {
                    Debug.LogError("Error fetching base from Firebase.");
                }
            });
    }

    /// <summary>
    /// Converts a 2D screen position (mouse or touch) to lat/lon by projecting
    /// a ray onto a ground plane at y=0, then calling map.WorldToGeoPosition().
    /// 
    /// If your map is offset or rotated, update the plane's normal/position accordingly.
    /// </summary>
    private bool ScreenPositionToLatLon(Vector2 screenPos, out Vector2d latLon)
    {
        latLon = Vector2d.zero;

        // If your map is at y=0, using Vector3.up + Vector3.zero is fine:
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 hitPos = ray.GetPoint(distance);
            latLon = map.WorldToGeoPosition(hitPos);
            return true;
        }

        return false;
    }
}
