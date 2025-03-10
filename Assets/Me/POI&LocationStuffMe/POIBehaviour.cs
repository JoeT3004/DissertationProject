using UnityEngine;
using Mapbox.Utils;

/// <summary>
/// Attached to each spawned POI GameObject to store lat/lon (for repositioning) 
/// and additional metadata from the JSON (amenity, ref, etc.).
/// </summary>
public class POIBehaviour : MonoBehaviour
{
    public Vector2d latLon;
    public long Id;
    public string Amenity;
    public string Ref;
}
