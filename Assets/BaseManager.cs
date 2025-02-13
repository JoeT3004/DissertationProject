using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Firebase.Database;
using Firebase.Extensions;

public class BaseManager : MonoBehaviour
{
    [Header("Map & Marker Setup")]
    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject baseMarkerPrefab;

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;     // Visible only on Tab 1 if no base
    [SerializeField] private Button placeBaseButton;     // "Place Base" button
    [SerializeField] private GameObject placeBaseMessage;// "Tap the map" message

    private bool hasBase = false;
    private bool isPlacingBase = false;

    private string playerId;
    private Vector2d baseCoordinates;
    private GameObject currentBaseMarker;

    public bool HasBase() => hasBase;
    public bool IsPlacingBase() => isPlacingBase;

    // For TabManager logic
    public bool IsPromptPanelActive() => promptPanel != null && promptPanel.activeSelf;

    private System.Collections.IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        playerId = RetrieveOrCreatePlayerId();
        Debug.Log("[BaseManager] Local playerId: " + playerId);

        if (promptPanel != null) promptPanel.SetActive(false);
        if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

        if (placeBaseButton != null) placeBaseButton.interactable = true;

        // Attempt to fetch existing base
        FetchBaseFromFirebase();
    }

    private void Update()
    {
        if (hasBase) return;
        if (!isPlacingBase) return;

        // check for tap
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                TryPlaceBaseAtScreenPosition(t.position);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBaseAtScreenPosition(Input.mousePosition);
        }
    }

    private void LateUpdate()
    {
        if (hasBase && currentBaseMarker != null)
        {
            Vector3 newPos = map.GeoToWorldPosition(baseCoordinates, true);
            currentBaseMarker.transform.position = newPos;
        }
    }

    // ------------------------------------------------------------------------
    // Utility: Hide Prompt
    // ------------------------------------------------------------------------
    public void HidePromptPanel()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    // ------------------------------------------------------------------------
    // Tab #1: "ShowBaseOnMap"
    // ------------------------------------------------------------------------
    public void ShowBaseOnMap()
    {
        Debug.Log($"[BaseManager] ShowBaseOnMap called. hasBase={hasBase}");

        if (hasBase)
        {
            HidePromptPanel();

            if (placeBaseMessage != null)
                placeBaseMessage.SetActive(false);

            // Let tabs remain unlocked
            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.SetTabButtonsInteractable(true);

            // Center map
            map.SetCenterLatitudeLongitude(baseCoordinates);
            map.UpdateMap();
        }
        else
        {
            // no base => show prompt
            if (promptPanel != null)
                promptPanel.SetActive(true);

            // let the user switch tabs if they want
            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.SetTabButtonsInteractable(true);
        }
    }

    // ------------------------------------------------------------------------
    // Start Placing a Base
    // ------------------------------------------------------------------------
    public void StartPlacingBase()
    {
        if (hasBase)
        {
            Debug.Log("Already have a base. Cannot place again.");
            return;
        }

        HidePromptPanel();

        // Show message "tap the map"
        if (placeBaseMessage != null)
            placeBaseMessage.SetActive(true);

        // lock tabs while placing
        isPlacingBase = true;
        var tm = FindObjectOfType<TabManager>();
        if (tm != null) tm.SetTabButtonsInteractable(false);

        Debug.Log("[BaseManager] Now in base-placing mode => tabs locked.");
    }

    private void TryPlaceBaseAtScreenPosition(Vector2 screenPos)
    {
        if (placeBaseMessage != null)
            placeBaseMessage.SetActive(false);

        bool success = ScreenPositionToLatLon(screenPos, out Vector2d latLon);
        if (success)
        {
            ConfirmBaseLocation(latLon);
            isPlacingBase = false;
        }
        else
        {
            Debug.LogWarning("[BaseManager] Could not place base, missed plane?");
        }
    }

    private void ConfirmBaseLocation(Vector2d location)
    {
        if (hasBase)
        {
            Debug.Log("We already have a base, ignoring placement.");
            return;
        }

        hasBase = true;
        baseCoordinates = location;
        PlaceBaseMarker(location);
        SaveBaseToFirebase(location);

        if (placeBaseButton != null)
            placeBaseButton.interactable = false;

        Debug.Log($"[BaseManager] Base confirmed at {location.x},{location.y}");

        // done placing => unlock tabs
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(true);
            // **Important** to re-run SwitchTab(1) logic => hides reload if we are on Tab 1
            tm.RefreshCurrentTabUI();
        }
    }

    // ------------------------------------------------------------------------
    // Remove Base
    // ------------------------------------------------------------------------
    public void RemoveBase()
    {
        if (!hasBase)
        {
            Debug.Log("[BaseManager] No base to remove.");
            return;
        }

        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("[BaseManager] DB ref is null.");
            return;
        }

        FirebaseInit.DBReference.Child("bases").Child(playerId).RemoveValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[BaseManager] Remove base failed!");
                    return;
                }

                Debug.Log("[BaseManager] Base removed from Firebase.");

                hasBase = false;
                if (currentBaseMarker != null)
                {
                    Destroy(currentBaseMarker);
                    currentBaseMarker = null;
                }

                if (placeBaseButton != null)
                    placeBaseButton.interactable = true;

                // If user is on Tab 1 => show prompt again
                var tm = FindObjectOfType<TabManager>();
                if (tm != null && tm.CurrentTabIndex == 1)
                {
                    ShowBaseOnMap();   // shows prompt
                    tm.RefreshCurrentTabUI();
                }
            });
    }

    // ------------------------------------------------------------------------
    // Firebase
    // ------------------------------------------------------------------------
    private void PlaceBaseMarker(Vector2d coords)
    {
        Vector3 wPos = map.GeoToWorldPosition(coords, true);
        if (currentBaseMarker == null)
        {
            currentBaseMarker = Instantiate(baseMarkerPrefab, wPos, Quaternion.identity);
        }
        else
        {
            currentBaseMarker.transform.position = wPos;
        }
    }

    private void SaveBaseToFirebase(Vector2d coords)
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("[BaseManager] DB not ready, skipping save.");
            return;
        }

        FirebaseInit.DBReference.Child("bases").Child(playerId).Child("latitude").SetValueAsync(coords.x);
        FirebaseInit.DBReference.Child("bases").Child(playerId).Child("longitude").SetValueAsync(coords.y);
        Debug.Log("[BaseManager] Base saved to Firebase at coords " + coords);
    }

    private void FetchBaseFromFirebase()
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("[BaseManager] Firebase not ready.");
            return;
        }

        Debug.Log("[BaseManager] Fetching base for " + playerId);

        FirebaseInit.DBReference.Child("bases").Child(playerId).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[BaseManager] Fetch base failed.");
                    return;
                }

                var snap = task.Result;
                if (!snap.Exists || !snap.HasChildren)
                {
                    Debug.Log("[BaseManager] No base found for " + playerId);
                    hasBase = false;
                    return;
                }

                hasBase = true;
                double lat = double.Parse(snap.Child("latitude").Value.ToString());
                double lon = double.Parse(snap.Child("longitude").Value.ToString());
                baseCoordinates = new Vector2d(lat, lon);
                PlaceBaseMarker(baseCoordinates);
                Debug.Log("[BaseManager] Found existing base in DB.");
            });
    }

    private bool ScreenPositionToLatLon(Vector2 screenPos, out Vector2d latLon)
    {
        latLon = Vector2d.zero;
        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(screenPos);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPos = ray.GetPoint(distance);
            latLon = map.WorldToGeoPosition(hitPos);
            return true;
        }
        return false;
    }

    private string RetrieveOrCreatePlayerId()
    {
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
            return newId;
        }
        return PlayerPrefs.GetString("playerId");
    }
}
