using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabLeaderboard : MonoBehaviour
{
    public static PlayFabLeaderboard Instance;

    public const string STAT_HIGH_SCORE = "HighScore";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Submits a new score to PlayFab. PlayFab only updates if the new score is higher than the previous one.
    /// </summary>
    public void SubmitScore(int score)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("[Leaderboard] Cannot submit score: Player is not logged in.");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = STAT_HIGH_SCORE, Value = score }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, 
            result => Debug.Log($"[Leaderboard] Score of {score} successfully submitted!"),
            error => Debug.LogError($"[Leaderboard] Score submission failed: {error.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// Fetches the top global players from the PlayFab leaderboard.
    /// </summary>
    public void FetchGlobalLeaderboard(int maxResults, System.Action<List<PlayerLeaderboardEntry>> onSuccess)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_HIGH_SCORE,
            StartPosition = 0,
            MaxResultsCount = maxResults
        };

        PlayFabClientAPI.GetLeaderboard(request, 
            result => onSuccess?.Invoke(result.Leaderboard),
            error => Debug.LogError($"[Leaderboard] Failed to retrieve leaderboard: {error.GenerateErrorReport()}")
        );
    }
}