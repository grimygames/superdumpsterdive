using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SoloDumpsterMode : MonoBehaviour, IDumpsterMode
{
    private DumpsterManager manager;
    private TimeAttackLogic timeAttackLogic;

    void Awake()
    {
        manager = GetComponent<DumpsterManager>();
    }

    void Start()
    {
        timeAttackLogic = Object.FindObjectOfType<TimeAttackLogic>();
        manager.matchStartTime = Time.time;
    }

    public void HandleItemClicked(string itemName, GameObject itemObject, TrashData data)
    {
        if (data == null) return;

        if (data.timeBonus > 0 && timeAttackLogic != null)
        {
            timeAttackLogic.AddTime(data.timeBonus);
        }

        Vector2 bagPos = (itemObject != null) ? itemObject.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        switch (data.type)
        {
            case TrashData.ItemType.Needle:
                if (timeAttackLogic != null) timeAttackLogic.AddTime(-10f);
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(false); 
                if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("OUCH! HIT A NEEDLE!", Color.white);
                if (itemObject != null) Destroy(itemObject);
                // 🌟 PARABOLIC FIX: Converted flat spawn rules to clean falling animations
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;

            case TrashData.ItemType.Diaper:
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;

            case TrashData.ItemType.Bag:
                int burstCount = Random.Range(5, 9);
                for (int i = 0; i < burstCount; i++)
                {
                    TrashData spilledData = manager.GetRandomTrashData();
                    // 🌟 NESTED LOOP SHIELD: Blocks nested bags from populating
                    while (spilledData.type == TrashData.ItemType.Bag)
                    {
                        spilledData = manager.GetRandomTrashData();
                    }
                    Vector2 offsetTarget = bagPos + (Random.insideUnitCircle * 150f);
                    manager.SpawnItemUI(spilledData, true, bagPos, offsetTarget);
                }
                if (itemObject != null) Destroy(itemObject);
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;
                
            case TrashData.ItemType.WalkieTalkie:
                HighlightSoloTargets();
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;
                
                
            case TrashData.ItemType.Raccoon:
                Debug.Log("SOLO RACCOON: You woke the beast!");
                StartCoroutine(RunJumpscareAnimation(itemObject, data));
                if (timeAttackLogic != null) timeAttackLogic.AddTime(-5f);
                return;
				
			case TrashData.ItemType.TakeoutBox:
				if (itemObject != null) Destroy(itemObject);
				// 80% chance for Cookie, 20% chance for Rancid
				bool isRancid = Random.value > 0.8f; 
				
				if (isRancid)
				{
					if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("GROSS! RANCID FOOD!", Color.green);
					if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
					if (timeAttackLogic != null) timeAttackLogic.AddTime(-5f);
				}
				else
				{
					TrashData cookieData = manager.trashLibrary.Find(t => t.type == TrashData.ItemType.FortuneCookie);
					if (cookieData != null) manager.SpawnItemUI(cookieData, true, bagPos, bagPos + (Random.insideUnitCircle * 100f));
				}
				manager.sessionItemsUsed.Add(data.itemSprite);
				return;

			case TrashData.ItemType.FortuneCookie:
				if (itemObject != null) Destroy(itemObject);
				manager.sessionItemsUsed.Add(data.itemSprite);
				
				// Explicitly safe range: 0, 1, 2, 3, or 4
				int soloFortuneRoll = Random.Range(0, 5);
				
				switch (soloFortuneRoll)
				{
					case 0: // GOOD 1: Riches (2 Copper + 20 Seconds)
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("Riches are in your future");
						
						// Always add to the session rewards first!
						manager.currentSessionRewards.copperEarned += 2;

						// Add time bonus in solo time attack mode
						if (timeAttackLogic != null)
						{
							timeAttackLogic.AddTime(20f);
						}

						Debug.Log("<color=yellow>[FORTUNE COOKIE]</color> Riches rolled! Session Copper is now: " + manager.currentSessionRewards.copperEarned);
						
						// Save to profile if logged in
						if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
						{
							PlayFabAuthBridge.Instance.profile.copperCount += 2;
							PlayFabAuthBridge.Instance.SaveProgressToCloud();
						}
						break;

					case 1: // 1. CLEAN SWEEP
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("Your path is clear");
						manager.ClearJunkForFortune();
						manager.PickNewTarget();
						break;

					case 2: // 2. THE SKY IS FALLING (CASCADE)
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("Is the sky falling?");
						int cascadeCount = Random.Range(15, 22);
						for (int i = 0; i < cascadeCount; i++)
						{
							manager.TossInNewTrash();
						}
						break;

					case 3: // 3. ITEM PURGE (REMOVE ITEMS FROM BOARD)
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("A lightened load...");
						manager.PurgeNonTargetTrash(8);
						break;

					case 4: // 4. SCRAMBLE & PENALTY
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("Chaos strikes!");
						
						manager.ScrambleAllTrashPositions();
						
						if (timeAttackLogic != null)
						{
							timeAttackLogic.AddTime(-5f); // Time penalty in solo
						}
						break;
				}
				return;

			case TrashData.ItemType.Vibrator:
				if (itemObject != null) Destroy(itemObject);
				manager.sessionItemsUsed.Add(data.itemSprite);
				
				// UPDATED: Now uses the centralized juice slam!
				if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.PlayVibratorSlam(data.itemSprite);
				return;
        }
		
		switch (data.materialCategory)
        {
            case TrashData.ResourceCategory.Aluminum:
                manager.currentSessionRewards.aluminumEarned += data.resourceValueAmount;
                break;
            case TrashData.ResourceCategory.Glass:
                manager.currentSessionRewards.glassEarned += data.resourceValueAmount;
                break;
            case TrashData.ResourceCategory.Plastic:
                manager.currentSessionRewards.plasticEarned += data.resourceValueAmount;
                break;
            case TrashData.ResourceCategory.Other:
                manager.currentSessionRewards.otherEarned += data.resourceValueAmount;
                break;
            case TrashData.ResourceCategory.Copper:
                manager.currentSessionRewards.copperEarned += data.resourceValueAmount;
                Debug.Log("<color=orange>[TRASH CLICKED]</color> Added Copper! Session Total: " + manager.currentSessionRewards.copperEarned);
                break;
        }
		
		if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
		{
			switch (data.materialCategory)
			{
				case TrashData.ResourceCategory.None:
				// Explicitly do nothing! (Prevents the engine from guessing)
					break;
				case TrashData.ResourceCategory.Aluminum:
					PlayFabAuthBridge.Instance.profile.aluminumCount += data.resourceValueAmount;
					break;
				case TrashData.ResourceCategory.Glass:
					PlayFabAuthBridge.Instance.profile.glassCount += data.resourceValueAmount;
					break;
				case TrashData.ResourceCategory.Plastic:
					PlayFabAuthBridge.Instance.profile.plasticCount += data.resourceValueAmount;
					break;
				case TrashData.ResourceCategory.Other:
					PlayFabAuthBridge.Instance.profile.otherCount += data.resourceValueAmount;
					break;
				case TrashData.ResourceCategory.Copper:
					PlayFabAuthBridge.Instance.profile.copperCount += data.resourceValueAmount;
					manager.currentSessionRewards.copperEarned += data.resourceValueAmount;
					break;
			}
		}
		else
		{
			// Prints a yellow warning instead of crashing your game during testing!
			Debug.LogWarning("PlayFab Profile missing! Resource points not saved. (Did you bypass the login screen?)");
		}
        if (itemName == manager.myTargetItem)
        {
            if (timeAttackLogic != null) timeAttackLogic.OnItemFound();
            if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(true); 
            
            if (DumpsterJuiceFX.Instance != null && itemObject != null) DumpsterJuiceFX.Instance.PlayItemFlyToPlayer(itemObject);
            else if (itemObject != null) Destroy(itemObject);
            
            manager.sessionItemsFound.Add(data.itemSprite);
            manager.TossInNewTrash();
            manager.PickNewTarget();
        }
        else if (data.type == TrashData.ItemType.Junk)
        {
            if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("That's not it!", Color.red);
        }
    }

    public void HandleItemTossed(TrashData data)
    {
        if (timeAttackLogic == null || data == null) return;

        switch (data.type)
        {
            case TrashData.ItemType.Needle:               
                manager.sessionItemsUsed.Add(data.itemSprite);
                break;

            case TrashData.ItemType.Vibrator:
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(false);
                manager.ScrambleAllTrashPositions();
                manager.sessionItemsUsed.Add(data.itemSprite);
                break;

            case TrashData.ItemType.Raccoon:
                Debug.Log("SOLO RACCOON FLICKED: Clean yeet reward!");
                // 🌟 NO POINT IF TOSSED OUT: Points/Feedback loops stripped out cleanly
                manager.sessionItemsUsed.Add(data.itemSprite);
                break;

            default:
                if (data.itemName == manager.myTargetItem)
                {
                    // 🌟 NO POINT IF TOSSED OUT: Points/Feedback loops stripped out cleanly
                    manager.sessionItemsFound.Add(data.itemSprite);
                    manager.PickNewTarget(); 
                }
                break;
        }

        // 🌟 PARABOLIC FIX: Replaced baseline flat spawn allocation with a falling item toss animation
        manager.TossInNewTrash();
    }

    private void HighlightSoloTargets()
    {
        foreach (Transform child in manager.spawnArea)
        {
            if (child.TryGetComponent(out DraggableTrash trash) && trash.data != null)
            {
                if (trash.data.itemName == manager.myTargetItem)
                {
                    var targetImage = child.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (targetImage != null)
                    {
                        StartCoroutine(FlashTargetItem(targetImage));
                    }
                }
            }
        }
    }

    IEnumerator FlashTargetItem(UnityEngine.UI.Image img)
    {
        if (img == null) yield break;
        Color originalColor = img.color;
        
        for (int i = 0; i < 3; i++)
        {
            img.color = new Color(1f, 0.9f, 0.2f, 1f);
            yield return new WaitForSeconds(0.2f);
            img.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
    
	private System.Collections.IEnumerator RunJumpscareAnimation(GameObject raccoonObj, TrashData itemData)
    {
        if (raccoonObj == null) yield break;
        
        raccoonObj.GetComponent<Button>().enabled = false;
        RectTransform rt = raccoonObj.GetComponent<RectTransform>();
        Image img = raccoonObj.GetComponent<Image>();
        
        if (raccoonObj.TryGetComponent(out CanvasGroup cg)) Destroy(cg); 
        
        if (!raccoonObj.TryGetComponent(out Canvas canvas)) canvas = raccoonObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999; 

        if (!raccoonObj.TryGetComponent(out GraphicRaycaster raycaster)) raccoonObj.AddComponent<GraphicRaycaster>();
        
        if (itemData != null && itemData.angryRaccoonSprite != null && img != null)
        {
            img.sprite = itemData.angryRaccoonSprite;
            img.color = new Color(1f, 1f, 1f, 1f);
        }

        Vector3 startScale = rt.localScale;
        Vector3 maxJumpScale = Vector3.one * 6.5f; 
        Vector2 baseAnchoredPos = rt.anchoredPosition;
        Vector2 centerScreenPos = Vector2.zero; 
        
        float elapsed = 0f;
        float rushDuration = 0.25f; 
        
        // --- 1. Jumpscare Shake Attack ---
        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / rushDuration;
            
            rt.localScale = Vector3.Lerp(startScale, maxJumpScale, percent);
            Vector2 currentCenter = Vector2.Lerp(baseAnchoredPos, centerScreenPos, percent);
            float shakeIntensity = 20f * (1f + percent * 2f);
            rt.anchoredPosition = currentCenter + new Vector2(Random.Range(-shakeIntensity, shakeIntensity), Random.Range(-shakeIntensity, shakeIntensity));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 45f) * 15f);
            
            yield return null;
        }

        rt.localScale = maxJumpScale;
        CameraShake shaker = Object.FindObjectOfType<CameraShake>();
        if (shaker != null) shaker.InduceShake(0.5f, 22f);

        // --- 2. Hold Jumpscare in Face ---
        elapsed = 0f;
        float holdDuration = 1.0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = centerScreenPos + new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 50f) * 15f);
            yield return null;
        }

        // --- 3. 🌟 Smooth Slide Down Off-Screen Fix ---
        elapsed = 0f;
        float slideDuration = 0.5f;
        Vector2 slideStart = rt.anchoredPosition;

        // FIXED: Accessing areaRect via 'manager.areaRect' to clear out the solo compiler block
        float canvasBottomCeiling = -(manager.areaRect.rect.height / 2f) - 300f; 
        Vector2 slideDestination = new Vector2(rt.anchoredPosition.x, canvasBottomCeiling); 
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / slideDuration;
            
            // Accelerating mathematical drop curve down out of view
            float t = percent * percent; 
            
            rt.anchoredPosition = Vector2.Lerp(slideStart, slideDestination, t);
            yield return null;
        }
        
        Destroy(raccoonObj);
        manager.TossInNewTrash();
    }
    
	public void TriggerSoloTimeUp(GameObject timeUpPanel, int finalScore)
		{
			if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.ForceClearFortuneUI();

			PlayerProfileData cloudProfile = PlayFabAuthBridge.Instance.profile;
			MatchSessionRewards session = manager.currentSessionRewards;

			cloudProfile.aluminumCount += session.aluminumEarned;
			cloudProfile.glassCount += session.glassEarned;
			cloudProfile.plasticCount += session.plasticEarned;
			cloudProfile.otherCount += session.otherEarned;
			cloudProfile.copperCount += session.copperEarned;
			

			PlayFabAuthBridge.Instance.SaveProgressToCloud();

			if (timeUpPanel != null)
			{
				timeUpPanel.SetActive(true);
				EndGameTally tally = timeUpPanel.GetComponentInChildren<EndGameTally>(true);
				GameFeed.Instance.LogMessage("TIME'S UP!", Color.yellow);
				if (tally != null)
				{
					float timeTaken = Time.time - manager.matchStartTime;
					tally.BeginTally(timeTaken, finalScore, 0, manager.sessionItemsFound, manager.sessionItemsUsed);
				}
			}
		}
}