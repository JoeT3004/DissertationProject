using System.Collections;
using TMPro;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private int currentScore = 50;

    private string playerId;

    private void Awake()
    {
        // Basic singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait until Firebase is ready
        yield return new WaitUntil(() => FirebaseInit.IsFirebaseReady);

        // Retrieve or create playerId
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", newId);
        }
        playerId = PlayerPrefs.GetString("playerId");

        // Load initial score (just once)
        LoadScoreFromFirebase();

        // **Subscribe to real-time score updates**
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score")
            .ValueChanged += OnScoreChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe if the DBReference still exists
        if (FirebaseInit.DBReference != null && !string.IsNullOrEmpty(playerId))
        {
            FirebaseInit.DBReference
                .Child("users")
                .Child(playerId)
                .Child("score")
                .ValueChanged -= OnScoreChanged;
        }
    }

    // ----------- Score Real-Time Listener -----------
    private void OnScoreChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogWarning("[ScoreManager] OnScoreChanged error: " + e.DatabaseError.Message);
            return;
        }

        if (!e.Snapshot.Exists)
        {
            // If no score node, re-init to 50
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

    // ------------------------------------------------

    private void LoadScoreFromFirebase()
    {
        FirebaseInit.DBReference
            .Child("users")
            .Child(playerId)
            .Child("score")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[ScoreManager] Failed to load score from Firebase.");
                    UpdateScoreUI();
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    // New player
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

    // Called by BaseManager or any script that modifies the score
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

    public int GetCurrentScore() => currentScore;
}
