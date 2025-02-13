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

    // We'll keep references to spawned markers so we can remove or update them if needed
    private Dictionary<string, GameObject> spawnedMarkers = new Dictionary<string, GameObject>();

    private IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
        {
            yield return null;
        }

        Debug.Log("Firebase is ready. Setting up listener for all bases...");

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
            Debug.LogError("Listener for bases failed: " + e.DatabaseError.Message);
            return;
        }

        // This snapshot contains the entire "bases" node
        DataSnapshot snapshot = e.Snapshot;
        if (!snapshot.Exists || !snapshot.HasChildren)
        {
            Debug.Log("No bases found in Firebase. Clearing markers.");
            ClearAllMarkers();
            return;
        }

        Debug.Log("Bases data changed! Rebuilding markers...");

        // 1) Clear existing markers
        ClearAllMarkers();

        // 2) Recreate them from the snapshot
        foreach (var child in snapshot.Children)
        {
            string thisPlayerId = child.Key;
            Debug.Log("[AllBasesManager] Found base for playerId = " + thisPlayerId);


            // --- ADD THIS CHECK to ignore local player's base, because BaseManager handles it ---
            if (thisPlayerId == PlayerPrefs.GetString("playerId"))
            {
                // Skip spawning a marker for our own base
                continue;
            }

            // The rest spawns other user markers..
            if (child == null) continue; // safety check

            // If your data structure is always "bases/{playerId}/latitude" & "bases/{playerId}/longitude"
            if (!child.HasChild("latitude") || !child.HasChild("longitude"))
            {
                Debug.LogWarning("Skipping base with missing lat/long for playerId = " + child.Key);
                continue;
            }

            object latVal = child.Child("latitude").Value;
            object lonVal = child.Child("longitude").Value;

            if (latVal == null || lonVal == null)
            {
                Debug.LogWarning("Skipping base because latVal/lonVal is null for " + child.Key);
                continue;
            }

            double lat = double.Parse(latVal.ToString());
            double lon = double.Parse(lonVal.ToString());

            // ...
            


            Vector2d coords = new Vector2d(lat, lon);
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);

            GameObject newMarker = Instantiate(otherUserMarkerPrefab, worldPos, Quaternion.identity);
            BaseMarker markerScript = newMarker.GetComponent<BaseMarker>();
            if (markerScript != null)
            {
                markerScript.Initialize(thisPlayerId);
            }

            spawnedMarkers[thisPlayerId] = newMarker;
        }


        // Show how many bases were placed
        Debug.Log($"Successfully placed {spawnedMarkers.Count} bases.");
    }

    private void ClearAllMarkers()
    {
        foreach (var kvp in spawnedMarkers)
        {
            Destroy(kvp.Value);
        }
        spawnedMarkers.Clear();
    }
}
