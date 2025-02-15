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
            Debug.LogWarning("Firebase not ready yet. Aborting.");
            return;
        }

        Debug.Log("[FakeBaseTester] Spawning fake bases...");

        // Optionally, recenter your map so you can see them in Game view
        // (Comment out if you do NOT want to override your map center.)
        RecenterMap();

        // Generate random positions around (centerLatitude, centerLongitude)
        for (int i = 0; i < numberOfFakeBases; i++)
        {
            // Construct a "fake" playerId: e.g. "TestPlayer_0", "TestPlayer_1", etc.
            string fakePlayerId = fakePlayerPrefix + i;

            // Generate random lat/lon near the center
            double lat = centerLatitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);
            double lon = centerLongitude + Random.Range(-(float)latLonRandomRange, (float)latLonRandomRange);

            // Write these coords to: bases/{fakePlayerId}/latitude, /longitude
            var db = FirebaseInit.DBReference;
            db.Child("bases").Child(fakePlayerId).Child("latitude").SetValueAsync(lat);
            db.Child("bases").Child(fakePlayerId).Child("longitude").SetValueAsync(lon);
        }

        Debug.Log($"[FakeBaseTester] Finished creating {numberOfFakeBases} fake bases under prefix '{fakePlayerPrefix}'.");
    }

    [ContextMenu("RemoveFakeBases")]
    public void RemoveFakeBases()
    {
        if (!FirebaseInit.IsFirebaseReady)
        {
            Debug.LogWarning("Firebase not ready yet. Aborting.");
            return;
        }

        Debug.Log("[FakeBaseTester] Removing all fake bases...");

        // For each index, remove the child node
        for (int i = 0; i < numberOfFakeBases; i++)
        {
            string fakePlayerId = fakePlayerPrefix + i;
            FirebaseInit.DBReference
                .Child("bases")
                .Child(fakePlayerId)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogWarning($"[FakeBaseTester] Error removing {fakePlayerId}");
                    else
                        Debug.Log($"[FakeBaseTester] Removed base for {fakePlayerId}");
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
