using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mapbox.Unity.Map;
using Mapbox.Examples;

/// <summary>
/// Manages the 3 main tabs (Map, Base, Settings) by:
///  1) Hiding/showing relevant UI,
///  2) Enabling/disabling map or camera interactions,
///  3) Handling "Remove Base" and "Update Username" from Settings,
///  4) Toggling "Reduce Load" mode for troop updates.
/// </summary>
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
    [SerializeField] private GameObject baseUsernamePanel;
    [SerializeField] private GameObject upgradeBaseButton;
    [SerializeField] private TMP_Text currentBaseStats;

    private const string DEFAULT_MAP_STYLE = "mapbox://styles/mapbox/streets-v11";
    private const string DARK_MAP_STYLE = "mapbox://styles/mapbox/dark-v10";

    private int currentTabIndex = 0;

    [SerializeField, Range(1, 21)]
    private float baseViewZoomLevel = 16f;

    [Header("Settings Tab UI Elements")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_InputField settingsUsernameInputField;
    [SerializeField] private Button updateUsernameButton;
    [SerializeField] private Button removeBaseUIButton;
    [SerializeField] private GameObject settingsTitle;
    [SerializeField] private GameObject removeBaseConfirmPanel;
    [SerializeField] private Button yesRemoveBaseButton;
    [SerializeField] private Button noRemoveBaseButton;
    [SerializeField] private Toggle reduceLoadToggle;

    private static bool reduceLoadMode = false;

    /// <summary>
    /// Read‐only property for the current tab index.
    /// </summary>
    public int CurrentTabIndex => currentTabIndex;

    private void Start()
    {
        // We ensure the removeBaseUIButton is clickable
        removeBaseUIButton.interactable = true;

        // Subscribe to "reduce load" toggle changes
        if (reduceLoadToggle != null)
        {
            reduceLoadToggle.onValueChanged.AddListener(OnReduceLoadToggleChanged);
        }

        // Switch to tab 0 (Map) by default
        SwitchTab(0);

        // Remove old listeners on the "Remove Base" button
        removeBaseUIButton.onClick.RemoveAllListeners();
        removeBaseUIButton.onClick.AddListener(OnRemoveBaseButtonClicked);

        // Confirm/cancel for remove base
        yesRemoveBaseButton.onClick.RemoveAllListeners();
        yesRemoveBaseButton.onClick.AddListener(ConfirmRemoveBase);

        noRemoveBaseButton.onClick.RemoveAllListeners();
        noRemoveBaseButton.onClick.AddListener(CancelRemoveBase);

        // Update username button
        if (updateUsernameButton != null)
        {
            updateUsernameButton.onClick.RemoveAllListeners();
            updateUsernameButton.onClick.AddListener(OnUpdateUsernameButtonClicked);
        }
    }

    /// <summary>
    /// Switches the game to one of the 3 tabs. 
    /// This controls which UI is visible, if the map is interactable, etc.
    /// 
    /// TAB 0 = "Map"
    /// TAB 1 = "Base"
    /// TAB 2 = "Settings"
    /// </summary>
    public void SwitchTab(int tabIndex)
    {
        currentTabIndex = tabIndex;

        // We'll track whether the map should be interactive
        bool enableMapInteraction = false;
        // We'll also track if we show the "Reload Map" UI
        bool showReloadMap = false;

        // For convenience, hide some items by default:
        if (upgradeBaseButton) upgradeBaseButton.SetActive(false);
        if (currentBaseStats) currentBaseStats.gameObject.SetActive(false);

        // The main tab switch logic:
        switch (tabIndex)
        {
            case 0: // Map Tab
                // 1) We hide the Settings panel and base username panel
                if (settingsPanel) settingsPanel.SetActive(false);
                if (baseUsernamePanel) baseUsernamePanel.SetActive(false);

                // 2) Hide any base prompt if active
                baseManager.HidePromptPanel();

                // 3) Show the recenter button so user can recenter on themselves
                showRecenterButton.SetActive(true);

                // 4) The remove base button is not used on the map tab
                removeBaseUIButton.gameObject.SetActive(false);

                // 5) Show the player's score
                score.SetActive(true);

                // 6) We want the reloadMapCanvas visible so they can refresh the map if needed
                showReloadMap = true;

                // 7) Let them interact with the map
                enableMapInteraction = true;

                // 8) Switch the map to the default style
                ChangeMapStyle(DEFAULT_MAP_STYLE);

                // 9) Re-enable camera panning/zoom
                EnableQuadTreeCameraMovement(true);

                // 10) Hide the "Are you sure" panel for removing base
                if (removeBaseConfirmPanel) removeBaseConfirmPanel.SetActive(false);

                break;

            case 1: // Base Tab
                // 1) Hide settings panel if open
                if (settingsPanel) settingsPanel.SetActive(false);

                // 2) Hide the confirm remove panel if open
                if (removeBaseConfirmPanel) removeBaseConfirmPanel.SetActive(false);

                // 3) If the user has NO base AND no username, prompt them to set a username
                if (!baseManager.HasBase() && string.IsNullOrEmpty(UsernameManager.Username))
                {
                    UsernameManager.Instance.ShowUsernamePanel();
                    baseManager.HidePromptPanel();
                }
                else
                {
                    // Otherwise, show base
                    baseManager.ShowBaseOnMap();
                }

                // 4) Hide recenter button if not relevant in base tab
                showRecenterButton.SetActive(false);

                // 5) Show score if you want
                score.SetActive(true);

                // 6) If the user has a base, show the upgrade button and stats
                if (baseManager.HasBase())
                {
                    if (upgradeBaseButton) upgradeBaseButton.SetActive(true);

                    if (currentBaseStats)
                    {
                        currentBaseStats.gameObject.SetActive(true);
                        int level = baseManager.CurrentLevel;
                        int health = baseManager.CurrentHealth;
                        int maxHealth = level * 100;
                        currentBaseStats.text = $"Base Level: {level}\nBase Health: {health}/{maxHealth}";
                    }
                }

                // 7) We can let them interact with map in the base tab
                enableMapInteraction = true;
                // 8) Switch to a darker map style, just for variety
                ChangeMapStyle(DARK_MAP_STYLE);

                // 9) If the user has a base, show the remove base button
                removeBaseUIButton.gameObject.SetActive(baseManager.HasBase());

                // 10) If user is placing a base, hide reloadMapCanvas. Otherwise, show it if a prompt is up
                if (baseManager.IsPlacingBase())
                {
                    showReloadMap = false;
                }
                else
                {
                    showReloadMap = baseManager.IsPromptPanelActive();
                }

                // 11) If they have a base, we might want to "lock" the camera to that area / set a custom zoom
                if (baseManager.HasBase())
                {
                    // Disabling panning/zoom from the user, but we forcibly set the zoom
                    EnableQuadTreeCameraMovement(false);
                    map.UpdateMap(map.CenterLatitudeLongitude, baseViewZoomLevel);
                }
                else
                {
                    // If they have no base, let them pan around to find a good spot
                    EnableQuadTreeCameraMovement(true);
                }

                break;

            case 2: // Settings Tab
                // 1) Hide base prompt, recenter button, and score in settings
                baseManager.HidePromptPanel();
                showRecenterButton.SetActive(false);
                score.SetActive(false);

                // 2) Hide base username panel if open
                if (baseUsernamePanel) baseUsernamePanel.SetActive(false);

                // 3) We don't show reload map in settings
                showReloadMap = false;

                // 4) Typically disable map interaction in settings
                enableMapInteraction = false;

                // 5) Also disable camera movement
                EnableQuadTreeCameraMovement(false);

                // 6) Show the reduceLoad toggle if it exists
                if (reduceLoadToggle != null)
                    reduceLoadToggle.gameObject.SetActive(true);

                // 7) Show the settings panel
                if (settingsPanel != null)
                    settingsPanel.SetActive(true);

                // 8) If the user has a base, show the "Remove Base" and username fields
                if (baseManager.HasBase())
                {
                    removeBaseUIButton.gameObject.SetActive(true);
                    settingsUsernameInputField.gameObject.SetActive(true);
                    updateUsernameButton.gameObject.SetActive(true);
                }
                else
                {
                    // If no base, hide them
                    removeBaseUIButton.gameObject.SetActive(false);
                    settingsUsernameInputField.gameObject.SetActive(false);
                    updateUsernameButton.gameObject.SetActive(false);
                }
                break;
        }

        // Enable or disable the map itself
        EnableMapInteractions(enableMapInteraction);

        // Show/hide the reload canvas
        if (reloadMapCanvas != null)
        {
            reloadMapCanvas.SetActive(showReloadMap);
        }
    }

    /// <summary>
    /// Called from BaseManager or anywhere else when the UI might need updating again.
    /// Re-runs the SwitchTab logic with the currentTabIndex.
    /// </summary>
    public void RefreshCurrentTabUI()
    {
        SwitchTab(currentTabIndex);
    }

    /// <summary>
    /// Enables or disables the Map component. 
    /// If disabled, user can't pan/zoom via the map script.
    /// </summary>
    private void EnableMapInteractions(bool enable)
    {
        if (map != null)
        {
            map.enabled = enable;
        }
    }

    /// <summary>
    /// Logic invoked if the user tries to switch tabs by pressing the map/base/settings buttons.
    /// We skip switching if the user is placing a base (to avoid partial states).
    /// </summary>
    private void ButtonClicked(int tabIndex)
    {
        if (baseManager.IsPlacingBase())
        {
            Debug.LogWarning("Cannot switch tabs while placing a base. Must finish placing first.");
            return;
        }
        SwitchTab(tabIndex);
    }

    /// <summary>
    /// Changes the map's style layer source, then updates the map to reload tiles.
    /// </summary>
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

    /// <summary>
    /// Enables or disables the QuadTreeCameraMovement script on the map, controlling panning/zoom.
    /// </summary>
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

    /// <summary>
    /// Called by the "Remove Base" button. Shows a confirmation panel so user can confirm/cancel.
    /// </summary>
    public void OnRemoveBaseButtonClicked()
    {
        Debug.Log("OnRemoveBaseButtonClicked() called!");

        // Hide other settings UI, show confirm panel
        foreach (Transform child in settingsPanel.transform)
        {
            if (child.gameObject != removeBaseConfirmPanel)
            {
                child.gameObject.SetActive(false);
            }
        }
        removeBaseConfirmPanel.SetActive(true);
    }

    /// <summary>
    /// User pressed "Yes" => we call BaseManager.RemoveBase, hide the panel, 
    /// and set the tab to 2 again (Settings).
    /// </summary>
    public void ConfirmRemoveBase()
    {
        baseManager.RemoveBase();
        removeBaseConfirmPanel.SetActive(false);
        settingsTitle.SetActive(true);

        SwitchTab(2);
        removeBaseUIButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// User pressed "No" => hide the confirm panel, re-show the normal settings UI.
    /// </summary>
    public void CancelRemoveBase()
    {
        removeBaseConfirmPanel.SetActive(false);
        settingsTitle.SetActive(true);

        SwitchTab(2);
    }

    /// <summary>
    /// Called when user clicks "Update Username" in the settings tab.
    /// Sets local + PlayerPrefs username, updates Firebase if we have a base.
    /// </summary>
    public void OnUpdateUsernameButtonClicked()
    {
        string newUsername = (settingsUsernameInputField != null)
            ? settingsUsernameInputField.text
            : "";

        if (string.IsNullOrEmpty(newUsername))
        {
            Debug.LogWarning("Cannot update username: field is empty.");
            return;
        }

        UsernameManager.SetUsername(newUsername);
        PlayerPrefs.SetString("username", newUsername);

        if (baseManager.HasBase())
        {
            baseManager.UpdateUsernameInFirebase(newUsername);
        }
        else
        {
            Debug.Log("No base placed yet, but we saved the new username locally.");
        }

        RefreshCurrentTabUI();
        Debug.Log($"Updated username to '{newUsername}' in Settings!");
    }

    /// <summary>
    /// Called by the reduceLoadToggle => sets a static bool that TroopController can read 
    /// to skip frames, updating troop positions once per second instead of every frame.
    /// </summary>
    public void OnReduceLoadToggleChanged(bool isOn)
    {
        reduceLoadMode = isOn;
        Debug.Log("Reduce Load Mode = " + reduceLoadMode);
    }

    /// <summary>
    /// Returns if "reduce load mode" is enabled. TroopController references this to do fewer updates.
    /// </summary>
    public static bool IsReduceLoadMode()
    {
        return reduceLoadMode;
    }

    /// <summary>
    /// Enables or disables the map/base/settings buttons at the bottom 
    /// (useful for locking out navigation while placing a base, etc.).
    /// </summary>
    public void SetTabButtonsInteractable(bool interactable)
    {
        mapButton.interactable = interactable;
        baseButton.interactable = interactable;
        settingsButton.interactable = interactable;
    }
}
