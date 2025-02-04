using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mapbox.Utils;

public class POIBehaviour : MonoBehaviour
{
    // This will store the latitude/longitude for this POI.
    // We'll use it to recalculate world positions whenever the map moves or re-centers.
    public Vector2d latLon;

    // Additional metadata from the JSON (optional)
    public long Id;
    public string Amenity;
    public string Ref;
}


