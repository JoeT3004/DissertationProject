using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;

/// <summary>
/// Handles the player's score, stored in Firebase under "users/{playerId}/score".
/// Listens in real-time and updates the UI (scoreText).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private int currentScore = 50;

    private string playerId;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait for Firebase
        yield return new WaitUntil(() => FirebaseInit.IsFirebaseReady);

        // Make sure we have a playerId
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
        }
        playerId = PlayerPrefs.GetString("playerId");

        // Load initial score
        LoadScoreFromFirebase();

        // Subscribe to real-time changes of "score"
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score")
            .ValueChanged += OnScoreChanged;
    }

    private void OnDestroy()
    {
        if (FirebaseInit.DBReference != null && !string.IsNullOrEmpty(playerId))
        {
            FirebaseInit.DBReference
                .Child("users")
                .Child(playerId)
                .Child("score")
                .ValueChanged -= OnScoreChanged;
        }
    }

    /// <summary>
    /// Called whenever the "score" node changes in DB. Updates local and UI.
    /// </summary>
    private void OnScoreChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogWarning("[ScoreManager] OnScoreChanged error: " + e.DatabaseError.Message);
            return;
        }

        if (!e.Snapshot.Exists)
        {
            // If no score node => re-init to 50
            currentScore = 50;
            SaveScoreToFirebase();
        }
        else
        {
            currentScore = int.Parse(e.Snapshot.Value.ToString());
        }

        UpdateScoreUI();
        Debug.Log("[ScoreManager] OnScoreChanged -> new score = " + currentScore);
    }

    /// <summary>
    /// Loads initial score from DB. If none found, sets to 50.
    /// </summary>
    private void LoadScoreFromFirebase()
    {
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[ScoreManager] Failed to load score from Firebase.");
                    UpdateScoreUI();
                    return;
                }

                var snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    currentScore = 50;
                    SaveScoreToFirebase();
                }
                else
                {
                    currentScore = int.Parse(snapshot.Value.ToString());
                }
                UpdateScoreUI();
                Debug.Log($"[ScoreManager] Loaded initial score: {currentScore}");
            });
    }

    /// <summary>
    /// Persists the current score to Firebase under "users/{playerId}/score".
    /// </summary>
    private void SaveScoreToFirebase()
    {
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score")
            .SetValueAsync(currentScore)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[ScoreManager] Failed to save score to Firebase.");
                }
                else
                {
                    Debug.Log($"[ScoreManager] Score saved to Firebase: {currentScore}");
                }
            });
    }

    /// <summary>
    /// Adds 'points' to currentScore (can be negative), then updates DB.
    /// </summary>
    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        SaveScoreToFirebase();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    /// <summary>
    /// Returns the current local score. 
    /// </summary>
    public int GetCurrentScore() => currentScore;
}
