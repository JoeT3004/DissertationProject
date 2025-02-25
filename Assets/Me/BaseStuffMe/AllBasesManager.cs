using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class AllBasesManager : MonoBehaviour
{
    public static AllBasesManager Instance { get; private set; }

    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject otherUserMarkerPrefab;

    // Positions of each user's base
    private Dictionary<string, Vector2d> markerCoordinates = new Dictionary<string, Vector2d>();

    // Dictionary for storing each user's username
    private Dictionary<string, string> userNames = new Dictionary<string, string>();

    // For each user, a spawned "enemy" marker (non-local user)
    private Dictionary<string, GameObject> spawnedMarkers = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait until Firebase is ready
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

        // Listen for changes under "users"
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

        // Clear old markers first
        ClearAllMarkers();

        // Identify local user ID (to skip spawning a marker for ourselves)
        string localUserId = PlayerPrefs.GetString("playerId");

        // For each user in 'users'
        foreach (var userSnap in snapshot.Children)
        {
            string userId = userSnap.Key;

            // Attempt to read their "base" node
            var baseSnap = userSnap.Child("base");
            if (!baseSnap.Exists)
            {
                // This user has no base
                continue;
            }

            // Make sure lat/lon exist
            if (!baseSnap.HasChild("latitude") || !baseSnap.HasChild("longitude"))
            {
                Debug.LogWarning($"[AllBasesManager] Skipping user '{userId}' - no lat/long found.");
                continue;
            }

            double lat = double.Parse(baseSnap.Child("latitude").Value.ToString());
            double lon = double.Parse(baseSnap.Child("longitude").Value.ToString());
            Vector2d coords = new Vector2d(lat, lon);

            // Username
            string username = baseSnap.HasChild("username")
                ? baseSnap.Child("username").Value.ToString()
                : "Unknown";

            // Always store them in dictionaries so AttackManager can retrieve user’s name
            userNames[userId] = username;
            markerCoordinates[userId] = coords;

            Debug.Log($"[AllBasesManager] Stored user='{userId}', username='{username}', lat={lat}, lon={lon}.");

            // Decide if we spawn a marker for this user
            // We skip the local user, so we only show "enemy" markers
            if (userId == localUserId)
            {
                // Local user => skip spawning 'enemy' marker
                continue;
            }

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
    /// Returns the username for the given user, or "Unknown" if not found or not in DB.
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
