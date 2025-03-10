using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Manages all troops in the scene by listening to the Firebase "troops" node. 
/// Whenever the DB changes, it clears existing troops and spawns the updated list.
/// </summary>
public class AllTroopsManager : MonoBehaviour
{
    public static AllTroopsManager Instance { get; private set; }

    [SerializeField] private AbstractMap map;

    [Header("Troop Prefabs")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject alienPrefab;
    [SerializeField] private GameObject robotPrefab;

    // Dictionary to track spawned troops by their troopId in DB
    private readonly Dictionary<string, GameObject> spawnedTroops = new Dictionary<string, GameObject>();

    /// <summary>
    /// Ensures we only have one instance of AllTroopsManager in the scene.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Waits for Firebase, then subscribes to "troops" ValueChanged.
    /// </summary>
    private IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        FirebaseInit.DBReference
            .Child("troops")
            .ValueChanged += OnTroopsValueChanged;
    }

    /// <summary>
    /// Removes the DB listener on destroy, preventing memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (FirebaseInit.DBReference != null)
        {
            FirebaseInit.DBReference.Child("troops").ValueChanged -= OnTroopsValueChanged;
        }
    }

    /// <summary>
    /// Called whenever the "troops" node in Firebase changes. 
    /// It spawns or removes troops in the scene accordingly.
    /// </summary>
    private void OnTroopsValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllTroopsManager] troops DB error: " + e.DatabaseError.Message);
            return;
        }

        // Clear existing troop GameObjects
        foreach (var kvp in spawnedTroops)
        {
            Destroy(kvp.Value);
        }
        spawnedTroops.Clear();

        if (!e.Snapshot.Exists) return; // no troops in DB

        // Iterate each troop entry in DB
        foreach (var troopSnapshot in e.Snapshot.Children)
        {
            string troopId = troopSnapshot.Key;

            // Must have troopType, currentLat, currentLon
            if (!troopSnapshot.HasChild("troopType")) continue;
            if (!troopSnapshot.HasChild("currentLat") || !troopSnapshot.HasChild("currentLon")) continue;

            // Figure out which prefab to use
            string troopTypeString = troopSnapshot.Child("troopType").Value.ToString();
            AttackManager.TroopType troopType = (AttackManager.TroopType)
                System.Enum.Parse(typeof(AttackManager.TroopType), troopTypeString);
            GameObject prefabToSpawn = GetTroopPrefab(troopType);

            // Convert lat/lon to world space
            double curLat = double.Parse(troopSnapshot.Child("currentLat").Value.ToString());
            double curLon = double.Parse(troopSnapshot.Child("currentLon").Value.ToString());
            Vector2d coords = new Vector2d(curLat, curLon);
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);

            // Instantiate
            GameObject troopGO = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
            TroopController controller = troopGO.GetComponent<TroopController>();
            if (controller != null)
            {
                controller.SetMap(map);
                controller.InitFromSnapshot(troopId, troopSnapshot);
            }

            spawnedTroops[troopId] = troopGO;
        }
    }

    /// <summary>
    /// Helper method to pick the correct troop prefab based on TroopType.
    /// </summary>
    private GameObject GetTroopPrefab(AttackManager.TroopType t)
    {
        switch (t)
        {
            case AttackManager.TroopType.Ghost:
                return ghostPrefab;
            case AttackManager.TroopType.Alien:
                return alienPrefab;
            case AttackManager.TroopType.Robot:
                return robotPrefab;
            default:
                return ghostPrefab;
        }
    }
}
