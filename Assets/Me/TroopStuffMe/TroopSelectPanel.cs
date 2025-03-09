using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mapbox.Utils;

public class TroopSelectPanel : MonoBehaviour
{
    // Enemy info
    public string enemyOwnerId;
    private string enemyUsername;

    // ========== GHOST UI FIELDS ==========
    [Header("Ghost Troop UI")]
    [SerializeField] private TMP_Text troopCostText_Ghost;
    [SerializeField] private TMP_Text troopCountText_Ghost;
    [SerializeField] private TMP_Text timeToReachText_Ghost;
    [SerializeField] private Button plusButton_Ghost;
    [SerializeField] private Button minusButton_Ghost;

    // ========== ALIEN UI FIELDS ==========
    [Header("Alien Troop UI (Wizard Model)")]
    [SerializeField] private TMP_Text troopCostText_Alien;
    [SerializeField] private TMP_Text troopCountText_Alien;
    [SerializeField] private TMP_Text timeToReachText_Alien;
    [SerializeField] private Button plusButton_Alien;
    [SerializeField] private Button minusButton_Alien;

    // ========== ROBOT UI FIELDS ==========
    [Header("Robot Troop UI")]
    [SerializeField] private TMP_Text troopCostText_Robot;
    [SerializeField] private TMP_Text troopCountText_Robot;
    [SerializeField] private TMP_Text timeToReachText_Robot;
    [SerializeField] private Button plusButton_Robot;
    [SerializeField] private Button minusButton_Robot;

    // ========== ATTACK / CANCEL BUTTONS ==========
    [Header("Panel Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button cancelButton;

    




    // ========== INTERNAL COUNTERS ==========
    private int ghostCount = 0;
    private int alienCount = 0;
    private int robotCount = 0;

    // ========== Troop Speed Prototypes (Optional) ==========
    [Header("Prefabs / Speed Prototypes")]
    [SerializeField] private TroopController ghostTroopPrototype;
    [SerializeField] private TroopController alienTroopPrototype;
    [SerializeField] private TroopController robotTroopPrototype;

    private void OnEnable()
    {
        // Reset counts each time the panel opens
        ghostCount = 0;
        alienCount = 0;
        robotCount = 0;

        RefreshAllUI();

        // Hook up plus/minus
        plusButton_Ghost.onClick.AddListener(OnPlusGhostClicked);
        minusButton_Ghost.onClick.AddListener(OnMinusGhostClicked);

        plusButton_Alien.onClick.AddListener(OnPlusAlienClicked);
        minusButton_Alien.onClick.AddListener(OnMinusAlienClicked);

        plusButton_Robot.onClick.AddListener(OnPlusRobotClicked);
        minusButton_Robot.onClick.AddListener(OnMinusRobotClicked);

        // Hook up Attack / Cancel
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

        // Optionally lock tabs while panel is open
        SetTabButtonsInteractable(false);
    }

    private void OnDisable()
    {
        // Unhook all the listeners to avoid duplication
        plusButton_Ghost.onClick.RemoveListener(OnPlusGhostClicked);
        minusButton_Ghost.onClick.RemoveListener(OnMinusGhostClicked);

        plusButton_Alien.onClick.RemoveListener(OnPlusAlienClicked);
        minusButton_Alien.onClick.RemoveListener(OnMinusAlienClicked);

        plusButton_Robot.onClick.RemoveListener(OnPlusRobotClicked);
        minusButton_Robot.onClick.RemoveListener(OnMinusRobotClicked);

        if (attackButton != null) attackButton.onClick.RemoveListener(OnAttackClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);

        // Optionally unlock tabs if the panel closes
        SetTabButtonsInteractable(true);
    }

    public void Show(string enemyOwnerId, string enemyUsername)
    {
        this.enemyOwnerId = enemyOwnerId;
        this.enemyUsername = enemyUsername;
        // baseCost param is optional; we have costGhost/costAlien/costRobot internally.
        gameObject.SetActive(true);

        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        troopCostText_Ghost.text = $"Cost: {AttackManager.Instance.GhostCost}";
        troopCostText_Alien.text = $"Cost: {AttackManager.Instance.AlienCost}";
        troopCostText_Robot.text = $"Cost: {AttackManager.Instance.RobotCost}";

        // Show counts
        troopCountText_Ghost.text = ghostCount.ToString();
        troopCountText_Alien.text = alienCount.ToString();
        troopCountText_Robot.text = robotCount.ToString();

        // Recalc times from the prototypes, etc...
        UpdateTimeEstimateForType(ghostTroopPrototype, timeToReachText_Ghost);
        UpdateTimeEstimateForType(alienTroopPrototype, timeToReachText_Alien);
        UpdateTimeEstimateForType(robotTroopPrototype, timeToReachText_Robot);
    }


    private void UpdateTimeEstimateForType(TroopController prototype, TMP_Text timeText)
    {
        if (prototype == null || timeText == null) return;

        Vector2d? myBaseCoords = BaseManager.Instance.GetBaseCoordinates();
        Vector2d? enemyBaseCoords = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);

        if (!myBaseCoords.HasValue || !enemyBaseCoords.HasValue)
        {
            timeText.text = "Time: ???";
            return;
        }

        double distanceMeters = GeoUtils.HaversineDistance(myBaseCoords.Value, enemyBaseCoords.Value);
        double speedMps = prototype.speed;
        if (speedMps <= 0) speedMps = 0.0001;

        double travelTimeSeconds = distanceMeters / speedMps;
        double mins = travelTimeSeconds / 60.0;
        timeText.text = $"Time: {mins:F1} min";
    }

    // ==================== BUTTON HANDLERS FOR PLUS/MINUS ====================
    private void OnPlusGhostClicked() { ghostCount++; RefreshAllUI(); }
    private void OnMinusGhostClicked() { if (ghostCount > 0) ghostCount--; RefreshAllUI(); }

    private void OnPlusAlienClicked() { alienCount++; RefreshAllUI(); }
    private void OnMinusAlienClicked() { if (alienCount > 0) alienCount--; RefreshAllUI(); }

    private void OnPlusRobotClicked() { robotCount++; RefreshAllUI(); }
    private void OnMinusRobotClicked() { if (robotCount > 0) robotCount--; RefreshAllUI(); }

    // ==================== ATTACK / CANCEL ====================
    private void OnAttackClicked()
    {
        // Launch attacks for each type that has > 0
        if (ghostCount > 0)
            AttackManager.Instance.LaunchAttack(enemyOwnerId, ghostCount, AttackManager.TroopType.Ghost);

        if (alienCount > 0)
            AttackManager.Instance.LaunchAttack(enemyOwnerId, alienCount, AttackManager.TroopType.Alien);

        if (robotCount > 0)
            AttackManager.Instance.LaunchAttack(enemyOwnerId, robotCount, AttackManager.TroopType.Robot);

        // Hide this panel
        gameObject.SetActive(false);

        // Re-enable tabs
        SetTabButtonsInteractable(true);
    }

    private void OnCancelClicked()
    {
        // Just close the panel, do nothing else
        gameObject.SetActive(false);

        // Re-enable tabs
        SetTabButtonsInteractable(true);
    }

    private void SetTabButtonsInteractable(bool interactable)
    {
        TabManager tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(interactable);
        }
    }
}
