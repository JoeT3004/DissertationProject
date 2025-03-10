using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Manages the local player's base: placing, upgrading, removing, and real-time sync with Firebase.
/// Also handles UI for Tab2, base prompts, base health/level, etc.
/// </summary>
public class BaseManager : MonoBehaviour
{
    public static BaseManager Instance { get; private set; }

    [Header("Map & Marker Setup")]
    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject baseMarkerPrefab;

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;      // Visible only on Tab1 if no base
    [SerializeField] private Button placeBaseButton;      // "Place Base" button
    [SerializeField] private GameObject placeBaseMessage; // "Tap the map" message
    [SerializeField] private TMP_Text healthRestored;

    [Header("Costs")]
    [SerializeField] private int upgradeCost = 50;

    private bool hasBase = false;
    private bool isPlacingBase = false;
    private string playerId;
    private GameObject currentBaseMarker;
    private Vector2d baseCoordinates;

    private int currentHealth;
    private int currentLevel;

    // Public properties
    public bool HasBase() => hasBase;
    public bool IsPlacingBase() => isPlacingBase;
    public int CurrentHealth => currentHealth;
    public int CurrentLevel => currentLevel;
    public bool IsPromptPanelActive() => promptPanel != null && promptPanel.activeSelf;

    /// <summary>
    /// Returns the lat/lon of the local player's base.
    /// </summary>
    public Vector2d GetBaseCoordinates() => baseCoordinates;

    /// <summary>
    /// Basic singleton pattern for BaseManager.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Fallback if map not assigned
        if (!map)
        {
            map = FindObjectOfType<AbstractMap>();
            if (!map)
            {
                Debug.LogError("[BaseManager] No AbstractMap found in scene!");
            }
        }
    }

    private IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        // Player ID
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
        }
        playerId = PlayerPrefs.GetString("playerId");
        Debug.Log("[BaseManager] Local playerId: " + playerId);

        if (promptPanel) promptPanel.SetActive(false);
        if (placeBaseMessage) placeBaseMessage.SetActive(false);
        if (placeBaseButton) placeBaseButton.interactable = true;

        // Fetch existing base from Firebase
        FetchBaseFromFirebase();
    }

    private void OnDestroy()
    {
        // Unsubscribe from "base" ValueChanged if we have a reference
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
        // If we do NOT have a base and are in "placing base" mode, 
        // check for tap or click to place the base
        if (!hasBase && isPlacingBase)
        {
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
        // Keep the local base marker position synced with the map
        if (hasBase && currentBaseMarker != null)
        {
            Vector3 newPos = map.GeoToWorldPosition(baseCoordinates, true);
            currentBaseMarker.transform.position = newPos;
        }
    }

    // ---------------------------
    // UI logic for Tab #1
    // ---------------------------

    /// <summary>
    /// Hides the base prompt panel if it exists.
    /// </summary>
    public void HidePromptPanel()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    /// <summary>
    /// Called (e.g. from TabManager) to re-center the map on the player's base (if they have one).
    /// Or show a prompt if they do not have a base yet.
    /// </summary>
    public void ShowBaseOnMap()
    {
        Debug.Log($"[BaseManager] ShowBaseOnMap called. hasBase={hasBase}");

        if (hasBase)
        {
            HidePromptPanel();
            if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

            var tm = FindObjectOfType<TabManager>();
            if (tm != null) tm.SetTabButtonsInteractable(true);

            // Center map on local base
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
            }
            else
            {
                // Show the prompt to place a base
                if (promptPanel != null) promptPanel.SetActive(true);
                if (placeBaseMessage != null) placeBaseMessage.SetActive(false);

                var tm = FindObjectOfType<TabManager>();
                if (tm != null) tm.SetTabButtonsInteractable(true);
            }
        }
    }

    /// <summary>
    /// Re-centers the map on an enemy base if the coords exist,
    /// disabling tab switching while the user looks at the enemy location.
    /// </summary>
    public void ShowEnemyBaseOnMap(Vector2d coords)
    {
        var tm = FindObjectOfType<TabManager>();
        if (tm != null) tm.SetTabButtonsInteractable(false);

        map.SetCenterLatitudeLongitude(coords);
        map.UpdateMap();
    }

    /// <summary>
    /// User initiates "placing base" mode, which locks out tab switching until the base is placed.
    /// </summary>
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

    /// <summary>
    /// Takes a screen position (touch/click), converts to lat/lon, and attempts to place the base there.
    /// </summary>
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

    /// <summary>
    /// Marks the local user as having a base at the given location, spawns the marker, and saves to Firebase.
    /// </summary>
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

    // ---------------------------
    // Real-time base updates
    // ---------------------------

    /// <summary>
    /// Subscribes to changes for the local user's "base" node in Firebase.
    /// </summary>
    private void StartListeningToMyBase()
    {
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base")
            .ValueChanged += OnMyBaseChanged;
    }

    /// <summary>
    /// Updates the base's "username" node in Firebase if DB is ready. 
    /// Called by UsernameManager or TabManager in some flows.
    /// </summary>
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

    /// <summary>
    /// Called whenever the user's base node changes in DB. Updates local health/level, 
    /// possibly shows a "destroyed" prompt if destroyedBaseNotify is set, etc.
    /// </summary>
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
            // Base removed
            hasBase = false;

            // Destroy local marker
            if (currentBaseMarker != null)
            {
                Destroy(currentBaseMarker);
                currentBaseMarker = null;
            }

            Debug.Log("[BaseManager] Local base is destroyed => removing local marker.");
            return;
        }

        // Parse new health/level
        currentHealth = snap.HasChild("health") ? int.Parse(snap.Child("health").Value.ToString()) : 100;
        currentLevel = snap.HasChild("level") ? int.Parse(snap.Child("level").Value.ToString()) : 1;

        // Check "destroyedBaseNotify"
        if (snap.HasChild("destroyedBaseNotify"))
        {
            bool notify = bool.Parse(snap.Child("destroyedBaseNotify").Value.ToString());
            if (notify)
            {
                ShowDestroyBasePrompt();
                // Reset the flag
                FirebaseInit.DBReference
                  .Child("users")
                  .Child(playerId)
                  .Child("base")
                  .Child("destroyedBaseNotify")
                  .SetValueAsync(false);
            }
        }

        // Update the local base marker UI
        if (currentBaseMarker)
        {
            var marker = currentBaseMarker.GetComponent<BaseMarker>();
            if (marker != null)
            {
                // Also parse the base's username
                string newUsername = snap.HasChild("username")
                    ? snap.Child("username").Value.ToString()
                    : "Unknown";

                marker.Initialize(playerId, newUsername, currentHealth, currentLevel);
            }
        }

        // Possibly refresh UI
        var tm = FindObjectOfType<TabManager>();
        if (tm != null) tm.RefreshCurrentTabUI();

        Debug.Log($"[BaseManager] OnMyBaseChanged -> Health={currentHealth}, Level={currentLevel}");
    }

    // ---------------------------
    // Firebase: fetch, save, remove
    // ---------------------------

    /// <summary>
    /// Checks Firebase for an existing base for this player, sets up local data, and starts listening to changes if found.
    /// </summary>
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

                // We have a base
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

    /// <summary>
    /// Creates or updates the "base" node for this user in Firebase with default health/level.
    /// Also sets username if missing.
    /// </summary>
    private void SaveBaseToFirebase(Vector2d coords)
    {
        var baseRef = FirebaseInit.DBReference
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
        StartListeningToMyBase(); // if not already
    }

    /// <summary>
    /// Removes the local user's base from Firebase, including awarding refunds for upgrades, 
    /// destroying local marker, clearing username, etc.
    /// </summary>
    public void RemoveBase()
    {
        if (!hasBase)
        {
            Debug.Log("[BaseManager] No base to remove.");
            return;
        }

        // Refund all upgrade cost spent
     //Calculates a refund based on current base level

        Debug.LogWarning(currentLevel);
        int refund = (currentLevel - 1) * upgradeCost / 2;
        ScoreManager.Instance.AddPoints(refund);

        Debug.LogWarning($"[BaseManager] Refunded {refund} points for level={currentLevel} base.");


        // Remove from DB
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
                UsernameManager.ClearUsername();

                var tm = FindObjectOfType<TabManager>();
                if (tm != null && tm.CurrentTabIndex == 1)
                {
                    ShowBaseOnMap(); // show prompt
                    tm.RefreshCurrentTabUI();
                }
            });
    }

    // ---------------------------
    // Upgrades
    // ---------------------------

    /// <summary>
    /// Spends "upgradeCost" points to increase base level by 1, health by 100. 
    /// Saves new values in Firebase.
    /// </summary>
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


        // Actually upgrade
        currentLevel += 1;
        currentHealth += 100;

        // Save new level/health in DB
        var baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("base");

        baseRef.Child("level").SetValueAsync(currentLevel);
        baseRef.Child("health").SetValueAsync(currentHealth);

        // Update marker if present
        if (currentBaseMarker)
        {
            var marker = currentBaseMarker.GetComponent<BaseMarker>();
            if (marker != null)
            {
                marker.UpdateStats(currentHealth, currentLevel);
            }
        }

        // Refresh UI
        var tm = FindObjectOfType<TabManager>();
        if (tm != null) tm.RefreshCurrentTabUI();

        Debug.Log($"[BaseManager] Base upgraded! Lvl={currentLevel}, HP={currentHealth}. Cost={cost}");
    }


    // ---------------------------
    // Utility
    // ---------------------------

    /// <summary>
    /// Spawns (or repositions) the local base marker prefab at the given geo coords.
    /// </summary>
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

    /// <summary>
    /// Converts a screen position to lat/lon by raycasting against a plane at y=0.
    /// </summary>
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

    /// <summary>
    /// Displays a brief UI notification that the user destroyed an enemy base. 
    /// Waits 3s, then hides.
    /// </summary>
    private void ShowDestroyBasePrompt()
    {
        Debug.Log("You destroyed an enemy base! Base fully restored.");
        string message = "You destroyed an enemy base! Base fully restored.";
        StartCoroutine(DisplayPrompt(message, 3f));
    }

    private IEnumerator DisplayPrompt(string message, float duration)
    {
        if (healthRestored != null)
        {
            healthRestored.text = message;
            healthRestored.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(duration);
        if (healthRestored != null)
        {
            healthRestored.gameObject.SetActive(false);
        }
    }
}
