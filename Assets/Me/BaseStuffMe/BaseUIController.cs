using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Slider healthSlider; // Slider component
    [SerializeField] private TMP_Text healthText;   // Text element placed inside the slider

    private int maxHealth = 100;

    private void Start()
    {
        // For a top-down view, offset the canvas upward relative to the base.
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>
    /// Initializes the UI with the username, current health, and maximum health.
    /// </summary>
    public void Initialize(string username, int currentHealth, int maxHealth)
    {
        this.maxHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        UpdateUI(username, currentHealth);
    }

    /// <summary>
    /// Updates the displayed username, slider value, and health text.
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
