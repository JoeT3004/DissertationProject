using UnityEngine;

/// <summary>
/// Represents a single base marker in the world (enemy or local).
/// Holds references to a BaseUIController for showing health and level.
/// </summary>
public class BaseMarker : MonoBehaviour
{
    public string PlayerId { get; private set; }
    public string Username { get; private set; }
    public int Health { get; private set; }
    public int Level { get; private set; }

    private BaseUIController uiController;

    private void Awake()
    {
        uiController = GetComponentInChildren<BaseUIController>();
        if (uiController == null)
        {
            Debug.LogWarning("BaseUIController not found in children of BaseMarker.");
        }
    }

    /// <summary>
    /// Initializes the base marker with the provided data, sets up the UI, etc.
    /// </summary>
    public void Initialize(string playerId, string username, int health, int level)
    {
        PlayerId = playerId;
        Username = username;
        Health = health;
        Level = level;

        if (uiController != null)
        {
            // Calculate max health based on level (level * 100)
            int computedMaxHealth = level * 100;
            uiController.Initialize(username, health, computedMaxHealth);
        }
    }

    /// <summary>
    /// Updates the UI with the new health and level values. 
    /// </summary>
    public void UpdateStats(int health, int level)
    {
        Health = health;
        Level = level;
        if (uiController != null)
        {
            uiController.UpdateUI(Username, health);
        }
    }
}
