using UnityEngine;
using Mapbox.Utils;
using System.Collections;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }

    [SerializeField] private TroopSelectPanel troopSelectPanel;


    [Header("Troop Damage")]
    [SerializeField] private int ghostDamage = 50;
    [SerializeField] private int alienDamage = 20;
    [SerializeField] private int robotDamage = 80;

    // ADD matching cost fields so AttackManager knows each cost:
    [Header("Troop Costs")]
    [SerializeField] private int ghostCost = 20;
    [SerializeField] private int alienCost = 25;
    [SerializeField] private int robotCost = 40;

    public int GhostCost => ghostCost;
    public int AlienCost => alienCost;
    public int RobotCost => robotCost;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Hide troop panel initially
        troopSelectPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by BaseTapHandler when player taps an enemy base.
    /// </summary>
    public void OpenTroopSelectionUI(string enemyOwnerId, string enemyUsername, int enemyHealth, int enemyLevel)
    {
        Debug.Log("[AttackManager] OpenTroopSelectionUI => Called");
        // Optionally check if user has a base
        if (!BaseManager.Instance.HasBase())
        {
            Debug.LogWarning("Cannot attack: user has no base, skipping UI.");
            return;
        }

        Debug.Log("[AttackManager] HasBase() == true, showing TroopSelectPanel");
        troopSelectPanel.gameObject.SetActive(true);
        troopSelectPanel.Show(enemyOwnerId, enemyUsername);
        
    }


    /// <summary>
    /// Called by TroopSelectPanel when user presses "Attack".
    /// </summary>
    public void LaunchAttack(string targetBaseOwnerId, int troopCount, TroopType troopType)
    {
        if (!BaseManager.Instance.HasBase())
        {
            Debug.LogWarning("Can't launch an attack: player has no base!");
            return;
        }

        // 1) Retrieve the correct cost & damage for the chosen troop type
        int costPerTroop = 0;
        int damageValue = 0;
        switch (troopType)
        {
            case TroopType.Ghost:
                costPerTroop = ghostCost;
                damageValue = ghostDamage;
                break;
            case TroopType.Alien:
                costPerTroop = alienCost;
                damageValue = alienDamage;
                break;
            case TroopType.Robot:
                costPerTroop = robotCost;
                damageValue = robotDamage;
                break;
        }

        // 2) Now cost is separate from damage
        int totalCost = troopCount * costPerTroop;

        // 3) Check user’s score
        int currentScore = ScoreManager.Instance.GetCurrentScore();
        if (currentScore < totalCost)
        {
            Debug.LogWarning($"Not enough score. Need {totalCost}, have {currentScore}. Aborting attack.");
            return;
        }
        // 4) Deduct cost
        ScoreManager.Instance.AddPoints(-totalCost);

        // (The rest is the same:)
        var myBaseLatLon = BaseManager.Instance.GetBaseCoordinates();
        var targetCoords = AllBasesManager.Instance.GetBaseCoordinates(targetBaseOwnerId);
        if (targetCoords == null)
        {
            Debug.LogWarning("Target base not found!");
            return;
        }

        // 5) Spawn them in sequence:
        StartCoroutine(SpawnTroopQueue(myBaseLatLon, targetCoords.Value, targetBaseOwnerId, troopCount, troopType, damageValue));

        Debug.Log($"Launched attack with {troopCount} {troopType}, Cost={totalCost}, Damage={damageValue}");
    }

    private IEnumerator SpawnTroopQueue(
        Vector2d startCoords, Vector2d endCoords,
        string targetBaseOwnerId, int troopCount,
        TroopType troopType, int damageValue)
    {
        for (int i = 0; i < troopCount; i++)
        {
            CreateNewTroopRecord(startCoords, endCoords, targetBaseOwnerId, troopType, damageValue);
            yield return new WaitForSeconds(3f);
        }
    }



    public enum TroopType
    {
        Ghost,
        Alien, // replaced Wizard with Alien
        Robot
    }





    private void CreateNewTroopRecord(Vector2d startCoords, Vector2d endCoords,
                                  string targetBaseOwnerId, TroopType troopType,
                                  int damageValue)
    {
        string troopId = System.Guid.NewGuid().ToString();
        var troopRef = FirebaseInit.DBReference.Child("troops").Child(troopId);

        var troopData = new Dictionary<string, object>();
        troopData["troopType"] = troopType.ToString();
        troopData["damage"] = damageValue;

        troopData["attackerId"] = PlayerPrefs.GetString("playerId");
        troopData["attackerUsername"] = UsernameManager.Username;

        // Get the target’s actual username from AllBasesManager:
        string targetUsername = AllBasesManager.Instance.GetUsernameOfUser(targetBaseOwnerId);
        // Store BOTH:
        troopData["targetBaseOwnerId"] = targetBaseOwnerId;
        troopData["targetUsername"] = targetUsername;

        // Etc...
        troopData["startLat"] = startCoords.x;
        troopData["startLon"] = startCoords.y;
        troopData["currentLat"] = startCoords.x;
        troopData["currentLon"] = startCoords.y;
        troopData["endLat"] = endCoords.x;
        troopData["endLon"] = endCoords.y;

        troopRef.SetValueAsync(troopData).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                Debug.LogWarning("Failed to create new troop record in Firebase.");
            else
                Debug.Log($"Created troop record: {troopId}");
        });
    }





}
