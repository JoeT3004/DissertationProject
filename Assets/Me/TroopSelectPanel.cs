using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TroopSelectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text targetBaseText;
    [SerializeField] private TMP_Text troopCountText;

    [SerializeField] private TMP_Text troopCostUI;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button attackButton;

    // Add the cancel button reference
    [SerializeField] private Button cancelButton;

    private int troopCount = 1;
    private string enemyOwnerId;
    private string enemyUsername;
    private int costPerTroop;
    private void Awake()
    {
        /*
        
        plusButton.onClick.RemoveAllListeners();
        plusButton.onClick.AddListener(OnPlusClicked);

        minusButton.onClick.RemoveAllListeners();
        minusButton.onClick.AddListener(OnMinusClicked);

        attackButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(OnAttackClicked);

        // And if you have a cancelButton, do the same
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(OnCancelClicked);
        */
    }


    public void Show(string enemyOwnerId, string enemyUsername, int costPerTroop)
    {

        Debug.Log("[TroopSelectPanel] Show => Called with " + enemyOwnerId);
        this.enemyOwnerId = enemyOwnerId;
        this.enemyUsername = enemyUsername;
        this.costPerTroop = costPerTroop;
        troopCount = 1;

        if (targetBaseText != null)
            targetBaseText.text = $"Troop Select! \n Attacking: {enemyUsername}";

        if (troopCostUI != null)
            troopCostUI.text = $"Troop Cost: {costPerTroop}";

        RefreshTroopCountText();
        gameObject.SetActive(true);

    }

    private void RefreshTroopCountText()
    {
        if (troopCountText != null)
            troopCountText.text = troopCount.ToString();
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
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(true); // unlock tabs
        }
        gameObject.SetActive(false);
    }

    public void OnCancelClicked()
    {
        var tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.SetTabButtonsInteractable(true);
        }
        gameObject.SetActive(false);
    }

}
