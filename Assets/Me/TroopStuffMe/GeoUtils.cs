using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils; // for Vector2d

public static class GeoUtils
{
    /// <summary>
    /// Returns the distance in meters between two lat/lon points.
    /// </summary>
    public static double HaversineDistance(Vector2d fromLatLon, Vector2d toLatLon)
    {
        // Earth radius in meters
        const double R = 6371000;
        double lat1 = fromLatLon.x * Mathf.Deg2Rad;
        double lon1 = fromLatLon.y * Mathf.Deg2Rad;
        double lat2 = toLatLon.x * Mathf.Deg2Rad;
        double lon2 = toLatLon.y * Mathf.Deg2Rad;

        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        double a = Mathf.Sin((float)dLat / 2f) * Mathf.Sin((float)dLat / 2f) +
                   Mathf.Cos((float)lat1) * Mathf.Cos((float)lat2) *
                   Mathf.Sin((float)dLon / 2f) * Mathf.Sin((float)dLon / 2f);
        double c = 2f * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1f - a)));

        double distance = R * c;
        return distance; // in meters
    }
}
