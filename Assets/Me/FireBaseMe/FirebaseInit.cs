using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseInit : MonoBehaviour
{


    public static FirebaseApp App;
    public static DatabaseReference DBReference;
    public static bool IsFirebaseReady { get; private set; } = false;

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                App = FirebaseApp.DefaultInstance;

                // Log the URL to check if it’s null or empty
                Debug.Log("DEBUG: Current DatabaseURL is: " + App.Options.DatabaseUrl);

                DBReference = FirebaseDatabase.DefaultInstance.RootReference;
                IsFirebaseReady = true;

                Debug.Log("Firebase is ready!");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

}
