using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections.Generic;

public class AllTroopsManager : MonoBehaviour
{
    public static AllTroopsManager Instance { get; private set; }

    [SerializeField] private AbstractMap map;
    [SerializeField] private GameObject troopPrefab;

    private Dictionary<string, GameObject> spawnedTroops = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private System.Collections.IEnumerator Start()
    {
        while (!FirebaseInit.IsFirebaseReady)
            yield return null;

        // Listen for changes under "troops"
        FirebaseInit.DBReference
            .Child("troops")
            .ValueChanged += OnTroopsValueChanged;
    }

    private void OnDestroy()
    {
        if (FirebaseInit.DBReference != null)
        {
            FirebaseInit.DBReference.Child("troops").ValueChanged -= OnTroopsValueChanged;
        }
    }

    private void OnTroopsValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("[AllTroopsManager] troops DB error: " + e.DatabaseError.Message);
            return;
        }

        // Clear old
        foreach (var kvp in spawnedTroops)
        {
            Destroy(kvp.Value);
        }
        spawnedTroops.Clear();

        if (!e.Snapshot.Exists) return; // no troops

        foreach (var troopSnapshot in e.Snapshot.Children)
        {
            string troopId = troopSnapshot.Key;

            // Read data
            if (!troopSnapshot.HasChild("attackerId") ||
                !troopSnapshot.HasChild("currentLat") ||
                !troopSnapshot.HasChild("currentLon"))
            {
                // Invalid troop entry?
                continue;
            }

            string attackerId = troopSnapshot.Child("attackerId").Value.ToString();
            double curLat = double.Parse(troopSnapshot.Child("currentLat").Value.ToString());
            double curLon = double.Parse(troopSnapshot.Child("currentLon").Value.ToString());

            Vector2d coords = new Vector2d(curLat, curLon);
            Vector3 worldPos = map.GeoToWorldPosition(coords, true);


            var troopGO = Instantiate(troopPrefab, worldPos, Quaternion.identity);
            var troopController = troopGO.GetComponent<TroopController>();
            if (troopController != null)
            {
                troopController.SetMap(map);  // <---- Provide the map reference
                troopController.InitFromSnapshot(troopId, troopSnapshot);
            }
            spawnedTroops[troopId] = troopGO;
        }
    }



    private void LateUpdate()
    {
        // In LateUpdate, we simply let each TroopController handle its own movement,
        // or you can re-position them if you store lat/lon in this manager.
        // Typically, each TroopController will check the DB or use RPC approach
        // to update its own position.
    }
}

