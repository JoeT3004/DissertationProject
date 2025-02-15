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

    // Store lat/lon for each player so we can recalc positions in LateUpdate
    private Dictionary<string, Vector2d> markerCoordinates = new Dictionary<string, Vector2d>();

    private IEnumerator Start()
    {
        // Wait until Firebase is ready (set by your FirebaseInit)
        while (!FirebaseInit.IsFirebaseReady)
        {
            yield return null;
        }

        Debug.Log("[AllBasesManager] Firebase is ready. Setting up listener for all bases...");

        // Attach a listener so we get updates whenever the 'bases' node changes
        FirebaseInit.DBReference.Child("bases").ValueChanged += HandleBasesValueChanged;
    }

    private void OnDestroy()
    {
        // Clean up the event listener if this object is destroyed
        if (FirebaseInit.DBReference != null)
        {
            FirebaseInit.DBReference.Child("bases").ValueChanged -= HandleBasesValueChanged;
        }
    }

    private void HandleBasesValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (map == null)
        {
            Debug.LogError("[AllBasesManager] AbstractMap reference is null! Cannot place markers.");
            return;
        }
        if (otherUserMarkerPrefab == null)
        {
            Debug.LogError("[AllBasesManager] 'otherUserMarkerPrefab' is null! Cannot spawn markers.");
            return;
        }

        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllBasesManager] Listener for bases failed: " + e.DatabaseError.Message);
            return;
        }

        // This snapshot contains the entire "bases" node
        DataSnapshot snapshot = e.Snapshot;
        if (!snapshot.Exists || !snapshot.HasChildren)
        {
            Debug.Log("[AllBasesManager] No bases found in Firebase. Clearing markers.");
            ClearAllMarkers();
            return;
        }

        Debug.Log("[AllBasesManager] Bases data changed! Rebuilding markers...");

        // 1) Clear existing markers & coords
        ClearAllMarkers();

        // 2) Recreate them from the snapshot
        foreach (var child in snapshot.Children)
        {
            string thisPlayerId = child.Key;
            Debug.Log($"[AllBasesManager] Found base for playerId = {thisPlayerId}");

            // --- Ignore local player's base; BaseManager handles that separately ---
            if (thisPlayerId == PlayerPrefs.GetString("playerId"))
            {
                // Skip spawning a marker for our own base
                continue;
            }

            // The rest spawns other user markers.
            if (child == null) continue; // safety check

            // If your data structure is always: bases/{playerId}/latitude & .../longitude
            if (!child.HasChild("latitude") || !child.HasChild("longitude"))
            {
                Debug.LogWarning($"[AllBasesManager] Skipping base with missing lat/long for playerId = {thisPlayerId}");
                continue;
            }

            object latVal = child.Child("latitude").Value;
            object lonVal = child.Child("longitude").Value;

            if (latVal == null || lonVal == null)
            {
                Debug.LogWarning($"[AllBasesManager] Skipping base because latVal/lonVal is null for {thisPlayerId}");
                continue;
            }

            // Convert to double
            double lat = double.Parse(latVal.ToString());
            double lon = double.Parse(lonVal.ToString());
            Vector2d coords = new Vector2d(lat, lon);

            // Instantiate the marker at the correct world position (initially)
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);
            GameObject newMarker = Instantiate(otherUserMarkerPrefab, worldPos, Quaternion.identity);

            // If there's a BaseMarker script, set the playerId
            BaseMarker markerScript = newMarker.GetComponent<BaseMarker>();
            if (markerScript != null)
            {
                markerScript.Initialize(thisPlayerId);
            }

            // Store references so we can reposition later
            spawnedMarkers[thisPlayerId] = newMarker;
            markerCoordinates[thisPlayerId] = coords;
        }

        Debug.Log($"[AllBasesManager] Successfully placed {spawnedMarkers.Count} other-user base(s).");
    }

    /// <summary>
    /// In LateUpdate, re-position each spawned marker to follow the map
    /// (in case the map is panning/zooming).
    /// </summary>
    private void LateUpdate()
    {
        if (map == null) return;

        foreach (var kvp in spawnedMarkers)
        {
            string playerId = kvp.Key;
            GameObject marker = kvp.Value;

            // Lookup lat/lon we stored
            if (!markerCoordinates.TryGetValue(playerId, out Vector2d coords))
                continue;

            // Convert lat/lon -> new world pos for this frame
            Vector3 newPos = map.GeoToWorldPosition(coords, true);

            marker.transform.position = newPos;
        }
    }

    private void ClearAllMarkers()
    {
        // Destroy all existing markers
        foreach (var kvp in spawnedMarkers)
        {
            Destroy(kvp.Value);
        }
        spawnedMarkers.Clear();
        markerCoordinates.Clear();
    }
}
