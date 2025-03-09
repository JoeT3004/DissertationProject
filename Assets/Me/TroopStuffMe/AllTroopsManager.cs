using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Mapbox.Unity.Map;
using Mapbox.Utils;
public class AllTroopsManager : MonoBehaviour
{
    public static AllTroopsManager Instance { get; private set; }

    [SerializeField] private AbstractMap map;
    [Header("Troop Prefabs")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject alienPrefab;
    [SerializeField] private GameObject robotPrefab;

    private Dictionary<string, GameObject> spawnedTroops = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait for Firebase to be ready
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        // Listen for changes under "troops"
        FirebaseInit.DBReference.Child("troops").ValueChanged += OnTroopsValueChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (FirebaseInit.DBReference != null)
            FirebaseInit.DBReference.Child("troops").ValueChanged -= OnTroopsValueChanged;
    }

    private void OnTroopsValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllTroopsManager] troops DB error: " + e.DatabaseError.Message);
            return;
        }

        // Clear out old troops
        foreach (var kvp in spawnedTroops)
            Destroy(kvp.Value);
        spawnedTroops.Clear();

        // If "troops" node is empty, do nothing
        if (!e.Snapshot.Exists) return;

        // Iterate each troop
        foreach (var troopSnapshot in e.Snapshot.Children)
        {
            string troopId = troopSnapshot.Key;

            // Grab the "troopType" to pick correct prefab
            if (!troopSnapshot.HasChild("troopType")) continue;
            string troopTypeString = troopSnapshot.Child("troopType").Value.ToString();

            AttackManager.TroopType troopType = (AttackManager.TroopType)
                 System.Enum.Parse(typeof(AttackManager.TroopType), troopTypeString);

            // Decide which prefab
            GameObject prefabToSpawn = ghostPrefab;
            switch (troopType)
            {
                case AttackManager.TroopType.Ghost: prefabToSpawn = ghostPrefab; break;
                case AttackManager.TroopType.Alien: prefabToSpawn = alienPrefab; break;
                case AttackManager.TroopType.Robot: prefabToSpawn = robotPrefab; break;
            }

            // read lat/lon
            if (!troopSnapshot.HasChild("currentLat") || !troopSnapshot.HasChild("currentLon"))
                continue; // skip if missing

            double curLat = double.Parse(troopSnapshot.Child("currentLat").Value.ToString());
            double curLon = double.Parse(troopSnapshot.Child("currentLon").Value.ToString());
            Vector2d coords = new Vector2d(curLat, curLon);
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);

            // Instantiate
            GameObject troopGO = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
            TroopController ctrl = troopGO.GetComponent<TroopController>();
            if (ctrl != null)
            {
                ctrl.SetMap(map);
                ctrl.InitFromSnapshot(troopId, troopSnapshot);
            }

            spawnedTroops[troopId] = troopGO;
        }
    }
}


