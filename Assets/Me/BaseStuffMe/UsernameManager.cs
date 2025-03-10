using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the global Username for the local player. 
/// If not set, prompts the user to enter one. 
/// Persists in PlayerPrefs.
/// </summary>
public class UsernameManager : MonoBehaviour
{
    public static UsernameManager Instance { get; private set; }

    /// <summary>
    /// The current global username, used by BaseManager or AttackManager.
    /// </summary>
    public static string Username { get; private set; }

    [SerializeField] private GameObject usernamePanel;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Button submitButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("username"))
        {
            Username = PlayerPrefs.GetString("username");
            usernamePanel.SetActive(false);
        }
        else
        {
            usernamePanel.SetActive(false); // hidden by default until we want it
        }

        // Listen for user submission
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmitUsername);
    }

    /// <summary>
    /// Enables the username panel so the user can input a name.
    /// </summary>
    public void ShowUsernamePanel()
    {
        if (usernamePanel) usernamePanel.SetActive(true);
    }

    /// <summary>
    /// Called when user clicks "submit" on the username panel. 
    /// Sets the new username locally and in PlayerPrefs. 
    /// Updates Firebase if we already have a base.
    /// </summary>
    private void OnSubmitUsername()
    {
        if (usernameInputField == null) return;

        string enteredName = usernameInputField.text;
        if (!string.IsNullOrEmpty(enteredName))
        {
            Username = enteredName;
            PlayerPrefs.SetString("username", Username);

            if (usernamePanel) usernamePanel.SetActive(false);

            var bm = BaseManager.Instance;
            if (bm != null && bm.HasBase())
            {
                bm.UpdateUsernameInFirebase(Username);
            }

            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.RefreshCurrentTabUI();
        }
    }

    /// <summary>
    /// Public static method to set username from external code. 
    /// Also updates the static property.
    /// </summary>
    public static void SetUsername(string newUsername)
    {
        Username = newUsername;
    }

    /// <summary>
    /// Clears the username (e.g. after removing base).
    /// </summary>
    public static void ClearUsername()
    {
        Username = null;
    }
}
