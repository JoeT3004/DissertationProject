using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMarker : MonoBehaviour
{
    public string PlayerId { get; private set; }

    // Additional optional fields for storing stats
    [SerializeField] public int Health { get; private set; }
    [SerializeField] public int Level { get; private set; }

    public void Initialize(string playerId)
    {
        PlayerId = playerId;
    }

    // Optionally set stats if you want to display them
    public void SetStats(int health, int level)
    {
        Health = health;
        Level = level;

        // Example: color the base differently by level, or show text
        // E.g. a text mesh, UI canvas, or name label, etc.
        // Debug.Log($"[BaseMarker] Player {PlayerId} => Health={Health}, Level={Level}");
    }
}


