using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Manages the spawning of "other users'" bases on the map by listening to "users" data in Firebase.
/// Skips the local user (doesn't show an "enemy" marker for them).
/// Stores usernames and coordinates for retrieval (e.g., AttackManager).
/// </summary>
public class AllBasesManager : MonoBehaviour
{
    public static AllBasesManager Instance { get; private set; }

    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject otherUserMarkerPrefab;

    // Stores each user's base location by userId
    private Dictionary<string, Vector2d> markerCoordinates = new Dictionary<string, Vector2d>();

    // Stores usernames by userId
    private Dictionary<string, string> userNames = new Dictionary<string, string>();

    // Tracks each spawned "enemy" marker for other users
    private Dictionary<string, GameObject> spawnedMarkers = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Waits for Firebase, then subscribes to changes under "users".
    /// </summary>
    private IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        if (!map)
        {
            map = FindObjectOfType<AbstractMap>();
            if (!map)
            {
                Debug.LogError("[AllBasesManager] No AbstractMap found in scene!");
                yield break;
            }
        }

        FirebaseInit.DBReference
            .Child("users")
            .ValueChanged += HandleUsersValueChanged;
    }

    private void OnDestroy()
    {
        if (FirebaseInit.DBReference != null)
        {
            FirebaseInit.DBReference.Child("users").ValueChanged -= HandleUsersValueChanged;
        }
    }

    /// <summary>
    /// Called whenever the "users" node in Firebase changes. 
    /// Rebuilds markers for all bases in the scene, skipping the local user's base marker.
    /// </summary>
    private void HandleUsersValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (!map)
        {
            Debug.LogError("[AllBasesManager] AbstractMap is null!");
            return;
        }
        if (!otherUserMarkerPrefab)
        {
            Debug.LogError("[AllBasesManager] 'otherUserMarkerPrefab' is null!");
            return;
        }
        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllBasesManager] DB error: " + e.DatabaseError.Message);
            return;
        }

        var snapshot = e.Snapshot;
        if (!snapshot.Exists)
        {
            Debug.Log("[AllBasesManager] 'users' node is empty. Clearing markers.");
            ClearAllMarkers();
            return;
        }

        Debug.Log("[AllBasesManager] Rebuilding markers from 'users' snapshot...");
        ClearAllMarkers(); // remove old

        string localUserId = PlayerPrefs.GetString("playerId");

        // For each user in 'users'
        foreach (var userSnap in snapshot.Children)
        {
            string userId = userSnap.Key;

            // Check for "base" node
            var baseSnap = userSnap.Child("base");
            if (!baseSnap.Exists) continue; // no base

            if (!baseSnap.HasChild("latitude") || !baseSnap.HasChild("longitude"))
            {
                Debug.LogWarning($"[AllBasesManager] Skipping user '{userId}' - no lat/long found.");
                continue;
            }

            double lat = double.Parse(baseSnap.Child("latitude").Value.ToString());
            double lon = double.Parse(baseSnap.Child("longitude").Value.ToString());
            Vector2d coords = new Vector2d(lat, lon);

            // Retrieve username, or "Unknown"
            string username = baseSnap.HasChild("username")
                ? baseSnap.Child("username").Value.ToString()
                : "Unknown";

            // Store in dictionaries
            userNames[userId] = username;
            markerCoordinates[userId] = coords;

            Debug.Log($"[AllBasesManager] Stored user='{userId}', username='{username}', lat={lat}, lon={lon}.");

            // Skip local user's base => don't spawn an "enemy" marker
            if (userId == localUserId) continue;

            // For an enemy user, proceed to spawn marker
            int enemyHealth = baseSnap.HasChild("health")
                ? int.Parse(baseSnap.Child("health").Value.ToString())
                : 100;
            int enemyLevel = baseSnap.HasChild("level")
                ? int.Parse(baseSnap.Child("level").Value.ToString())
                : 1;

            Vector3 worldPos = map.GeoToWorldPosition(coords, true);
            GameObject markerGO = Instantiate(otherUserMarkerPrefab, worldPos, Quaternion.identity);

            var markerScript = markerGO.GetComponent<BaseMarker>();
            if (markerScript != null)
            {
                markerScript.Initialize(userId, username, enemyHealth, enemyLevel);
            }

            spawnedMarkers[userId] = markerGO;
        }

        Debug.Log($"[AllBasesManager] Done building markers. userNames.Count={userNames.Count}");
    }

    /// <summary>
    /// Called after all Update() calls, repositions markers if the map or camera has moved.
    /// </summary>
    private void LateUpdate()
    {
        // Update positions as map moves/zooms
        foreach (var kvp in spawnedMarkers)
        {
            string userId = kvp.Key;
            GameObject marker = kvp.Value;

            if (!markerCoordinates.TryGetValue(userId, out Vector2d coords))
                continue;

            Vector3 newPos = map.GeoToWorldPosition(coords, true);
            marker.transform.position = newPos;
        }
    }

    /// <summary>
    /// Destroys all "enemy base" markers and clears dictionaries.
    /// </summary>
    private void ClearAllMarkers()
    {
        foreach (var kvp in spawnedMarkers)
        {
            Destroy(kvp.Value);
        }
        spawnedMarkers.Clear();
        markerCoordinates.Clear();
        userNames.Clear();
    }

    /// <summary>
    /// Returns the base coordinates for a user, or null if not found.
    /// </summary>
    public Vector2d? GetBaseCoordinates(string userId)
    {
        if (markerCoordinates.TryGetValue(userId, out Vector2d coords))
            return coords;
        return null;
    }

    /// <summary>
    /// Returns the username for the given user, or "Unknown" if not found or missing in DB.
    /// </summary>
    public string GetUsernameOfUser(string userId)
    {
        if (userNames.TryGetValue(userId, out string uname))
        {
            return uname;
        }
        return "Unknown";
    }
}
