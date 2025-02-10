using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseApp App; // optional static reference
    public static DatabaseReference DBReference;

    private void Awake()
    {
        // Make this persist across scene loads (optional, but recommended)
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        // Initialize check on start
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        // Checks that all Firebase dependencies are present
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Create and hold reference to the Firebase app
                App = FirebaseApp.DefaultInstance;

                // Create a reference to the root of the database
                DBReference = FirebaseDatabase.DefaultInstance.RootReference;

                Debug.Log("Firebase is ready!");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }
}
