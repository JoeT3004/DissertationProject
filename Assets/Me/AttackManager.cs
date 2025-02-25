using UnityEngine;
using Mapbox.Utils;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }

    [SerializeField] private TroopSelectPanel troopSelectPanel;
    [SerializeField] private int costPerTroop = 20;

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
        troopSelectPanel.Show(enemyOwnerId, enemyUsername, costPerTroop);
        
    }


    /// <summary>
    /// Called by TroopSelectPanel when user presses "Attack".
    /// </summary>
    public void LaunchAttack(string targetBaseOwnerId, int troopCount)
    {
        if (!BaseManager.Instance.HasBase())
        {
            Debug.LogWarning("Can't launch an attack: player has no base!");
            return;
        }

        // 1) Calculate total cost
        int totalCost = troopCount * costPerTroop;
        int currentScore = ScoreManager.Instance.GetCurrentScore();

        // 2) Check if user has enough score
        if (currentScore < totalCost)
        {
            Debug.LogWarning($"Not enough score. Need {totalCost}, have {currentScore}. Aborting attack.");
            return;
        }

        // 3) Deduct cost
        ScoreManager.Instance.AddPoints(-totalCost);

        // 4) Continue your existing logic
        var myBaseLatLon = BaseManager.Instance.GetBaseCoordinates();
        var targetCoords = AllBasesManager.Instance.GetBaseCoordinates(targetBaseOwnerId);
        if (targetCoords == null)
        {
            Debug.LogWarning("Target base not found!");
            return;
        }

        for (int i = 0; i < troopCount; i++)
        {
            CreateNewTroopRecord(myBaseLatLon, targetCoords.Value, targetBaseOwnerId);
        }

        Debug.Log($"Launched attack on {targetBaseOwnerId} with {troopCount} troops. Cost {totalCost} points.");
    }


    private void CreateNewTroopRecord(
    Vector2d startCoords,
    Vector2d endCoords,
    string targetBaseOwnerId)
    {
        string troopId = System.Guid.NewGuid().ToString();
        var troopRef = FirebaseInit.DBReference
            .Child("troops")
            .Child(troopId);

        // We assume attacker is local player:
        string attackerUsername = UsernameManager.Username; // from your existing code
                                                            // We get target's username from AllBasesManager or DB. 
                                                            // If AllBasesManager doesn't have that method, you'll need to implement it
        string targetUsername = AllBasesManager.Instance.GetUsernameOfUser(targetBaseOwnerId);

        var troopData = new System.Collections.Generic.Dictionary<string, object>();
        troopData["attackerId"] = PlayerPrefs.GetString("playerId");
        troopData["attackerUsername"] = attackerUsername;  // store it
        troopData["targetBaseOwnerId"] = targetBaseOwnerId;
        troopData["targetUsername"] = targetUsername;      // store it

        troopData["startLat"] = startCoords.x;
        troopData["startLon"] = startCoords.y;
        troopData["currentLat"] = startCoords.x;  // at spawn
        troopData["currentLon"] = startCoords.y;
        troopData["endLat"] = endCoords.x;
        troopData["endLon"] = endCoords.y;
        troopData["damage"] = 50;

        troopRef.SetValueAsync(troopData).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Failed to create new troop record in Firebase.");
            }
            else
            {
                Debug.Log($"Created troop record: {troopId}");
            }
        });
    }



}
