using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EndGameTally : MonoBehaviour
{
    [Header("UI Text Displays (Inside notepadP1)")]
    public TMP_Text timeTakenText;
    public TMP_Text myScoreText;
    public TMP_Text opponentScoreText; 
	public TMP_Text copperEarnedText;

    [Header("Notepad Page GameObjects")]
    [Tooltip("Assign 'notepadP1' from your hierarchy here")]
    public GameObject notepadP1; // Stats Page
    [Tooltip("Assign 'notepadP2' from your hierarchy here")]
    public GameObject notepadP2; // Items Found Page
    [Tooltip("Assign 'notepadP3' from your hierarchy here")]
    public GameObject notepadP3; // Items Used Page

    [Header("Icon Stamp Layouts")]
    [Tooltip("Assign 'ItemsFoundContainer' from inside notepadP2")]
    public Transform itemsFoundContainer; 
    [Tooltip("Assign 'ItemsUsedContainer' from inside notepadP3")]
    public Transform itemsUsedContainer;  

    [Header("Prefabs & Settings")]
    public GameObject iconStampPrefab; 
    public float countDuration = 1.5f; 
    public float stampDelay = 0.15f;   

    // Local data storage caches to preserve data across tab swaps
    private List<Sprite> cachedFoundSprites = new List<Sprite>();
    private List<Sprite> cachedUsedSprites = new List<Sprite>();

    // Booleans to track if the player has triggered the stamp animations yet
    private bool hasStampedFound = false;
    private bool hasStampedUsed = false;
    private bool hasScoresCountedUp = false;

    // References to keep active stamp sequences from conflicting
    private Coroutine activeStampCoroutine;

    public void BeginTally(float finalTime, int finalMyScore, int finalOppScore, List<Sprite> found, List<Sprite> used)
    {
        Debug.Log($"<color=cyan>--- TALLY GAME DATA INITIALIZED ---</color>");
        
        // Cache data payload references immediately 
        cachedFoundSprites = new List<Sprite>(found);
        cachedUsedSprites = new List<Sprite>(used);

        // Reset system flags for a fresh game loop
        hasStampedFound = false;
        hasStampedUsed = false;
        hasScoresCountedUp = false;

        // Clean out any leftover elements from previous matches
        if (itemsFoundContainer) foreach (Transform t in itemsFoundContainer) Destroy(t.gameObject);
        if (itemsUsedContainer) foreach (Transform t in itemsUsedContainer) Destroy(t.gameObject);

        // Force initialize visibility onto page 1 (notepadP1)
        SwitchToStatsTab();

        // Fire off the rapid point count up on page 1
        StartCoroutine(TallyRoutine(finalTime, finalMyScore, finalOppScore));
    }

	private IEnumerator TallyRoutine(float finalTime, int finalMyScore, int finalOppScore)
		{
			if (hasScoresCountedUp) yield break;

			if (timeTakenText) timeTakenText.text = "0s";
			if (myScoreText) myScoreText.text = "0";
			if (opponentScoreText) opponentScoreText.text = "0";
			
			// ADDED: Safely update copper if the text field and manager exist
			if (copperEarnedText && DumpsterManager.Instance != null) 
			{
				copperEarnedText.text = DumpsterManager.Instance.currentSessionRewards.copperEarned.ToString();
			}

			// Quick unscaled wait frame buffer
			float waitElapsed = 0f;
			while (waitElapsed < 0.3f)
			{
				waitElapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			float elapsed = 0f;
			while (elapsed < countDuration)
			{
				elapsed += Time.unscaledDeltaTime; 
				float t = elapsed / countDuration;

				float currentTime = Mathf.Lerp(0, finalTime, t);
				float currentMyScore = Mathf.Lerp(0, finalMyScore, t);
				float currentOppScore = Mathf.Lerp(0, finalOppScore, t);

				if (timeTakenText) timeTakenText.text = Mathf.FloorToInt(currentTime).ToString() + "s";
				if (myScoreText) myScoreText.text = Mathf.FloorToInt(currentMyScore).ToString();
				if (opponentScoreText) opponentScoreText.text = Mathf.FloorToInt(currentOppScore).ToString();

				yield return null;
			}

			// Hard snaps
			if (timeTakenText) timeTakenText.text = Mathf.FloorToInt(finalTime).ToString() + "s";
			if (myScoreText) myScoreText.text = finalMyScore.ToString();
			if (opponentScoreText) opponentScoreText.text = finalOppScore.ToString();
			
			if (copperEarnedText != null) 
				{
					if (DumpsterManager.Instance != null)
					{
						copperEarnedText.text = DumpsterManager.Instance.currentSessionRewards.copperEarned.ToString();
						Debug.Log("<color=cyan>[END TALLY] Successfully updated UI with Copper: </color>" + DumpsterManager.Instance.currentSessionRewards.copperEarned);
					}
				}
        else
        {
            // IF YOU SEE THIS RED ERROR IN YOUR CONSOLE, YOUR UI SLOT IS EMPTY!
            Debug.LogError("🚨 COPPER UI MISSING! You forgot to drag your Copper Text object into the 'Copper Earned Text' slot on the EndGameTally script in the Inspector!");
        }

			hasScoresCountedUp = true;
		}

    // --- TAB NAVIGATION METHODS (Attach these directly to soloStatsTAB, soloitemsFoundTAB, and soloitemsUsedTAB) ---

    public void SwitchToStatsTab()
    {
        StopActiveStampCoroutine();
        SetPageVisibility(true, false, false);
    }

    public void SwitchToItemsFoundTab()
    {
        StopActiveStampCoroutine();
        SetPageVisibility(false, true, false);

        // Animation check: Stamp if first time, show instantly if returning
        if (!hasStampedFound)
        {
            hasStampedFound = true;
            activeStampCoroutine = StartCoroutine(StampSequenceRoutine(cachedFoundSprites, itemsFoundContainer));
        }
        else
        {
            PopulateInstantStaticIcons(cachedFoundSprites, itemsFoundContainer);
        }
    }

    public void SwitchToItemsUsedTab()
    {
        StopActiveStampCoroutine();
        SetPageVisibility(false, false, true);

        // Animation check: Stamp if first time, show instantly if returning
        if (!hasStampedUsed)
        {
            hasStampedUsed = true;
            activeStampCoroutine = StartCoroutine(StampSequenceRoutine(cachedUsedSprites, itemsUsedContainer));
        }
        else
        {
            PopulateInstantStaticIcons(cachedUsedSprites, itemsUsedContainer);
        }
    }

    // --- PROCESSING HELPER LOOPS ---

    private void SetPageVisibility(bool p1, bool p2, bool p3)
    {
        if (notepadP1) notepadP1.SetActive(p1);
        if (notepadP2) notepadP2.SetActive(p2);
        if (notepadP3) notepadP3.SetActive(p3);
    }

    private void StopActiveStampCoroutine()
    {
        if (activeStampCoroutine != null)
        {
            StopCoroutine(activeStampCoroutine);
            activeStampCoroutine = null;
        }
    }

    private IEnumerator StampSequenceRoutine(List<Sprite> spriteList, Transform container)
    {
        if (container == null) yield break;

        // Wipe any partial elements before starting a fresh sequence
        foreach (Transform t in container) Destroy(t.gameObject);

        foreach (Sprite s in spriteList)
        {
            StampIcon(s, container, true);
            
            float stampWait = 0f;
            while (stampWait < stampDelay)
            {
                stampWait += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        activeStampCoroutine = null;
    }

    private void PopulateInstantStaticIcons(List<Sprite> spriteList, Transform container)
    {
        if (container == null) return;

        // Clean out objects so layouts don't duplicate build meshes stacked up
        foreach (Transform t in container) Destroy(t.gameObject);

        // Instantiate solid elements immediately without pop transitions
        foreach (Sprite s in spriteList)
        {
            StampIcon(s, container, false);
        }
    }

    private void StampIcon(Sprite s, Transform parent, bool playAnimation)
    {
        if (iconStampPrefab == null) return;
        
        GameObject icon = Instantiate(iconStampPrefab, parent);
        Image img = icon.GetComponent<Image>();
        if (img) 
        {
            img.sprite = s;
            img.preserveAspect = true; 
        }
        
        if (playAnimation)
        {
            StartCoroutine(PopAnimation(icon.transform));
        }
        else
        {
            icon.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator PopAnimation(Transform t)
    {
        t.localScale = Vector3.one * 3f; 
        float elapsed = 0f;
        float dur = 0.2f; 

        while (elapsed < dur)
        {
            if (t == null) yield break; // Safety clear check
            elapsed += Time.unscaledDeltaTime; 
            t.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one, elapsed / dur);
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }
}