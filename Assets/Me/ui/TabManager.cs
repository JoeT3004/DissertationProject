using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject tab1Content; // Reference to Tab 1 Content
    public GameObject tab2Content; // Reference to Tab 2 Content
    public GameObject settingsContent; // Reference to Settings Content

    public GameObject mapboxMapView;//Reference to map content

    // Methods to activate one tab and deactivate others
    public void ShowTab1()
    {
        if (tab1Content && tab2Content && settingsContent && mapboxMapView) // Ensure references are assigned
        {
            tab1Content.SetActive(true);
            tab2Content.SetActive(false);
            settingsContent.SetActive(false);
            mapboxMapView.SetActive(true);
        }
    }

    public void ShowTab2()
    {
        if (tab1Content && tab2Content && settingsContent)
        {
            tab1Content.SetActive(false);
            tab2Content.SetActive(true);
            settingsContent.SetActive(false);
            mapboxMapView.SetActive(true);

        }
    }

    public void ShowSettings()
    {
        if (tab1Content && tab2Content && settingsContent)
        {
            tab1Content.SetActive(false);
            tab2Content.SetActive(false);
            settingsContent.SetActive(true);
            mapboxMapView.SetActive(false);

        }
    }
}
