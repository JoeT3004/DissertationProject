using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Examples;

using System;


public class TabManager : MonoBehaviour
{
    public AbstractMap map;
    public Button mapButton;
    public Button baseButton;
    public Button settingsButton;

    [SerializeField] private BaseManager baseManager;

    [SerializeField] private GameObject removeBaseButton;
    [SerializeField] private GameObject showRecenterButton;
    [SerializeField] private GameObject reloadMapCanvas;

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

        mapButton.onClick.AddListener(() => ButtonClicked(0));
        baseButton.onClick.AddListener(() => ButtonClicked(1));
        settingsButton.onClick.AddListener(() => ButtonClicked(2));

        // Start on tab 0
        SwitchTab(0);
    }

    private void ButtonClicked(int tabIndex)
    {
        // If we're in actual "base placing mode", do NOT allow switching
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

        switch (tabIndex)
        {
            case 0: // Map Tab
                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(true);
                removeBaseButton.SetActive(false);
                showReloadMap = true;
                enableMapInteraction = true;

                ChangeMapStyle(DEFAULT_MAP_STYLE);

                // Always enable panning on tab 0
                EnableQuadTreeCameraMovement(true);

                break;

            case 1: // Base Tab
                baseManager.ShowBaseOnMap();

                showRecenterButton.SetActive(false);
                enableMapInteraction = true;
                ChangeMapStyle(DARK_MAP_STYLE);
                removeBaseButton.SetActive(baseManager.HasBase());

                // If user is in placing mode => hide reloadMapCanvas
                if (baseManager.IsPlacingBase())
                {
                    showReloadMap = false;
                }
                else
                {
                    // If promptPanel is active => show reload
                    if (baseManager.IsPromptPanelActive())
                        showReloadMap = true;
                    else
                        showReloadMap = false;
                }

                // --- Disable or enable panning here ---
                if (baseManager.HasBase())
                {
                    // 1) Disable the QuadTreeCameraMovement
                    EnableQuadTreeCameraMovement(false);

                    // 2) Set a specific zoom level
                    map.UpdateMap(map.CenterLatitudeLongitude, baseViewZoomLevel);
                }
                else
                {
                    // No base => we allow the user to still pan around
                    EnableQuadTreeCameraMovement(true);
                }

                break;

            case 2: // Settings Tab
                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(false);
                removeBaseButton.SetActive(false);
                showReloadMap = false;
                enableMapInteraction = false;

                // Re-enable panning in settings if you want, or keep it disabled
                EnableQuadTreeCameraMovement(false);

                break;
        }

        EnableMapInteractions(enableMapInteraction);

        // Show/hide reloadCanvas
        if (reloadMapCanvas != null)
            reloadMapCanvas.SetActive(showReloadMap);
    }


    // Called from BaseManager after a base is placed or removed, 
    // or any time we want to re-check the logic for the current tab
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

    // Expose the current tab index if needed
    public int CurrentTabIndex => currentTabIndex;
}
