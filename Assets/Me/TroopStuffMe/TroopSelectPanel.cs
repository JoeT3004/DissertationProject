using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mapbox.Utils;

public class TroopSelectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text targetBaseText;
    [SerializeField] private TMP_Text troopCountText;
    [SerializeField] private TMP_Text troopCostUI;
    [SerializeField] private TMP_Text estimatedTimeText;

    [Header("Prefabs / References")]
    [SerializeField] private TroopController troopPrototype;
    // This reference should be set by dragging the TroopController script 
    // from your troop prefab (or an instance of it) from the Project window.

    private int troopCount = 1;
    private string enemyOwnerId;
    private string enemyUsername;
    private int costPerTroop;

    private bool isPanelActive;

    private void OnEnable()
    {
        isPanelActive = true;
        // Recalculate once when enabled.
        RecalculateEstimatedTime();
    }

    private void OnDisable()
    {
        isPanelActive = false;
    }

    private void Update()
    {
        // If you want the panel to update in real time when speed changes,
        // you can recalc each frame:
        if (isPanelActive)
        {
            RecalculateEstimatedTime();
        }
    }

    public void Show(string enemyOwnerId, string enemyUsername, int costPerTroop)
    {
        this.enemyOwnerId = enemyOwnerId;
        this.enemyUsername = enemyUsername;
        this.costPerTroop = costPerTroop;
        troopCount = 1;

        if (targetBaseText != null)
            targetBaseText.text = $"Troop Select!\nAttacking: {enemyUsername}";

        if (troopCostUI != null)
            troopCostUI.text = $"Troop Cost: {costPerTroop}";

        // Calculate the estimated travel time using full distance (as if the troop had not moved)
        RecalculateEstimatedTime();

        RefreshTroopCountText();
        gameObject.SetActive(true);
    }

    private void RefreshTroopCountText()
    {
        if (troopCountText != null)
            troopCountText.text = troopCount.ToString();
    }

    /// <summary>
    /// Recalculate the estimated travel time from the local base to the enemy base,
    /// using GeoUtils.HaversineDistance (meters) and the current troop speed (in m/s).
    /// This value remains constant (as if the troop had not moved).
    /// </summary>
    private void RecalculateEstimatedTime()
    {
        if (troopPrototype == null || string.IsNullOrEmpty(enemyOwnerId)) return;

        Vector2d? myBase = BaseManager.Instance.GetBaseCoordinates();
        Vector2d? enemyBase = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (!myBase.HasValue || !enemyBase.HasValue) return;

        // Use the Haversine formula to get the distance in meters.
        double distanceMeters = GeoUtils.HaversineDistance(myBase.Value, enemyBase.Value);
        // Read the troop speed from the prototype (in m/s)
        float troopSpeedMps = (troopPrototype != null) ? troopPrototype.speed : 1f;
        if (troopSpeedMps <= 0f) troopSpeedMps = 0.0001f;
        double travelTimeSeconds = distanceMeters / troopSpeedMps;

        if (estimatedTimeText != null)
        {
            double mins = travelTimeSeconds / 60.0;
            estimatedTimeText.text = $"Estimated time to reach {enemyUsername}:\n{mins:F1} min ({travelTimeSeconds:F0} s) @ speed={troopSpeedMps}";
        }
    }

    public void OnPlusClicked()
    {
        troopCount++;
        RefreshTroopCountText();
    }

    public void OnMinusClicked()
    {
        if (troopCount > 1)
            troopCount--;
        RefreshTroopCountText();
    }

    public void OnAttackClicked()
    {
        AttackManager.Instance.LaunchAttack(enemyOwnerId, troopCount);
        gameObject.SetActive(false);

        SetTabButtonsInteractable();

        //UNLOCK Tabs
    }

    public void OnCancelClicked()
    {
        gameObject.SetActive(false);

        SetTabButtonsInteractable();
        //UNlockTABS
    }

    private void SetTabButtonsInteractable(){
        TabManager tm = FindObjectOfType<TabManager>();

        if (tm != null)
        {
            tm.SetTabButtonsInteractable(true);
        }

    }
}
