using UnityEngine;
using TMPro;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class TimeAttackLogic : MonoBehaviour
{
    public GameObject soloUI;
    public GameObject multiplayerUI;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public GameObject endScreen;
    
    public float timeLeft = 60f;
    private int score = 0;
    private bool gameActive = false;

	void Start()
    {
        Time.timeScale = 1f;

        if (Launcher.IsTimeAttack)
        {
            soloUI.SetActive(true);
            multiplayerUI.SetActive(false);
            gameActive = true;
            
            // SET THE TIMER BASED ON THE LAUNCHER PRESET
            timeLeft = Launcher.SoloMatchDuration;
            
            GameSync gs = FindObjectOfType<GameSync>();
            if (gs != null) { gs.enabled = false; }
        }
        else
        {
            soloUI.SetActive(false);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (!gameActive) return;
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            if (timeLeft < 10) timerText.color = Color.red;
        }
        else GameOver();
    }

    public void AddTime(float amt) 
    { 
        timeLeft += amt; 
        StartCoroutine(FlashTimer(amt)); 
    }
    public void OnItemFound() { score++; scoreText.text = "FOUND: " + score; }
    
    IEnumerator FlashTimer(float amt)
    {
        // Flash Green for bonuses, Red for penalties
        timerText.color = amt > 0 ? Color.green : Color.red;
        yield return new WaitForSeconds(0.5f);
        
        // Return to standard white (or red if they are naturally under 10 seconds)
        timerText.color = (timeLeft > 10) ? Color.white : Color.red;
    }
	
	public void GameOver()
    {
        gameActive = false;
        
        SaveSoloHighScore(score); // Save the score before showing the UI
        
        if (DumpsterManager.Instance != null && DumpsterManager.Instance.activeModeLogic is SoloDumpsterMode soloMode)
        {
            soloMode.TriggerSoloTimeUp(endScreen, score);
        }
        else
        {
            endScreen.SetActive(true);
        }
        
        if (DumpsterManager.Instance != null) {
            DumpsterManager.Instance.TriggerTrashStorm();
        }
    }

	private void SaveSoloHighScore(int finalScore)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // 1. Submit the Score to the correct preset Statistic
        string statName = "SoloScore_" + Launcher.ActiveSoloPresetIndex;

        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = statName, Value = finalScore }
            }
        }, 
        result => Debug.Log($"Successfully submitted {statName} score: {finalScore}"),
        error => Debug.LogError("Error submitting score: " + error.ErrorMessage));

        // 2. Upload their current graphics as PUBLIC data so the Leaderboard can read them
        string currentAvatar = PlayerPrefs.GetString("SavedAvatar", "");
        string currentBorder = "default_border";
        
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            currentBorder = PlayFabAuthBridge.Instance.profile.selectedBorderId;
        }

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PublicAvatar", currentAvatar },
                { "PublicBorder", currentBorder }
            },
            Permission = UserDataPermission.Public // This is required for other clients to fetch it!
        }, 
        res => Debug.Log("Public leaderboard graphics updated."), 
        err => Debug.LogError("Error saving public graphics: " + err.ErrorMessage));
    }
}