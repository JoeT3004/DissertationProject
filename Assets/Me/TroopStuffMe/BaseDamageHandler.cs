using UnityEngine;
using Firebase.Extensions;

/// <summary>
/// Static utility for dealing damage to an enemy base in Firebase. 
/// If the base reaches 0 health, it's destroyed, awarding points to the attacker.
/// </summary>
public static class BaseDamageHandler
{
    /// <summary>
    /// Reads the target base from DB, applies damage, and either updates health or destroys the base.
    /// </summary>
    public static void DealDamageToBase(string targetOwnerId, int damage, string attackerId)
    {
        // Basic checks
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

        var baseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(targetOwnerId)
            .Child("base");

        // Grab the target base
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
                Debug.Log("Target base doesn't exist (maybe destroyed).");
                return;
            }

            // Parse existing health
            int oldHealth = snap.HasChild("health")
                ? int.Parse(snap.Child("health").Value.ToString())
                : 0;

            int newHealth = oldHealth - damage;
            if (newHealth <= 0)
            {
                Debug.Log("Base destroyed!");

                // Parse base level, awarding 50 points per level
                int baseLevel = snap.HasChild("level")
                    ? int.Parse(snap.Child("level").Value.ToString())
                    : 1;
                int awardPoints = baseLevel * 50;

                // Remove base node
                baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
                {
                    Debug.Log("Base removed from Firebase after destruction.");
                    AwardPoints(attackerId, awardPoints);
                    RestoreAttackerBase(attackerId);
                });
            }
            else
            {
                // Just update health
                baseRef.Child("health").SetValueAsync(newHealth);
            }
        });
    }

    /// <summary>
    /// Awards the specified number of points to the player's score in DB.
    /// </summary>
    private static void AwardPoints(string playerId, int points)
    {
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

    /// <summary>
    /// Restores the attacker's base to full health and sets 'destroyedBaseNotify' => true. 
    /// The client can show a prompt once this is set.
    /// </summary>
    private static void RestoreAttackerBase(string attackerId)
    {
        var attackerBaseRef = FirebaseInit.DBReference
            .Child("users")
            .Child(attackerId)
            .Child("base");

        attackerBaseRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled) return;
            var snap = task.Result;
            if (!snap.Exists) return;

            // Full health = baseLevel * 100
            int level = snap.HasChild("level")
                ? int.Parse(snap.Child("level").Value.ToString())
                : 1;
            int newHealth = level * 100;
            attackerBaseRef.Child("health").SetValueAsync(newHealth);

            // Let the attacker know they've destroyed a base
            attackerBaseRef.Child("destroyedBaseNotify").SetValueAsync(true);
        });
    }
}
