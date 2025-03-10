using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Controller for an "enemy" base marker (health bar, name, etc.).
/// Renders above the base in the world space.
/// </summary>
public class BaseUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private int maxHealth = 100;

    private void Start()
    {
        // Position the UI above the base
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>
    /// Initializes the UI fields with username, current health, and max health.
    /// </summary>
    public void Initialize(string username, int currentHealth, int maxHealth)
    {
        this.maxHealth = maxHealth;
        if (healthSlider)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        UpdateUI(username, currentHealth);
    }

    /// <summary>
    /// Updates the displayed username, health slider, and health text.
    /// </summary>
    public void UpdateUI(string username, int currentHealth)
    {
        if (usernameText != null)
            usernameText.text = username;

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }
}
