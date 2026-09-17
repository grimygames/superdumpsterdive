using UnityEngine;
using TMPro;
using System.Collections;

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
			// Force the Unity engine clock to run normally in case a previous match paused it
			Time.timeScale = 1f;

			if (Launcher.IsTimeAttack)
			{
				soloUI.SetActive(true);
				multiplayerUI.SetActive(false);
				gameActive = true;
				
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

    public void AddTime(float amt) { timeLeft += amt; StartCoroutine(FlashGreen()); }
    public void OnItemFound() { score++; scoreText.text = "FOUND: " + score; }
    
    IEnumerator FlashGreen()
    {
        timerText.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        timerText.color = (timeLeft > 10) ? Color.white : Color.red;
    }
	
	void GameOver()
		{
			gameActive = false;
			
			// 1. TRIGGER THE NEW ANIMATED SCORE SCREEN
			if (DumpsterManager.Instance != null && DumpsterManager.Instance.activeModeLogic is SoloDumpsterMode soloMode)
			{
				soloMode.TriggerSoloTimeUp(endScreen, score);
			}
			else
			{
				// Failsafe: if the mode logic is missing, just turn the panel on normally
				endScreen.SetActive(true);
			}
			
			// 2. TRIGGER THE RAIN (Do NOT disable DumpsterManager!)
			if (DumpsterManager.Instance != null) {
				DumpsterManager.Instance.TriggerTrashStorm();
			}
		}
}