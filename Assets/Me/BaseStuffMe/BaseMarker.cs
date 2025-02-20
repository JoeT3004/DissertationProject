using UnityEngine;

public class BaseMarker : MonoBehaviour
{
    public string PlayerId { get; private set; }
    private BaseUIController uiController;
    public string Username { get; private set; }
    public int Health { get; private set; }
    public int Level { get; private set; }

    private void Awake()
    {
        uiController = GetComponentInChildren<BaseUIController>();
        if (uiController == null)
            Debug.LogWarning("BaseUIController not found in children of BaseMarker.");
    }

    /// <summary>
    /// Initializes the base marker with the provided data.
    /// </summary>
    public void Initialize(string playerId, string username, int health, int level)
    {
        PlayerId = playerId;
        Username = username;
        Health = health;
        Level = level;
        if (uiController != null)
        {
            // Calculate max health based on level (for example, level * 100)
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
