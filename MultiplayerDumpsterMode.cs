using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class MultiplayerDumpsterMode : MonoBehaviour, IDumpsterMode
{
    private DumpsterManager manager;
    private GameSync gameSync;

    [Header("Match Timer Settings")]
    private TMP_Text timerDisplay;      
    public float matchDuration = 60f;   // Defaults to 60 seconds
    
    private bool matchHasEnded = false;
    private const string END_TIME_KEY = "MatchEndTime";

    void Awake()
    {
        manager = GetComponent<DumpsterManager>();
    }

    void Start()
    {
        gameSync = Object.FindObjectOfType<GameSync>();
        manager.matchStartTime = Time.time;

        // Auto-link UI timer text
        GameObject timerObj = GameObject.Find("MatchTimerText");
        if (timerObj != null) timerDisplay = timerObj.GetComponent<TMP_Text>();

        float actualDuration = matchDuration; 
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MatchDuration", out object customDurationObj))
        {
            actualDuration = System.Convert.ToSingle(customDurationObj);
        }
        matchDuration = actualDuration; 

        if (PhotonNetwork.IsMasterClient)
        {
            double endTime = PhotonNetwork.Time + actualDuration;
            ExitGames.Client.Photon.Hashtable timeProps = new ExitGames.Client.Photon.Hashtable { { END_TIME_KEY, endTime } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(timeProps);
        }
    }

    void Update()
    {
        if (matchHasEnded || !PhotonNetwork.InRoom) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(END_TIME_KEY, out object endTimeObj))
        {
            double endTime = (double)endTimeObj;
            double timeRemaining = endTime - PhotonNetwork.Time;

            if (timeRemaining > 0) UpdateTimerUI(timeRemaining);
            else TriggerMatchEnd();
        }
    }

    private void UpdateTimerUI(double timeRemaining)
    {
        if (timerDisplay != null)
        {
            int seconds = Mathf.CeilToInt((float)timeRemaining);
            timerDisplay.text = seconds.ToString();
            if (seconds <= 10) timerDisplay.color = (seconds % 2 == 0) ? Color.red : Color.white;
        }
    }

	private void TriggerMatchEnd()
	{
		if (matchHasEnded) return;
		matchHasEnded = true;

		if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.ForceClearFortuneUI();

		// 🌟 NULL-SAFE PLAYFAB SAVE (Prevents WebGL Silent Crash)
		if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
		{
			PlayerProfileData cloudProfile = PlayFabAuthBridge.Instance.profile;
			MatchSessionRewards session = manager.currentSessionRewards;

			cloudProfile.aluminumCount += session.aluminumEarned;
			cloudProfile.glassCount += session.glassEarned;
			cloudProfile.plasticCount += session.plasticEarned;
			cloudProfile.otherCount += session.otherEarned;
			cloudProfile.copperCount += session.copperEarned;

			PlayFabAuthBridge.Instance.SaveProgressToCloud();
		}
		else
		{
			Debug.LogWarning("[WebGL Safety] PlayFab Profile was not ready at match end. Skipping cloud push.");
		}

		if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("TIME'S UP!", Color.yellow);

		// 🌟 TRIGGER END SCREEN VIA MASTER CLIENT
		if (PhotonNetwork.IsMasterClient && gameSync != null)
		{
			gameSync.photonView.RPC("RPC_DetermineWinner", RpcTarget.All);
		}
	}

    // --- ITEM INTERACTIONS ---

    public void HandleItemClicked(string itemName, GameObject itemObject, TrashData data)
    {
        if (matchHasEnded || data == null) return; 

        if (gameSync != null && gameSync.diaperScrubScript != null)
        {
            if (gameSync.diaperScrubScript.IsScreenTooDirty()) return;
        }

        Vector2 bagPos = (itemObject != null) ? itemObject.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        switch (data.type)
        {
            case TrashData.ItemType.Needle:
                if (gameSync != null) { gameSync.ItemMissed(); gameSync.ItemMissed(); gameSync.ItemMissed(); }
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(false);
                if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("OUCH! NEEDLE POKED!", Color.red);
                if (gameSync != null) gameSync.SendFeedEvent("Opponent hit a needle!");
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;

            case TrashData.ItemType.Diaper:
                if (gameSync != null && gameSync.diaperScrubScript != null) gameSync.diaperScrubScript.SmearScreen(); 
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;

            case TrashData.ItemType.Bag:
                int spawnCount = Random.Range(4, 7);
                for (int i = 0; i < spawnCount; i++)
                {
                    TrashData spilledData = manager.GetRandomTrashData();
                    // 🌟 NESTED LOOP SHIELD: Blocks bags from populating inside other bags
                    while (spilledData.type == TrashData.ItemType.Bag)
                    {
                        spilledData = manager.GetRandomTrashData();
                    }
                    Vector2 offsetTarget = bagPos + (Random.insideUnitCircle * 150f);
                    manager.SpawnItemUI(spilledData, true, bagPos, offsetTarget);
                }
                if (gameSync != null) gameSync.ItemFound();
                if (itemObject != null) Destroy(itemObject);
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;
                
            case TrashData.ItemType.WalkieTalkie:
                if (gameSync != null) gameSync.RevealOpponentTarget(7f);
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;

            case TrashData.ItemType.Vibrator:
                if (gameSync != null) gameSync.LaunchVibratorSabotage();
                if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
                if (itemObject != null) Destroy(itemObject);
                manager.TossInNewTrash();
                manager.sessionItemsUsed.Add(data.itemSprite);
                return;
            
            case TrashData.ItemType.Raccoon:
                Debug.Log("MULTIPLAYER RACCOON CLICKED: Unleashing local punishment!");
                StartCoroutine(RunLocalMultiplayerJumpscare(raccoonObj: itemObject, itemData: data));
                if (gameSync != null) 
                {
                    gameSync.ItemMissed(); 
                    if (PhotonNetwork.InRoom)
                    {
                        gameSync.photonView.RPC("RPC_ApplyRaccoonAttack", RpcTarget.Others);
                    }
                }
                return;
			
			case TrashData.ItemType.TakeoutBox:
				if (itemObject != null) Destroy(itemObject);
				bool isMultiRancid = Random.value > 0.8f; 
				
				if (isMultiRancid)
				{
					if (gameSync != null && gameSync.diaperScrubScript != null) gameSync.diaperScrubScript.SmearScreen(); 
					if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
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
				int multiFortuneRoll = Random.Range(0, 5);
				
				switch (multiFortuneRoll)
				{
					case 0: // GOOD 1: Riches (2 Copper)
						if (DumpsterJuiceFX.Instance != null) 
							DumpsterJuiceFX.Instance.PlayFortuneReveal("Riches are in your future");
						
						// Always add to the session rewards first!
						manager.currentSessionRewards.copperEarned += 2;
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
						
						if (gameSync != null)
						{
							gameSync.ItemMissed(); // Score penalty in multiplayer
						}
						break;
				}
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
					break;

				case TrashData.ResourceCategory.Aluminum:
					PlayFabAuthBridge.Instance.profile.aluminumCount += data.resourceValueAmount;
					manager.currentSessionRewards.aluminumEarned += data.resourceValueAmount;
					break;

				case TrashData.ResourceCategory.Glass:
					PlayFabAuthBridge.Instance.profile.glassCount += data.resourceValueAmount;
					manager.currentSessionRewards.glassEarned += data.resourceValueAmount;
					break;

				case TrashData.ResourceCategory.Plastic:
					PlayFabAuthBridge.Instance.profile.plasticCount += data.resourceValueAmount;
					manager.currentSessionRewards.plasticEarned += data.resourceValueAmount;
					break;

				case TrashData.ResourceCategory.Other:
					PlayFabAuthBridge.Instance.profile.otherCount += data.resourceValueAmount;
					manager.currentSessionRewards.otherEarned += data.resourceValueAmount;
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
            if (gameSync != null) gameSync.ItemFound();
            if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(true);
            if (DumpsterJuiceFX.Instance != null && itemObject != null) DumpsterJuiceFX.Instance.PlayItemFlyToPlayer(itemObject);
            else if (itemObject != null) Destroy(itemObject);

            manager.sessionItemsFound.Add(data.itemSprite);
            manager.TossInNewTrash();
            manager.PickNewTarget();
        }
        else if (data.type == TrashData.ItemType.Junk)
        {
            if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("That's not it!!", Color.white);
        }
    }

    public void HandleItemTossed(TrashData data)
    {
        if (matchHasEnded || data == null) return; 

        switch (data.type)
        {
            case TrashData.ItemType.Needle:
                manager.sessionItemsUsed.Add(data.itemSprite);
                manager.TossInNewTrash();
                break;

            case TrashData.ItemType.Diaper:
                if (gameSync != null) gameSync.ReceiveDiaper();
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(true);
                manager.sessionItemsUsed.Add(data.itemSprite);  
                manager.TossInNewTrash();             
                break;

            case TrashData.ItemType.Junk:
                if (gameSync != null) gameSync.DropJunkOnOpponent(data);
                manager.sessionItemsUsed.Add(data.itemSprite);
                manager.TossInNewTrash();
                break;

            case TrashData.ItemType.Vibrator:
                if (gameSync != null) gameSync.LaunchVibratorSabotage();
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(false);
                manager.sessionItemsUsed.Add(data.itemSprite);
                manager.TossInNewTrash();
                break;

            case TrashData.ItemType.Raccoon:
                if (gameSync != null && PhotonNetwork.InRoom) 
                {
                    gameSync.photonView.RPC("RPC_ApplyRaccoonAttack", RpcTarget.Others);
                }
                if (TugOfWarJuicer.Instance != null) TugOfWarJuicer.Instance.TriggerScoreFeedback(true);
                manager.sessionItemsUsed.Add(data.itemSprite);
                manager.TossInNewTrash();
                break;

            default:
                if (data.itemName == manager.myTargetItem)
                {
                    // 🌟 NO POINT IF TOSSED OUT: Points/Feedback loops stripped out cleanly
                    manager.sessionItemsFound.Add(data.itemSprite);
                    manager.TossInNewTrash();
                    manager.PickNewTarget(); 
                }
                else
                {
                    if (gameSync != null) gameSync.ThrowRegularTrash(data);
                    manager.TossInNewTrash();
                }
                break;
        }
    }
    
    public void ExecuteNetworkRaccoonAttackStrike()
    {
        TrashData raccoonData = manager.trashLibrary.Find(item => item.type == TrashData.ItemType.Raccoon);
        if (raccoonData == null) return;

        GameObject customRaccoonVisualObj = Instantiate(manager.itemPrefab, manager.spawnArea);
        customRaccoonVisualObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        StartCoroutine(RunLocalMultiplayerJumpscare(customRaccoonVisualObj, raccoonData));
    }
    
	private System.Collections.IEnumerator RunLocalMultiplayerJumpscare(GameObject raccoonObj, TrashData itemData)
    {
        if (raccoonObj == null) yield break;
        
        if (raccoonObj.TryGetComponent(out Button btn)) btn.enabled = false;
        RectTransform rt = raccoonObj.GetComponent<RectTransform>();
        Image img = raccoonObj.GetComponentInChildren<Image>();
        
        if (raccoonObj.TryGetComponent(out CanvasGroup cg)) Destroy(cg); 
        
        if (!raccoonObj.TryGetComponent(out Canvas canvas)) canvas = raccoonObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999; 

        if (!raccoonObj.TryGetComponent(out GraphicRaycaster raycaster)) raccoonObj.AddComponent<GraphicRaycaster>();
        
        if (itemData != null && itemData.angryRaccoonSprite != null && img != null)
        {
            img.sprite = itemData.angryRaccoonSprite;
            img.color = Color.white;
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

        // FIXED: Replaced 'areaRect' with 'manager.areaRect' to clear out the compiler error
        float canvasBottomCeiling = -(manager.areaRect.rect.height / 2f) - 300f; 
        Vector2 slideDestination = new Vector2(rt.anchoredPosition.x, canvasBottomCeiling); 
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / slideDuration;
            
            // This creates an accelerating drop curve down to the floor
            float t = percent * percent; 
            
            rt.anchoredPosition = Vector2.Lerp(slideStart, slideDestination, t);
            yield return null;
        }
        
        // Wipe the clone completely out of memory and loop a replacement piece
        Destroy(raccoonObj);
        manager.TossInNewTrash();
    }
        
    public void SyncNewTargetToNetwork(TrashData targetData)
    {
        if (PhotonNetwork.InRoom)
        {
            int index = manager.trashLibrary.IndexOf(targetData);
            var networkProps = new ExitGames.Client.Photon.Hashtable { { "TargetIndex", index } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(networkProps);
        }
    }
}