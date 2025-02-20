using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UsernameManager : MonoBehaviour
{
    public static UsernameManager Instance { get; private set; }

    // The current global username
    // Notice we make set internal, or we can do public set if you prefer
    public static string Username { get; private set; }

    public static void SetUsername(string newUsername)
    {
        Username = newUsername;
    }


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
            // Hide by default, or show when we need user to set name
            usernamePanel.SetActive(false);
        }

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmitUsername);
    }

    public void ShowUsernamePanel()
    {
        usernamePanel.SetActive(true);
    }

    public void OnSubmitUsername()
    {
        if (!string.IsNullOrEmpty(usernameInputField.text))
        {
            Username = usernameInputField.text;
            PlayerPrefs.SetString("username", Username);
            usernamePanel.SetActive(false);

            // If the user already placed a base, update in Firebase
            var bm = BaseManager.Instance;
            if (bm != null && bm.HasBase())
            {
                bm.UpdateUsernameInFirebase(Username);
            }

            // Optionally refresh UI
            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.RefreshCurrentTabUI();
        }
    }

    // Called from BaseManager when removing a base
    public static void ClearUsername()
    {
        Username = null;
    }
}
