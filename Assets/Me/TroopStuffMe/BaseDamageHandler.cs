using UnityEngine;
using Firebase.Extensions;

public static class BaseDamageHandler
{
    public static void DealDamageToBase(string targetOwnerId, int damage, string attackerId)
    {
        // 1) Quick checks
        if (FirebaseInit.DBReference == null)
        {
            Debug.LogWarning("DBReference is null... (Firebase not ready?)");
            return;
        }
        if (string.IsNullOrEmpty(targetOwnerId))
        {
            Debug.LogWarning("DealDamageToBase called with null/empty targetOwnerId");
            return;
        }

        // 2) Grab a reference to the target owner's "base" node in DB
        var baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(targetOwnerId)
            .Child("base");

        // 3) Read that node from the DB
        baseRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Failed to retrieve target base for damage.");
                return;
            }

            var snap = task.Result;
            // 4) Check if base node exists
            if (!snap.Exists)
            {
                Debug.Log("Target base doesn't exist (maybe destroyed).");
                return;
            }

            // 5) We do have a base, parse health
            int oldHealth = snap.HasChild("health")
                ? int.Parse(snap.Child("health").Value.ToString())
                : 0;

            int newHealth = oldHealth - damage;

            if (newHealth <= 0)
            {
                // 6) If newHealth <= 0 => the base is destroyed
                Debug.Log("Base destroyed!");

                // read base level, defaulting to 1 if missing
                int baseLevel = snap.HasChild("level")
                    ? int.Parse(snap.Child("level").Value.ToString())
                    : 1;

                // Award 50 points per level
                int awardPoints = baseLevel * 50;

                // 7) Remove the base node from DB
                baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
                {
                    Debug.Log("Base removed from Firebase after destruction.");

                    // 8) Attacker gets the calculated points
                    AwardPoints(attackerId, awardPoints);

                    // 9) Also restore attacker’s base to full health (and set destroyedBaseNotify)
                    RestoreAttackerBase(attackerId);
                });
            }
            else
            {
                // 6b) Otherwise, just update the base’s new health
                baseRef.Child("health").SetValueAsync(newHealth);
            }
        });
    }

    private static void AwardPoints(string playerId, int points)
    {
        // 1) Reference 'score' node in DB
        var scoreRef = FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score");

        // 2) Read the existing score
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
            // 3) newScore = oldScore + points
            int newScore = oldScore + points;
            // 4) Set the new score in DB
            scoreRef.SetValueAsync(newScore);
        });
    }

    private static void RestoreAttackerBase(string attackerId)
    {
        // 1) Grab the attacker's own base node
        var attackerBaseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(attackerId)
            .Child("base");

        attackerBaseRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled) return;

            var snap = task.Result;
            if (!snap.Exists) return; // attacker might have no base

            // 2) read attacker’s level => set health => level * 100
            int level = snap.HasChild("level")
                ? int.Parse(snap.Child("level").Value.ToString())
                : 1;
            int newHealth = level * 100;
            attackerBaseRef.Child("health").SetValueAsync(newHealth);

            // 3) Also set 'destroyedBaseNotify' => true 
            // to show a short UI prompt client-side
            attackerBaseRef.Child("destroyedBaseNotify").SetValueAsync(true);
        });
    }
}
