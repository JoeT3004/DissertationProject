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

    [SerializeField] private GameObject settingsPanel; // The new opaque panel for tab 2

    [SerializeField] private GameObject score;

    [SerializeField] private Button removeBaseUIButton;  // Now only using this for both visibility & interaction

    [SerializeField] private GameObject settingsTitle;
    [SerializeField] private GameObject removeBaseConfirmPanel;
    [SerializeField] private Button yesRemoveBaseButton;
    [SerializeField] private Button noRemoveBaseButton;

    [Header("Base Tab UI Elements")]
    [SerializeField] private GameObject upgradeBaseButton;
    [SerializeField] private TMP_Text currentBaseStats;

    private const string DEFAULT_MAP_STYLE = "mapbox://styles/mapbox/streets-v11";
    private const string DARK_MAP_STYLE = "mapbox://styles/mapbox/dark-v10";

    private int currentTabIndex = 0;

    [SerializeField, Range(1, 21)]
    private float baseViewZoomLevel = 16f; // or your preferred default

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

        mapButton.onClick.AddListener(() => ButtonClicked(0));
        baseButton.onClick.AddListener(() => ButtonClicked(1));
        settingsButton.onClick.AddListener(() => ButtonClicked(2));

        // Start on tab 0
        SwitchTab(0);

        // Remove all default listeners for the RemoveBase button
        removeBaseUIButton.onClick.RemoveAllListeners();
        removeBaseUIButton.onClick.AddListener(OnRemoveBaseButtonClicked);

        yesRemoveBaseButton.onClick.RemoveAllListeners();
        yesRemoveBaseButton.onClick.AddListener(ConfirmRemoveBase);

        noRemoveBaseButton.onClick.RemoveAllListeners();
        noRemoveBaseButton.onClick.AddListener(CancelRemoveBase);
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

        // By default, we turn OFF the upgrade button and stats text in all tabs.
        // We'll turn them back ON only in case #1 if a base is present.
        upgradeBaseButton.SetActive(false);
        currentBaseStats.gameObject.SetActive(false);

        switch (tabIndex)
        {
            case 0: // Map Tab
                if (settingsPanel != null)
                    settingsPanel.SetActive(false);

                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(true);
                removeBaseUIButton.gameObject.SetActive(false);  // Replaced removeBaseButton with removeBaseUIButton
                score.SetActive(true);
                showReloadMap = true;
                enableMapInteraction = true;

                ChangeMapStyle(DEFAULT_MAP_STYLE);

                // Always enable panning on tab 0
                EnableQuadTreeCameraMovement(true);
                removeBaseConfirmPanel.SetActive(false);

                break;

            case 1: // Base Tab
                if (settingsPanel != null)
                    settingsPanel.SetActive(false);

                baseManager.ShowBaseOnMap();

                showRecenterButton.SetActive(false);
                score.SetActive(true);

                // Check if we have a base
                bool hasBase = baseManager.HasBase();

                // If a base exists, we enable the upgrade button & stats text
                if (hasBase)
                {
                    upgradeBaseButton.SetActive(true);
                    currentBaseStats.gameObject.SetActive(true);

                    // Update the stats text
                    int level = baseManager.CurrentLevel;
                    int health = baseManager.CurrentHealth;
                    currentBaseStats.text = $"Base Level: {level}\nBase Health: {health}/{health}";
                }

                enableMapInteraction = true;
                ChangeMapStyle(DARK_MAP_STYLE);

                removeBaseUIButton.gameObject.SetActive(hasBase);  // Replaced removeBaseButton with removeBaseUIButton
                removeBaseConfirmPanel.SetActive(false);

                // If user is in placing mode => hide reloadMapCanvas
                if (baseManager.IsPlacingBase())
                {
                    showReloadMap = false;
                }
                else
                {
                    // If promptPanel is active => show reload
                    showReloadMap = baseManager.IsPromptPanelActive();
                }

                // - Disable panning if we have a base. Then set a specific zoom
                if (hasBase)
                {
                    EnableQuadTreeCameraMovement(false);
                    map.UpdateMap(map.CenterLatitudeLongitude, baseViewZoomLevel);
                }
                else
                {
                    // No base => user can still pan
                    EnableQuadTreeCameraMovement(true);
                }
                break;

            case 2: // Settings Tab
                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(false);
                score.SetActive(false);

                showReloadMap = false;
                enableMapInteraction = false;

                // Optional: disable panning in settings
                EnableQuadTreeCameraMovement(false);

                // Show the SettingsPanel
                if (settingsPanel != null)
                    settingsPanel.SetActive(true);

                // Show or hide RemoveBaseButton inside settingsPanel
                if (baseManager.HasBase())
                {
                    removeBaseUIButton.gameObject.SetActive(true);  // Replaced removeBaseButton with removeBaseUIButton
                }
                else
                {
                    removeBaseUIButton.gameObject.SetActive(false);
                }
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

    public int CurrentTabIndex => currentTabIndex;
}
