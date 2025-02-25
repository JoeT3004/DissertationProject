using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Examples;
using System;
using TMPro;

public class TabManager : MonoBehaviour
{
    public AbstractMap map;
    public Button mapButton;
    public Button baseButton;
    public Button settingsButton;

    


    [SerializeField] private BaseManager baseManager;

    [SerializeField] private GameObject showRecenterButton;
    [SerializeField] private GameObject reloadMapCanvas;

    [SerializeField] private GameObject score;




    [Header("Base Tab UI Elements")]

    [SerializeField] private GameObject baseUsernamePanel; // The new opaque panel for tab 2
    [SerializeField] private GameObject upgradeBaseButton;
    [SerializeField] private TMP_Text currentBaseStats;

    private const string DEFAULT_MAP_STYLE = "mapbox://styles/mapbox/streets-v11";
    private const string DARK_MAP_STYLE = "mapbox://styles/mapbox/dark-v10";

    private int currentTabIndex = 0;

    [SerializeField, Range(1, 21)]
    private float baseViewZoomLevel = 16f; // or your preferred default

    [Header("Settings Tab UI Elements")]


    [SerializeField] private GameObject settingsPanel; // The new opaque panel for tab 2
    [SerializeField] private TMP_InputField settingsUsernameInputField;
    [SerializeField] private Button updateUsernameButton;


    [SerializeField] private Button removeBaseUIButton;  // Now only using this for both visibility & interaction

    [SerializeField] private GameObject settingsTitle;
    [SerializeField] private GameObject removeBaseConfirmPanel;
    [SerializeField] private Button yesRemoveBaseButton;
    [SerializeField] private Button noRemoveBaseButton;

    private void EnableQuadTreeCameraMovement(bool enable)
    {
        if (map != null)
        {
            var quadTree = map.GetComponent<QuadTreeCameraMovement>();
            if (quadTree != null)
            {
                quadTree.enabled = enable;
            }
        }
    }

    void Start()
    {
        removeBaseUIButton.interactable = true;

        
        // Start on tab 0
        SwitchTab(0);

        // Remove all default listeners for the RemoveBase button
        removeBaseUIButton.onClick.RemoveAllListeners();
        removeBaseUIButton.onClick.AddListener(OnRemoveBaseButtonClicked);

        yesRemoveBaseButton.onClick.RemoveAllListeners();
        yesRemoveBaseButton.onClick.AddListener(ConfirmRemoveBase);

        noRemoveBaseButton.onClick.RemoveAllListeners();
        noRemoveBaseButton.onClick.AddListener(CancelRemoveBase);

        if (updateUsernameButton != null)
        {
            // Clear old listeners
            updateUsernameButton.onClick.RemoveAllListeners();
            updateUsernameButton.onClick.AddListener(OnUpdateUsernameButtonClicked);
        }
    }

    private void ButtonClicked(int tabIndex)
    {
        // If we're in "base placing mode", do NOT allow switching
        if (baseManager.IsPlacingBase())
        {
            Debug.LogWarning("Cannot switch tabs while placing a base. Must finish placing first.");
            return;
        }

        SwitchTab(tabIndex);
    }

    public void SwitchTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        bool enableMapInteraction = false;
        bool showReloadMap = false;

        // Hide or show certain objects by default:
        upgradeBaseButton.SetActive(false);
        currentBaseStats.gameObject.SetActive(false);

        switch (tabIndex)
        {
            case 0: // Map Tab
                if (settingsPanel != null)
                    settingsPanel.SetActive(false);

                // If you want to hide the username panel every time you go to the map:
                baseUsernamePanel.SetActive(false);

                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(true);
                removeBaseUIButton.gameObject.SetActive(false);
                score.SetActive(true);

                showReloadMap = true;
                enableMapInteraction = true;

                ChangeMapStyle(DEFAULT_MAP_STYLE);
                EnableQuadTreeCameraMovement(true);

                removeBaseConfirmPanel.SetActive(false);

                break;

            case 1: // Base Tab
                if (settingsPanel != null)
                    settingsPanel.SetActive(false);

                // If you want to hide any settings UI here:
                removeBaseConfirmPanel.SetActive(false);

                // Possibly set baseUsernamePanel.SetActive(false or true)...

                // Username logic
                if (!baseManager.HasBase() && string.IsNullOrEmpty(UsernameManager.Username))
                {
                    UsernameManager.Instance.ShowUsernamePanel();
                    baseManager.HidePromptPanel();
                }
                else
                {
                    baseManager.ShowBaseOnMap();
                }

                showRecenterButton.SetActive(false);
                score.SetActive(true);

                if (baseManager.HasBase())
                {
                    upgradeBaseButton.SetActive(true);
                    currentBaseStats.gameObject.SetActive(true);

                    int level = baseManager.CurrentLevel;
                    int health = baseManager.CurrentHealth;
                    currentBaseStats.text = $"Base Level: {level}\nBase Health: {health}/{health}";
                }

                enableMapInteraction = true;
                ChangeMapStyle(DARK_MAP_STYLE);

                removeBaseUIButton.gameObject.SetActive(baseManager.HasBase());

                // If user is placing base, maybe hide reload map:
                if (baseManager.IsPlacingBase())
                    showReloadMap = false;
                else
                    showReloadMap = baseManager.IsPromptPanelActive();

                if (baseManager.HasBase())
                {
                    EnableQuadTreeCameraMovement(false);
                    map.UpdateMap(map.CenterLatitudeLongitude, baseViewZoomLevel);
                }
                else
                {
                    EnableQuadTreeCameraMovement(true);
                }
                break;

            case 2: // Settings Tab
                baseManager.HidePromptPanel();

                // Hide stuff on the Settings tab if you want:
                showRecenterButton.SetActive(false);
                score.SetActive(false);
                baseUsernamePanel.SetActive(false);

                showReloadMap = false;
                enableMapInteraction = false;

                EnableQuadTreeCameraMovement(false);

                if (settingsPanel != null)
                    settingsPanel.SetActive(true);

                if (baseManager.HasBase())
                {
                    removeBaseUIButton.gameObject.SetActive(true);
                }
                else
                {
                    removeBaseUIButton.gameObject.SetActive(false);
                }

                // If you want your "update username" UI to be visible only in settings,
                // you can do something like:
                // settingsUsernameInputField.gameObject.SetActive(true);
                // updateUsernameButton.gameObject.SetActive(true);

                break;
        }

        EnableMapInteractions(enableMapInteraction);

        // Show/hide reloadCanvas
        if (reloadMapCanvas != null)
            reloadMapCanvas.SetActive(showReloadMap);
    }


    // Called from BaseManager after a base is placed or removed
    public void RefreshCurrentTabUI()
    {
        SwitchTab(currentTabIndex);
    }

    private void EnableMapInteractions(bool enable)
    {
        if (map != null)
            map.enabled = enable;
    }

    public void SetTabButtonsInteractable(bool interactable)
    {
        mapButton.interactable = interactable;
        baseButton.interactable = interactable;
        settingsButton.interactable = interactable;
    }

    private void ChangeMapStyle(string styleUrl)
    {
        if (map != null && map.ImageLayer != null)
        {
            map.ImageLayer.SetLayerSource(styleUrl);
            map.UpdateMap(map.CenterLatitudeLongitude, map.Zoom);
        }
        else
        {
            Debug.LogWarning("Map or ImageLayer is not properly assigned!");
        }
    }

    public void OnRemoveBaseButtonClicked()
    {
        Debug.Log("OnRemoveBaseButtonClicked() called!");

        foreach (Transform child in settingsPanel.transform)
        {
            if (child.gameObject != removeBaseConfirmPanel)
            {
                child.gameObject.SetActive(false);
            }
        }

        removeBaseConfirmPanel.SetActive(true);
    }

    public void ConfirmRemoveBase()
    {
        baseManager.RemoveBase();
        removeBaseConfirmPanel.SetActive(false);

        settingsTitle.SetActive(true);

        


        SwitchTab(2);
        removeBaseUIButton.gameObject.SetActive(false);
    }

    public void CancelRemoveBase()
    {
        removeBaseConfirmPanel.SetActive(false);

        settingsTitle.SetActive(true);

        SwitchTab(2);
    }

    public void OnUpdateUsernameButtonClicked()
    {
        // 1) Grab the new username text
        string newUsername = (settingsUsernameInputField != null)
            ? settingsUsernameInputField.text
            : "";

        if (string.IsNullOrEmpty(newUsername))
        {
            Debug.LogWarning("Cannot update username: field is empty.");
            return;
        }

        // 2) Set local in UsernameManager + PlayerPrefs
        UsernameManager.SetUsername(newUsername);
        PlayerPrefs.SetString("username", newUsername);

        // 3) If we have a base, update it on Firebase
        if (baseManager.HasBase())
        {
            baseManager.UpdateUsernameInFirebase(newUsername);
        }
        else
        {
            Debug.Log("No base placed yet, but we saved the new username locally.");
        }

        // 4) Optionally refresh UI or close a popup, etc.
        RefreshCurrentTabUI();
        Debug.Log($"Updated username to '{newUsername}' in Settings!");
    }


    public int CurrentTabIndex => currentTabIndex;
}
