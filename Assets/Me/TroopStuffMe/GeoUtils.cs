using UnityEngine;
using Mapbox.Utils;

/// <summary>
/// Utility methods for geographical calculations (e.g., distances).
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Calculates the Haversine distance (in meters) between two lat/lon points.
    /// This formula approximates the curvature of Earth for accurate distance.
    /// </summary>
    /// <param name="fromLatLon">Starting lat/lon.</param>
    /// <param name="toLatLon">Ending lat/lon.</param>
    /// <returns>Distance in meters along Earth's surface.</returns>
    public static double HaversineDistance(Vector2d fromLatLon, Vector2d toLatLon)
    {
        // Earth radius in meters
        const double R = 6371000;

        // Convert degrees to radians
        double lat1 = fromLatLon.x * Mathf.Deg2Rad;
        double lon1 = fromLatLon.y * Mathf.Deg2Rad;
        double lat2 = toLatLon.x * Mathf.Deg2Rad;
        double lon2 = toLatLon.y * Mathf.Deg2Rad;

        // Differences
        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        // Haversine formula
        double a = Mathf.Sin((float)dLat / 2f) * Mathf.Sin((float)dLat / 2f) +
                   Mathf.Cos((float)lat1) * Mathf.Cos((float)lat2) *
                   Mathf.Sin((float)dLon / 2f) * Mathf.Sin((float)dLon / 2f);

        double c = 2f * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1f - a)));

        // Distance
        double distance = R * c;
        return distance;
    }
}
