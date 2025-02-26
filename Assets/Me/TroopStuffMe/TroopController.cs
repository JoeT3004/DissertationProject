using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Mapbox.Utils;
using Mapbox.Unity.Map;


public class TroopController : MonoBehaviour
{

    private AbstractMap map;
    public void SetMap(AbstractMap map)
    {
        this.map = map;

        // If we also have a TroopScaler, initialize it
        var scaler = GetComponent<TroopScaleAdjuster>();
        if (scaler != null)
        {
            scaler.Initialize(map);
        }
    }



    private string troopId;
    private string attackerId;
    private string targetBaseOwnerId;
    private Vector2d startCoords;
    private Vector2d endCoords;
    private int damage;
    [SerializeField] public float speed = 0.0001f; // "speed" in lat/lon per second just for demonstration

    private Vector2d currentCoords;

    private DatabaseReference troopRef;
    private bool isArrived = false;

    public void InitFromSnapshot(string troopId, DataSnapshot snapshot)
    {
        this.troopId = troopId;

        attackerId = snapshot.Child("attackerId").Value.ToString();
        string attackerUsername = snapshot.HasChild("attackerUsername")
            ? snapshot.Child("attackerUsername").Value.ToString()
            : "Unknown";

        targetBaseOwnerId = snapshot.Child("targetBaseOwnerId").Value.ToString();
        string targetUsername = snapshot.HasChild("targetUsername")
            ? snapshot.Child("targetUsername").Value.ToString()
            : "Unknown";

        damage = int.Parse(snapshot.Child("damage").Value.ToString());

        double sLat = double.Parse(snapshot.Child("startLat").Value.ToString());
        double sLon = double.Parse(snapshot.Child("startLon").Value.ToString());
        double eLat = double.Parse(snapshot.Child("endLat").Value.ToString());
        double eLon = double.Parse(snapshot.Child("endLon").Value.ToString());
        double cLat = double.Parse(snapshot.Child("currentLat").Value.ToString());
        double cLon = double.Parse(snapshot.Child("currentLon").Value.ToString());

        startCoords = new Vector2d(sLat, sLon);
        endCoords = new Vector2d(eLat, eLon);
        currentCoords = new Vector2d(cLat, cLon);

        troopRef = FirebaseInit.DBReference.Child("troops").Child(troopId);

        double travelTimeSec = snapshot.HasChild("travelTimeSec")
            ? double.Parse(snapshot.Child("travelTimeSec").Value.ToString())
            : 0.0;

        var ui = GetComponentInChildren<TroopUIController>();
        if (ui != null)
        {
            ui.SetTexts(attackerUsername, targetUsername);
            ui.SetTimeLeft(travelTimeSec);
        }
    }



    private void Update()
    {
        if (isArrived) return;

        double distance = Vector2d.Distance(currentCoords, endCoords);
        if (distance < 0.00001)
        {
            OnArriveAtTarget();
            return;
        }

        // Move a fraction
        float step = speed * Time.deltaTime;
        double t = step / distance;
        currentCoords = Vector2d.Lerp(currentCoords, endCoords, t);

        // Convert lat/lon -> world
        Vector3 worldPos = map.GeoToWorldPosition(currentCoords, true);
        transform.position = worldPos;

        // Update Firebase
        UpdateTroopPositionInDB(currentCoords);

        // **New**: Update the "time left"
        double timeLeftSec = distance / speed;
        UpdateUITimeLeft(timeLeftSec);
    }

    private void UpdateUITimeLeft(double timeLeftSec)
    {
        var ui = GetComponentInChildren<TroopUIController>();
        if (ui != null)
        {
            ui.SetTimeLeft(timeLeftSec);
        }
    }


    private void UpdateTroopPositionInDB(Vector2d coords)
    {
        if (troopRef == null) return;
        troopRef.Child("currentLat").SetValueAsync(coords.x);
        troopRef.Child("currentLon").SetValueAsync(coords.y);
    }

private void OnArriveAtTarget()
{
    // If the DB is not ready, skip or delay damage
    if (!FirebaseInit.IsFirebaseReady || FirebaseInit.DBReference == null)
    {
        Debug.LogWarning("Cannot deal damage yet, Firebase not ready. Will skip for now.");
        // Optionally set a flag to handle it later, or just remove the troop
        Destroy(gameObject);
        return;
    }

    // Proceed with the existing logic
    BaseDamageHandler.DealDamageToBase(targetBaseOwnerId, damage, attackerId);
    troopRef.RemoveValueAsync();
    Destroy(gameObject);
}

}
