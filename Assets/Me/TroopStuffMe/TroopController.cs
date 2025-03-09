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
        // Initialize any attached scaler if needed.
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

    private float accumulatedTime = 0f; // total time since last actual update


    // Speed is now expressed in meters/second
    [SerializeField] public float speed = 100f;

    private Vector2d currentCoords;
    // Total journey distance (in meters), computed once in InitFromSnapshot.
    private double totalDistanceMeters;

    private DatabaseReference troopRef;
    private bool isArrived = false;

    private float updateTimer = 0f;


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

        // Compute total journey distance in meters once:
        totalDistanceMeters = GeoUtils.HaversineDistance(startCoords, endCoords);

        troopRef = FirebaseInit.DBReference.Child("troops").Child(troopId);

        var ui = GetComponentInChildren<TroopUIController>();
        if (ui != null)
        {
            ui.SetTexts(attackerUsername, targetUsername);
            // Optionally, initialize the UI time with the full travel time:
            double fullTravelTime = totalDistanceMeters / speed;
            ui.SetTimeLeft(fullTravelTime);
        }
    }

    private void Update()
    {
        float dt;
        if (!ShouldUpdateThisFrame(out dt)) return;
        // If false, we skip this frame’s movement.

        if (isArrived) return;

        // distance to the target
        double distanceMeters = GeoUtils.HaversineDistance(currentCoords, endCoords);
        if (distanceMeters < 1.0)
        {
            OnArriveAtTarget();
            return;
        }

        // Move the troop as if 'dt' seconds of real time have passed
        float step = speed * dt;  // e.g. if dt=1.0, step = speed * 1 => big jump
        double fraction = step / distanceMeters;
        currentCoords = Vector2d.Lerp(currentCoords, endCoords, fraction);

        // update world position
        Vector3 worldPos = map.GeoToWorldPosition(currentCoords, true);
        transform.position = worldPos;

        // update firebase
        UpdateTroopPositionInDB(currentCoords);

        // UI time left
        double timeLeftSec = distanceMeters / speed - dt;
        // ^ we subtract dt from the “travel time left” so it’s consistent.

        if (timeLeftSec < 0) timeLeftSec = 0; // clamp

        UpdateUITimeLeft(timeLeftSec);
    }



    private bool ShouldUpdateThisFrame(out float actualDt)
    {
        // If not in reduce load mode, do normal approach.
        if (!TabManager.IsReduceLoadMode())
        {
            actualDt = Time.deltaTime;
            return true;
        }

        // If in reduce load mode, we accumulate time until 1 second has passed
        accumulatedTime += Time.deltaTime;
        if (accumulatedTime >= 1f)
        {
            actualDt = accumulatedTime;  // use the entire accumulated time
            accumulatedTime = 0f;
            return true;
        }

        actualDt = 0f;
        return false;
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
        if (!FirebaseInit.IsFirebaseReady || FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("Cannot deal damage yet, Firebase not ready. Will skip for now.");
            Destroy(gameObject);
            return;
        }
        BaseDamageHandler.DealDamageToBase(targetBaseOwnerId, damage, attackerId);
        troopRef.RemoveValueAsync();
        Destroy(gameObject);
    }
}
