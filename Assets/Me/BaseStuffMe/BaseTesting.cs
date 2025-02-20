using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

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

    [ContextMenu("SpawnFakeBases")]
    public void SpawnFakeBases()
    {
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("[FakeBaseTester] Firebase not ready yet. Aborting spawn.");
            return;
        }

        Debug.Log("[FakeBaseTester] Spawning fake bases...");

        // Optional: re-center the map so you can see them in the Scene view
        RecenterMap();

        DatabaseReference db = FirebaseInit.DBReference;

        // Generate random positions and create each fake user + base
        for (int i = 0; i < numberOfFakeBases; i++)
        {
            // e.g. "TestPlayer_0"
            string fakePlayerId = fakePlayerPrefix + i;

            double lat = centerLatitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);
            double lon = centerLongitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);

            // Write user data to: users/{fakePlayerId}/score
            db.Child("users")
              .Child(fakePlayerId)
              .Child("score")
              .SetValueAsync(50);

            // Write base data to: users/{fakePlayerId}/base/...
            DatabaseReference baseRef = db.Child("users")
                                          .Child(fakePlayerId)
                                          .Child("base");

            baseRef.Child("latitude").SetValueAsync(lat);
            baseRef.Child("longitude").SetValueAsync(lon);
            baseRef.Child("health").SetValueAsync(100);
            baseRef.Child("level").SetValueAsync(1);

            // Give each base a fake username
            string fakeUsername = "FakeBase_" + i;
            baseRef.Child("username").SetValueAsync(fakeUsername);

            Debug.Log($"[FakeBaseTester] Created fake base for '{fakePlayerId}' at lat={lat}, lon={lon}");
        }

        Debug.Log($"[FakeBaseTester] Finished creating {numberOfFakeBases} fake bases under prefix '{fakePlayerPrefix}'.");
    }

    [ContextMenu("RemoveFakeBases")]
    public void RemoveFakeBases()
    {
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("[FakeBaseTester] Firebase not ready yet. Aborting removal.");
            return;
        }

        Debug.Log("[FakeBaseTester] Removing all fake bases...");

        // Remove each fake user's entire node: "users/{fakePlayerId}"
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
                        Debug.LogWarning($"[FakeBaseTester] Error removing user '{fakePlayerId}'.");
                    else
                        Debug.Log($"[FakeBaseTester] Successfully removed user '{fakePlayerId}'.");
                });
        }
    }

    private void RecenterMap()
    {
        if (map == null) return;

        Vector2d centerCoords = new Vector2d(centerLatitude, centerLongitude);
        Debug.Log($"[FakeBaseTester] Re-centering map to lat/lon: {centerCoords}");
        map.SetCenterLatitudeLongitude(centerCoords);
        map.UpdateMap();
    }
}
