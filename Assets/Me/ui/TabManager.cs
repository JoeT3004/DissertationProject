using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class TabManager : MonoBehaviour
{
    public AbstractMap map;
    public Button mapButton;
    public Button baseButton;
    public Button settingsButton;

    [SerializeField] private Vector2d specificBase = new Vector2d(40.748817, -73.985428);
    [SerializeField] private BaseManager baseManager;

    // Define Mapbox Style URLs
    private const string DEFAULT_MAP_STYLE = "mapbox://styles/mapbox/streets-v11";
    private const string DARK_MAP_STYLE = "mapbox://styles/mapbox/dark-v10";

    void Start()
    {
        Debug.Log("TabManager Initialized!");

        // Set up button listeners
        mapButton.onClick.AddListener(() => ButtonClicked(0));
        baseButton.onClick.AddListener(() => ButtonClicked(1));
        settingsButton.onClick.AddListener(() => ButtonClicked(2));

        SwitchTab(0); // Default to Map View
    }

    void ButtonClicked(int tabIndex)
    {
        Debug.Log($"Button {tabIndex} Clicked!");
        SwitchTab(tabIndex);
    }

    public void SwitchTab(int tabIndex)
    {
        Debug.Log($"Switching to tab {tabIndex}");

        bool enableMapInteraction = false;

        switch (tabIndex)
        {
            case 0: // Default Map View
                enableMapInteraction = true;
                ChangeMapStyle(DEFAULT_MAP_STYLE);
                break;

            case 1: // Base Tab
                baseManager.ShowBaseOnMap();
                enableMapInteraction = true;
                ChangeMapStyle(DARK_MAP_STYLE);
                break;

            case 2: // Settings Tab
            //add toturial (will show at start of the game)
            // customizable maps
            // acccessibility
                break;
        }

        EnableMapInteractions(enableMapInteraction);
    }

    void EnableMapInteractions(bool enable)
    {
        if (map != null)
        {
            map.enabled = enable;
        }
    }

    void ChangeMapStyle(string styleUrl)
    {
        if (map != null && map.ImageLayer != null)
        {
            map.ImageLayer.SetLayerSource(styleUrl);
            map.UpdateMap(map.CenterLatitudeLongitude, map.Zoom);
            Debug.Log($"Map style changed to: {styleUrl}");
        }
        else
        {
            Debug.LogWarning("Map or ImageLayer is not properly assigned!");
        }
    }
}
