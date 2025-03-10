using System;

/// <summary>
/// Data structures for deserializing the OSM-based JSON for POIs. 
/// Contains a RootObject with "elements", each an Element with lat, lon, etc.
/// </summary>
[System.Serializable]
public class RootObject
{
    public float version;
    public string generator;
    public Osm3s osm3s;
    public Element[] elements;
}

[System.Serializable]
public class Osm3s
{
    public string timestamp_osm_base;
    public string timestamp_areas_base;
    public string copyright;
}

[System.Serializable]
public class Element
{
    public string type;
    public long id;
    public float lat;
    public float lon;
    public Tags tags;
}

[System.Serializable]
public class Tags
{
    public string amenity;
    public string @ref;
    public string brand;
    // Add more fields if needed
}
