using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;


/// <summary>
/// Spawns 'fake' enemy bases on Firebase so you can test tapping them in-game.
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
    /// Creates N fake players, each with a 'base' node that includes lat/lon, health, level, username, 
    /// plus a 'score' node. These appear as "enemy bases" in AllBasesManager.
    /// </summary>
    [ContextMenu("SpawnFakeBases")]
    public void SpawnFakeBases()
    {
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("[BaseTesting] Firebase not ready yet. Aborting spawn.");
            return;
        }

        Debug.Log($"[BaseTesting] Spawning {numberOfFakeBases} fake bases...");

        // Optionally re-center the map around the test area
        RecenterMap();

        DatabaseReference db = FirebaseInit.DBReference;

        for (int i = 0; i < numberOfFakeBases; i++)
        {
            // Each fake player gets a unique ID like "TestPlayer_0"
            string fakePlayerId = fakePlayerPrefix + i;

            // Randomly offset lat/lon
            double lat = centerLatitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);
            double lon = centerLongitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);

            // 1) Set a score => "users/{fakePlayerId}/score"
            db.Child("users")
              .Child(fakePlayerId)
              .Child("score")
              .SetValueAsync(50);

            // 2) Create a "base" node => "users/{fakePlayerId}/base"
            DatabaseReference baseRef = db.Child("users")
                                          .Child(fakePlayerId)
                                          .Child("base");

            baseRef.Child("latitude").SetValueAsync(lat);
            baseRef.Child("longitude").SetValueAsync(lon);
            baseRef.Child("health").SetValueAsync(100);
            baseRef.Child("level").SetValueAsync(1);

            // Each base has a unique username => "FakeBase_0", etc.
            string fakeUsername = "FakeBase_" + i;
            baseRef.Child("username").SetValueAsync(fakeUsername);

            Debug.Log($"[BaseTesting] Created fake base for '{fakePlayerId}' named '{fakeUsername}', lat={lat}, lon={lon}.");
        }

        Debug.Log($"[BaseTesting] Finished creating {numberOfFakeBases} fake bases under prefix '{fakePlayerPrefix}'.");
    }

    /// <summary>
    /// Removes all the fake bases that were spawned using the same prefix and count.
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

        for (int i = 0; i < numberOfFakeBases; i++)
        {
            string fakePlayerId = fakePlayerPrefix + i;
            FirebaseInit.DBReference
                .Child("users")
                .Child(fakePlayerId)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogWarning($"[BaseTesting] Error removing user '{fakePlayerId}'.");
                    else
                        Debug.Log($"[BaseTesting] Successfully removed user '{fakePlayerId}'.");
                });
        }
    }

    /// <summary>
    /// Optionally recenter the map to the test area so you can see the newly spawned markers more easily.
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
