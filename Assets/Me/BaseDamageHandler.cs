using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public static class BaseDamageHandler
{
    public static void DealDamageToBase(string targetOwnerId, int damage, string attackerId)
    {

        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("DBReference is null, cannot deal damage. " +
                             "Make sure FirebaseInit is in the scene and IsFirebaseReady is true.");
            return;
        }

        if (string.IsNullOrEmpty(targetOwnerId))
        {
            Debug.LogWarning("DealDamageToBase called with an empty or null targetOwnerId.");
            return;
        }
        // 1) Grab reference to the target base’s "health"
        var baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(targetOwnerId)
            .Child("base");

        baseRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Failed to retrieve target base for damage.");
                return;
            }

            var snap = task.Result;
            if (!snap.Exists)
            {
                Debug.Log("Target base no longer exists (maybe destroyed).");
                return;
            }

            int oldHealth = snap.HasChild("health")
                ? int.Parse(snap.Child("health").Value.ToString())
                : 0;

            int newHealth = oldHealth - damage;
            if (newHealth <= 0)
            {
                // base destroyed
                Debug.Log("Base destroyed!");

                // remove base
                baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
                {
                    Debug.Log("Base removed from Firebase after destruction.");

                    // Award attacker points
                    AwardPoints(attackerId, 100); // or dynamic points
                });
            }
            else
            {
                // Just update the new health
                baseRef.Child("health").SetValueAsync(newHealth);
            }
        });
    }

    private static void AwardPoints(string playerId, int points)
    {
        // Add the points to the attacker’s "score"
        var scoreRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score");

        scoreRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Failed to retrieve attacker’s score for awarding points.");
                return;
            }

            int oldScore = 0;
            if (task.Result.Exists)
            {
                oldScore = int.Parse(task.Result.Value.ToString());
            }
            int newScore = oldScore + points;
            scoreRef.SetValueAsync(newScore);
        });
    }
}
