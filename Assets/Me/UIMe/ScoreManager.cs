using System.Collections;
using TMPro;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TMP_Text scoreText;  // Use TextMeshProUGUI if preferred
    [SerializeField] private int currentScore = 50;

    private void Awake()
    {
        // Basic singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        // Wait until Firebase is ready before loading the score.
        yield return new WaitUntil(() => FirebaseInit.IsFirebaseReady);
        LoadScoreFromFirebase();
    }

    /// <summary>
    /// Adds (or subtracts) points and immediately updates Firebase.
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

    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Loads the player's score from Firebase.
    /// If no score exists (i.e. new player), initializes it to 50.
    /// </summary>
    private void LoadScoreFromFirebase()
    {
        string playerId = PlayerPrefs.GetString("playerId");
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
                if (snapshot.Exists)
                {
                    currentScore = int.Parse(snapshot.Value.ToString());
                    Debug.Log($"[ScoreManager] Loaded score: {currentScore}");
                }
                else
                {
                    // New player: score doesn't exist yet so initialize with the default of 50.
                    currentScore = 50;
                    SaveScoreToFirebase();
                    Debug.Log("[ScoreManager] No existing score found. Initializing with 50 points.");
                }
                UpdateScoreUI();
            });
    }

    /// <summary>
    /// Saves the current score to Firebase.
    /// </summary>
    private void SaveScoreToFirebase()
    {
        string playerId = PlayerPrefs.GetString("playerId");
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
}
