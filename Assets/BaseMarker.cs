using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMarker : MonoBehaviour
{
    public string PlayerId { get; private set; }

    // We'll call this once we know which user owns the marker
    public void Initialize(string playerId)
    {
        PlayerId = playerId;
        // If you want to visually differentiate your own base from others, 
        // you could change the color or label here.
        // e.g., GetComponent<Renderer>().material.color = Color.blue;
    }
}

