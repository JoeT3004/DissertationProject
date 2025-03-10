using UnityEngine;
using TMPro;

/// <summary>
/// Manages the floating UI text for a single troop (who it's attacking, 
/// who sent it, how much time remains, etc.).
/// </summary>
public class TroopUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text goingToText;
    [SerializeField] private TMP_Text comingFromText;
    [SerializeField] private TMP_Text timeLeftText;

    private void Start()
    {
        // Position the UI above the troop
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>
    /// Displays the two usernames (attacker and target) on the troop UI.
    /// </summary>
    public void SetTexts(string fromUsername, string toUsername)
    {
        goingToText.text = $"Attacking: {toUsername}'s base";
        comingFromText.text = $"Sent By: {fromUsername}'s base";
    }

    /// <summary>
    /// Updates the "time left" text in minutes (clamped to 0).
    /// </summary>
    public void SetTimeLeft(double timeLeftSec)
    {
        if (timeLeftText == null) return;

        if (timeLeftSec < 0) timeLeftSec = 0;
        double mins = timeLeftSec / 60.0;
        timeLeftText.text = $"Base reached in: {mins:F1} min";
    }
}

