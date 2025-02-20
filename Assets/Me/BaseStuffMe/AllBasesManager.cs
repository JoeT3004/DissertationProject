using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class AllBasesManager : MonoBehaviour
{
    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject otherUserMarkerPrefab;

    // Keep references to spawned markers so we can remove or update them
    private Dictionary<string, GameObject> spawnedMarkers = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector2d> markerCoordinates = new Dictionary<string, Vector2d>();

    private IEnumerator Start()
    {
        // Wait until Firebase is ready
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        // Fallback if map not assigned
        if (!map)
        {
            map = FindObjectOfType<AbstractMap>();
            if (!map)
            {
                Debug.LogError("[AllBasesManager] No AbstractMap found in the scene!");
                yield break;
            }
        }

        Debug.Log("[AllBasesManager] Firebase ready, setting up listener for 'users' node.");

        // Attach a listener to the entire 'users' node
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
            Debug.LogError("[AllBasesManager] AbstractMap reference is null!");
            return;
        }
        if (!otherUserMarkerPrefab)
        {
            Debug.LogError("[AllBasesManager] 'otherUserMarkerPrefab' is null!");
            return;
        }
        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllBasesManager] Listener for 'users' failed: " + e.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = e.Snapshot;
        if (!snapshot.Exists || !snapshot.HasChildren)
        {
            Debug.Log("[AllBasesManager] No users found. Clearing markers.");
            ClearAllMarkers();
            return;
        }

        Debug.Log("[AllBasesManager] 'users' data changed! Rebuilding markers...");
        ClearAllMarkers();

        // For each user (child), see if they have a 'base' subnode
        foreach (var userSnapshot in snapshot.Children)
        {
            string userId = userSnapshot.Key;
            if (userId == PlayerPrefs.GetString("playerId"))
            {
                // Skip ourselves
                continue;
            }

            var baseSnap = userSnapshot.Child("base");
            if (!baseSnap.Exists)
            {
                // This user doesn't have a base
                continue;
            }

            if (!baseSnap.HasChild("latitude") || !baseSnap.HasChild("longitude"))
            {
                Debug.LogWarning($"[AllBasesManager] Missing lat/long for user = {userId}");
                continue;
            }

            double lat = double.Parse(baseSnap.Child("latitude").Value.ToString());
            double lon = double.Parse(baseSnap.Child("longitude").Value.ToString());
            Vector2d coords = new Vector2d(lat, lon);

            // Optional: read other fields like health, level, username
            string username = baseSnap.HasChild("username") ? baseSnap.Child("username").Value.ToString() : "Unknown";
            int enemyHealth = baseSnap.HasChild("health") ? int.Parse(baseSnap.Child("health").Value.ToString()) : 100;
            int enemyLevel = baseSnap.HasChild("level") ? int.Parse(baseSnap.Child("level").Value.ToString()) : 1;

            // Instantiate marker
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);
            GameObject newMarker = Instantiate(otherUserMarkerPrefab, worldPos, Quaternion.identity);

            BaseMarker markerScript = newMarker.GetComponent<BaseMarker>();
            if (markerScript != null)
            {
                markerScript.Initialize(userId, username, enemyHealth, enemyLevel);
            }

            spawnedMarkers[userId] = newMarker;
            markerCoordinates[userId] = coords;
        }

        Debug.Log($"[AllBasesManager] Placed {spawnedMarkers.Count} markers for other users.");
    }

    private void LateUpdate()
    {
        if (!map) return;

        // Re-position each spawned marker in case the map has panned/zoomed
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
    }
}
