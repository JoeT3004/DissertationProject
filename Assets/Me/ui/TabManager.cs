using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class TabManager : MonoBehaviour
{
    /*
    public GameObject mapPanel;
    public GameObject basePanel;
    public GameObject settingsPanel;
*/
    public AbstractMap map;
    public Button mapButton;
    public Button baseButton;
    public Button settingsButton;

    private Vector2d specificBase = new Vector2d(40.748817, -73.985428);

    void Start()
    {
        // Debug log to confirm Start() runs
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
            case 0:
                enableMapInteraction = true;
                break;

            case 1:
                map.UpdateMap(specificBase, map.Zoom);
                enableMapInteraction = true;
                break;

            case 2:
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
}
