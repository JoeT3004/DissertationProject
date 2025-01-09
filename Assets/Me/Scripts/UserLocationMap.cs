using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using UnityEngine;

public class UserLocationMap : MonoBehaviour
{
    public AbstractMap map;

    void Start()
    {
        if (map == null)
        {
            Debug.LogError("AbstractMap is not assigned!");
            return;
        }

        StartCoroutine(UpdateUserLocation());
    }

    System.Collections.IEnumerator UpdateUserLocation()
    {
        // Ensure location services are enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("Location services are not enabled.");
            yield break;
        }

        // Start location services
        Input.location.Start();
        while (Input.location.status == LocationServiceStatus.Initializing && Input.location.status != LocationServiceStatus.Failed)
        {
            yield return new WaitForSeconds(1);
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Failed to retrieve location.");
            yield break;
        }

        // Update map center
        var userLatitude = Input.location.lastData.latitude;
        var userLongitude = Input.location.lastData.longitude;
        map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(userLatitude, userLongitude));
        map.UpdateMap();
    }
}
