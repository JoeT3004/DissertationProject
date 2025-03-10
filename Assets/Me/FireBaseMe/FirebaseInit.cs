using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// Initializes Firebase and creates a static DatabaseReference for the entire app to use.
/// Also sets IsFirebaseReady once everything is loaded.
/// </summary>
public class FirebaseInit : MonoBehaviour
{
    public static FirebaseApp App;
    public static DatabaseReference DBReference;
    public static bool IsFirebaseReady { get; private set; } = false;

    private void Start()
    {
        InitializeFirebase();
    }

    /// <summary>
    /// Checks and fixes Firebase dependencies, then sets up App and DBReference if successful.
    /// </summary>
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                App = FirebaseApp.DefaultInstance;
                Debug.Log("DEBUG: Current DatabaseURL is: " + App.Options.DatabaseUrl);

                DBReference = FirebaseDatabase.DefaultInstance.RootReference;
                IsFirebaseReady = true;

                Debug.Log("[FirebaseInit] Firebase is ready!");
            }
            else
            {
                Debug.LogError($"[FirebaseInit] Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }
}
