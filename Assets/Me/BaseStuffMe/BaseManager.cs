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
    [SerializeField] private GameObject promptPanel;      // Visible only on Tab1 if no base
    [SerializeField] private Button placeBaseButton;      // "Place Base" button
    [SerializeField] private GameObject placeBaseMessage; // "Tap the map" message

    [Header("Costs")]
    [SerializeField] private int upgradeCost = 50;

    private bool hasBase = false;
    private bool isPlacingBase = false;
    private string playerId;
    private GameObject currentBaseMarker;
    private Vector2d baseCoordinates;

    private int currentHealth;
    private int currentLevel;
    private int totalScoreSpentOnUpgrades;

    public static BaseManager Instance { get; private set; }

    public bool HasBase() => hasBase;
    public bool IsPlacingBase() => isPlacingBase;
    public int CurrentHealth => currentHealth;
    public int CurrentLevel => currentLevel;
    public bool IsPromptPanelActive() => promptPanel != null && promptPanel.activeSelf;

    private void Awake()
    {
        // Basic singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Fallback if map not assigned in Inspector:
        if (!map)
        {
            map = FindObjectOfType<AbstractMap>();
            if (!map)
            {
                Debug.LogError("[BaseManager] No AbstractMap found in scene!");
            }
        }
    }

    private System.Collections.IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        // PlayerID
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
        }
        playerId = PlayerPrefs.GetString("playerId");
        Debug.Log("[BaseManager] Local playerId: " + playerId);

        if (promptPanel != null) promptPanel.SetActive(false);
        if (placeBaseMessage != null) placeBaseMessage.SetActive(false);
        if (placeBaseButton != null) placeBaseButton.interactable = true;

        // Fetch existing base
        FetchBaseFromFirebase();
    }

    private void OnDestroy()
    {
        // Unsubscribe from base real-time listener, if you set one
        if (!string.IsNullOrEmpty(playerId) && FirebaseInit.DBReference != null)
        {
            FirebaseInit.DBReference
                .Child("users")
                .Child(playerId)
                .Child("base")
                .ValueChanged -= OnMyBaseChanged;
        }
    }

    private void Update()
    {
        // Only if we do NOT have a base and we are placing one
        if (!hasBase && isPlacingBase)
        {
            // Check for tap/click
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
    }

    private void LateUpdate()
    {
        // Reposition marker if we have a base
        if (hasBase && currentBaseMarker != null)
        {
            Vector3 newPos = map.GeoToWorldPosition(baseCoordinates, true);
            currentBaseMarker.transform.position = newPos;
        }
    }

    // ------------------------------------------
    //  UI Logic for Tab #1
    // ------------------------------------------
    public void HidePromptPanel()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    public void ShowBaseOnMap()
    {
        Debug.Log($"[BaseManager] ShowBaseOnMap called. hasBase={hasBase}");

        if (hasBase)
        {
            HidePromptPanel();
            if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.SetTabButtonsInteractable(true);

            // Center map on the base
            map.SetCenterLatitudeLongitude(baseCoordinates);
            map.UpdateMap();
        }
        else
        {
            // No base
            if (isPlacingBase)
            {
                HidePromptPanel();
                if (placeBaseMessage != null) placeBaseMessage.SetActive(true);

                // tabs locked from StartPlacingBase()
            }
            else
            {
                // Show prompt
                if (promptPanel != null) promptPanel.SetActive(true);
                if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

                var tm = FindObjectOfType<TabManager>();
                if (tm != null) tm.SetTabButtonsInteractable(true);
            }
        }
    }

    public void StartPlacingBase()
    {
        if (hasBase)
        {
            Debug.Log("Already have a base. Cannot place again.");
            return;
        }

        HidePromptPanel();
        if (placeBaseMessage != null) placeBaseMessage.SetActive(true);

        isPlacingBase = true;
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(false);
            tm.RefreshCurrentTabUI();
        }

        Debug.Log("[BaseManager] Now in base-placing mode => tabs locked.");
    }

    private void TryPlaceBaseAtScreenPosition(Vector2 screenPos)
    {
        if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

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
            Debug.Log("[BaseManager] We already have a base, ignoring placement.");
            return;
        }

        hasBase = true;
        baseCoordinates = location;
        PlaceBaseMarker(location);
        SaveBaseToFirebase(location);

        if (placeBaseButton != null)
            placeBaseButton.interactable = false;

        Debug.Log($"[BaseManager] Base confirmed at {location.x},{location.y}");

        // Done placing => unlock tabs
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(true);
            tm.RefreshCurrentTabUI();
        }
    }

    // ------------------------------------------
    //  Real-Time Base Updates
    // ------------------------------------------
    private void StartListeningToMyBase()
    {
        // Subscribe to changes for user’s "base" node
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base")
            .ValueChanged += OnMyBaseChanged;
    }

    public void UpdateUsernameInFirebase(string newUsername)
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("[BaseManager] DB not ready, skipping username update.");
            return;
        }

        DatabaseReference usernameRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base")
            .Child("username");

        usernameRef.SetValueAsync(newUsername).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("[BaseManager] Failed to update username in Firebase.");
            }
            else
            {
                Debug.Log("[BaseManager] Username updated in Firebase: " + newUsername);
            }
        });
    }


    private void OnMyBaseChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogWarning("[BaseManager] OnMyBaseChanged DB error: " + e.DatabaseError.Message);
            return;
        }

        var snap = e.Snapshot;
        if (!snap.Exists)
        {
            // Means the base node was removed
            hasBase = false;
            // Clean up marker, UI, etc. (Optional)
            return;
        }

        // Parse new values
        currentHealth = snap.HasChild("health") ? int.Parse(snap.Child("health").Value.ToString()) : 100;
        currentLevel = snap.HasChild("level") ? int.Parse(snap.Child("level").Value.ToString()) : 1;

        // Update local marker UI
        if (currentBaseMarker)
        {
            var marker = currentBaseMarker.GetComponent<BaseMarker>();
            if (marker != null)
            {
                // parse username as well
                string newUsername = snap.HasChild("username")
                    ? snap.Child("username").Value.ToString()
                    : "Unknown";

                marker.Initialize(playerId, newUsername, currentHealth, currentLevel);
            }
        }

        // Refresh any UI that displays base stats
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.RefreshCurrentTabUI();
        }

        Debug.Log($"[BaseManager] OnMyBaseChanged -> Health={currentHealth}, Level={currentLevel}");
    }

    // ------------------------------------------
    //  Firebase: Fetch/Save/Remove Base
    // ------------------------------------------
    private void FetchBaseFromFirebase()
    {
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("[BaseManager] Firebase not ready.");
            return;
        }

        Debug.Log("[BaseManager] Fetching base for " + playerId);
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[BaseManager] Fetch base failed.");
                    return;
                }

                var snap = task.Result;
                if (!snap.Exists)
                {
                    Debug.Log("[BaseManager] No base found for " + playerId);
                    hasBase = false;
                    return;
                }

                // We do have a base
                hasBase = true;

                double lat = double.Parse(snap.Child("latitude").Value.ToString());
                double lon = double.Parse(snap.Child("longitude").Value.ToString());
                baseCoordinates = new Vector2d(lat, lon);

                currentHealth = snap.HasChild("health") ? int.Parse(snap.Child("health").Value.ToString()) : 100;
                currentLevel = snap.HasChild("level") ? int.Parse(snap.Child("level").Value.ToString()) : 1;

                // Place local marker
                PlaceBaseMarker(baseCoordinates);
                if (currentBaseMarker != null)
                {
                    var localMarker = currentBaseMarker.GetComponent<BaseMarker>();
                    if (localMarker != null)
                    {
                        string username = snap.HasChild("username")
                            ? snap.Child("username").Value.ToString()
                            : "Unknown";
                        localMarker.Initialize(playerId, username, currentHealth, currentLevel);
                    }
                }

                // Start listening to real-time changes
                StartListeningToMyBase();

                // Refresh UI if needed
                var tm = FindObjectOfType<TabManager>();
                if (tm != null) tm.RefreshCurrentTabUI();

                Debug.Log($"[BaseManager] Found existing base. Health={currentHealth}, Level={currentLevel}");
            });
    }

    private void SaveBaseToFirebase(Vector2d coords)
    {
        DatabaseReference baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base");

        baseRef.Child("latitude").SetValueAsync(coords.x);
        baseRef.Child("longitude").SetValueAsync(coords.y);

        int defaultHealth = 100;
        int defaultLevel = 1;
        baseRef.Child("health").SetValueAsync(defaultHealth);
        baseRef.Child("level").SetValueAsync(defaultLevel);

        // Save username
        string usernameToSave = UsernameManager.Username;
        if (string.IsNullOrEmpty(usernameToSave))
        {
            usernameToSave = "Player_" + playerId.Substring(0, 5);
        }
        baseRef.Child("username").SetValueAsync(usernameToSave);

        currentHealth = defaultHealth;
        currentLevel = defaultLevel;

        Debug.Log("[BaseManager] Base saved under users/{playerId}/base");
        // Also start listening to changes if not already
        StartListeningToMyBase();
    }

    public void RemoveBase()
    {
        if (!hasBase)
        {
            Debug.Log("[BaseManager] No base to remove.");
            return;
        }

        // Refund
        ScoreManager.Instance.AddPoints(totalScoreSpentOnUpgrades);
        Debug.Log($"[BaseManager] Refunded {totalScoreSpentOnUpgrades} points.");

        totalScoreSpentOnUpgrades = 0;

        // Remove the 'base' node
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base")
            .RemoveValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[BaseManager] Remove base failed!");
                    return;
                }

                Debug.Log("[BaseManager] Base removed from Firebase.");

                currentHealth = 0;
                currentLevel = 0;
                hasBase = false;

                if (currentBaseMarker != null)
                {
                    Destroy(currentBaseMarker);
                    currentBaseMarker = null;
                }

                if (placeBaseButton != null)
                    placeBaseButton.interactable = true;

                // Force new username next time
                PlayerPrefs.DeleteKey("username");
                UsernameManager.ClearUsername(); // Use a method in UsernameManager

                // If user is on Tab1 => show prompt
                var tm = FindObjectOfType<TabManager>();
                if (tm != null && tm.CurrentTabIndex == 1)
                {
                    ShowBaseOnMap();
                    tm.RefreshCurrentTabUI();
                }
            });
    }

    // ------------------------------------------
    //  Upgrades
    // ------------------------------------------
    public void UpgradeBase()
    {
        int cost = upgradeCost;
        int currentScore = ScoreManager.Instance.GetCurrentScore();

        if (currentScore < cost)
        {
            Debug.Log("[BaseManager] Not enough points to upgrade!");
            return;
        }

        // Subtract points
        ScoreManager.Instance.AddPoints(-cost);

        // Track how much we spent for refund
        totalScoreSpentOnUpgrades += cost;

        // Actually upgrade
        currentLevel += 1;
        currentHealth += 100;

        // Update Firebase
        DatabaseReference baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base");
        baseRef.Child("level").SetValueAsync(currentLevel);
        baseRef.Child("health").SetValueAsync(currentHealth);

        // Update local marker UI
        if (currentBaseMarker)
        {
            var marker = currentBaseMarker.GetComponent<BaseMarker>();
            if (marker != null)
            {
                marker.UpdateStats(currentHealth, currentLevel);
            }
        }

        // Refresh Tab UI
        var tm = FindObjectOfType<TabManager>();
        if (tm != null) tm.RefreshCurrentTabUI();

        Debug.Log($"[BaseManager] Base upgraded! Lvl={currentLevel}, HP={currentHealth}. Cost={cost}");
    }

    // ------------------------------------------
    //  Utility
    // ------------------------------------------
    private void PlaceBaseMarker(Vector2d coords)
    {
        Vector3 wPos = map.GeoToWorldPosition(coords, true);
        if (!currentBaseMarker)
        {
            currentBaseMarker = Instantiate(baseMarkerPrefab, wPos, Quaternion.identity);
        }
        else
        {
            currentBaseMarker.transform.position = wPos;
        }
    }

    private bool ScreenPositionToLatLon(Vector2 screenPos, out Vector2d latLon)
    {
        latLon = Vector2d.zero;
        if (!map)
        {
            Debug.LogError("[BaseManager] No map reference to do lat/lon conversion!");
            return false;
        }

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
}
