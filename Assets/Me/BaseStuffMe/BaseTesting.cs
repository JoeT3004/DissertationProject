using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Spawns and removes "fake" bases in Firebase for testing.
/// Useful if you want some enemy bases to tap on without needing real players.
/// </summary>
public class BaseTesting : MonoBehaviour
{
    [Header("Map & Firebase")]
    [SerializeField] private AbstractMap map;

    [Header("Fake Base Config")]
    [SerializeField] private int numberOfFakeBases = 3;

    [Tooltip("Center lat/lon around which to distribute fake bases")]
    [SerializeField] private double centerLatitude = 51.5;
    [SerializeField] private double centerLongitude = -0.12;

    [Tooltip("Max random offset in degrees from the center lat/lon")]
    [SerializeField] private double latLonRandomRange = 0.02;

    [Tooltip("A prefix to use for your fake playerIds")]
    [SerializeField] private string fakePlayerPrefix = "TestPlayer_";

    /// <summary>
    /// Creates N fake players in Firebase, each with:
    ///   - A 'score' node (50 points),
    ///   - A 'base' node with random lat/lon near 'centerLatitude, centerLongitude',
    ///   - health=100, level=1, and username "FakeBase_i".
    /// These bases appear as "enemy bases" in AllBasesManager if you're not the local user.
    /// 
    /// This is invoked either via a context menu or from the editor/inspector button.
    /// </summary>
    [ContextMenu("SpawnFakeBases")]
    public void SpawnFakeBases()
    {
        // Check if Firebase is ready
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("[BaseTesting] Firebase not ready yet. Aborting spawn.");
            return;
        }

        Debug.Log($"[BaseTesting] Spawning {numberOfFakeBases} fake bases...");

        // Optionally re-center the map to the test area for convenience
        RecenterMap();

        DatabaseReference db = FirebaseInit.DBReference;

        // Create each fake base
        for (int i = 0; i < numberOfFakeBases; i++)
        {
            string fakePlayerId = fakePlayerPrefix + i;

            // random lat/lon offset
            double lat = centerLatitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);
            double lon = centerLongitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);

            // 1) set a default score
            db.Child("users")
              .Child(fakePlayerId)
              .Child("score")
              .SetValueAsync(50);

            // 2) create a "base" node
            DatabaseReference baseRef = db.Child("users")
                                          .Child(fakePlayerId)
                                          .Child("base");

            baseRef.Child("latitude").SetValueAsync(lat);
            baseRef.Child("longitude").SetValueAsync(lon);
            baseRef.Child("health").SetValueAsync(100);
            baseRef.Child("level").SetValueAsync(1);

            // e.g. "FakeBase_0"
            string fakeUsername = "FakeBase_" + i;
            baseRef.Child("username").SetValueAsync(fakeUsername);

            Debug.Log($"[BaseTesting] Created fake base for '{fakePlayerId}' named '{fakeUsername}', lat={lat}, lon={lon}.");
        }

        Debug.Log($"[BaseTesting] Finished creating {numberOfFakeBases} fake bases under prefix '{fakePlayerPrefix}'.");
    }

    /// <summary>
    /// Removes the same N fake bases from Firebase that match the 'fakePlayerPrefix' 
    /// and 'numberOfFakeBases' index range.
    /// </summary>
    [ContextMenu("RemoveFakeBases")]
    public void RemoveFakeBases()
    {
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("[BaseTesting] Firebase not ready yet. Aborting removal.");
            return;
        }

        Debug.Log("[BaseTesting] Removing all fake bases...");

        // For each base, remove that entire user node from "users"
        for (int i = 0; i < numberOfFakeBases; i++)
        {
            string fakePlayerId = fakePlayerPrefix + i;
            FirebaseInit.DBReference
                .Child("users")
                .Child(fakePlayerId)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogWarning($"[BaseTesting] Error removing user '{fakePlayerId}'.");
                    }
                    else
                    {
                        Debug.Log($"[BaseTesting] Successfully removed user '{fakePlayerId}'.");
                    }
                });
        }
    }

    /// <summary>
    /// Optionally re-center the map to (centerLatitude, centerLongitude). 
    /// This helps see your new test bases more easily.
    /// </summary>
    private void RecenterMap()
    {
        if (map == null)
        {
            Debug.LogWarning("[BaseTesting] No Map assigned, skipping re-center.");
            return;
        }

        Vector2d centerCoords = new Vector2d(centerLatitude, centerLongitude);
        Debug.Log($"[BaseTesting] Re-centering map to lat/lon: {centerCoords}");
        map.SetCenterLatitudeLongitude(centerCoords);
        map.UpdateMap();
    }
}
