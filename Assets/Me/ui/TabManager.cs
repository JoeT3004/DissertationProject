using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;

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
                // Hide prompt if it was visible (Prompt Panel is Tab 1 only)
                baseManager.HidePromptPanel();

                // Show/Hide UI
                showRecenterButton.SetActive(true);
                removeBaseButton.SetActive(false);

                // Reload always on Tab 0
                showReloadMap = true;
                enableMapInteraction = true;
                ChangeMapStyle(DEFAULT_MAP_STYLE);
                break;

            case 1: // Base Tab
                // Show or prompt
                baseManager.ShowBaseOnMap();

                // Hide recenter
                showRecenterButton.SetActive(false);

                enableMapInteraction = true;
                ChangeMapStyle(DARK_MAP_STYLE);

                removeBaseButton.SetActive(baseManager.HasBase());

                // -----------------------------------------------
                // NEW CONDITION: We do *NOT* show reloadMapCanvas 
                // if the user is in "placing base" mode.
                // If promptPanel is active & not placing, we do show it.
                // -----------------------------------------------
                if (baseManager.IsPlacingBase())
                {
                    // In placing mode => Hide reload map
                    showReloadMap = false;
                }
                else
                {
                    // If promptPanel is active => show it
                    // (meaning user has no base, but hasn't started placing)
                    if (baseManager.IsPromptPanelActive())
                        showReloadMap = true;
                    else
                        showReloadMap = false;
                }
                break;

            case 2: // Settings Tab
                // Hide prompt
                baseManager.HidePromptPanel();

                showRecenterButton.SetActive(false);
                removeBaseButton.SetActive(false);
                showReloadMap = false;
                enableMapInteraction = false;
                break;
        }

        EnableMapInteractions(enableMapInteraction);

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
