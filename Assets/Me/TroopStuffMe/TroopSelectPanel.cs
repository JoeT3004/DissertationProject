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
    // ^ The "TroopController" on your troop prefab (drag it from Project window).
    // We'll read "speed" from this in real time.

    private int troopCount = 1;
    private string enemyOwnerId;
    private string enemyUsername;
    private int costPerTroop;

    private bool isPanelActive;

    private void OnEnable()
    {
        isPanelActive = true;
    }

    private void OnDisable()
    {
        isPanelActive = false;
    }

    private void Update()
    {
        // If the panel is open, recalc the estimated time each frame,
        // so it updates if we change speed in the inspector.
        if (!isPanelActive) return;

        RecalculateEstimatedTime();
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

        // Get the local user’s base and enemy base coordinates
        Vector2d? myBase = BaseManager.Instance.GetBaseCoordinates();
        Vector2d? enemyBase = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (myBase.HasValue && enemyBase.HasValue)
        {
            // Use the Haversine formula to get the distance in meters
            double distanceMeters = GeoUtils.HaversineDistance(myBase.Value, enemyBase.Value);
            // Read the troop speed (in meters/second) from a prototype reference
            // (Assumes you have a TroopController reference set up in the inspector)
            float troopSpeedMps = (troopPrototype != null) ? troopPrototype.speed : 1f;
            double travelTimeSeconds = distanceMeters / troopSpeedMps;

            if (estimatedTimeText != null)
            {
                double mins = travelTimeSeconds / 60.0;
                estimatedTimeText.text = $"Estimated time to reach {enemyUsername}:\n{mins:F1} minutes ({travelTimeSeconds:F0} sec)";
            }
        }

        RefreshTroopCountText();
        gameObject.SetActive(true);
    }


    private void RefreshTroopCountText()
    {
        if (troopCountText != null)
            troopCountText.text = troopCount.ToString();
    }

    /// <summary>
    /// Recompute the distance and time based on the user’s base to the enemy’s base,
    /// plus the current speed from 'troopPrototype'.
    /// </summary>
    private void RecalculateEstimatedTime()
    {
        // Must have a troopPrototype reference and an 'enemyOwnerId'
        if (troopPrototype == null || string.IsNullOrEmpty(enemyOwnerId)) return;

        var myBase = BaseManager.Instance.GetBaseCoordinates(); // local coords
        var enemyBase = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (!enemyBase.HasValue) return; // no coords

        double dist = Vector2d.Distance(myBase, enemyBase.Value);
        float currentSpeed = troopPrototype.speed; // read from inspector in real time
        if (currentSpeed <= 0f) currentSpeed = 0.0001f; // avoid div by zero

        double travelTimeSeconds = dist / currentSpeed;

        if (estimatedTimeText != null)
        {
            double mins = travelTimeSeconds / 60.0;
            estimatedTimeText.text =
                $"Estimated:\n{mins:F1} min ({travelTimeSeconds:F0} s) @ speed={currentSpeed}";
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
    }

    public void OnCancelClicked()
    {
        gameObject.SetActive(false);
    }
}
