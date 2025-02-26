using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    // Add more fields if you need them from the JSON
}

