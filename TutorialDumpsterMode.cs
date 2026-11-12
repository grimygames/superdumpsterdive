using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TutorialDumpsterMode : MonoBehaviour, IDumpsterMode
{
    private DumpsterManager manager;

    public enum TutorialStep
    {
        Welcome,
        FindTarget,
        SwipeJunk,
        OpenBag,
        AvoidHazard,
        CollectRecyclable,
        UseItem,
        Complete
    }

    [System.Serializable]
    public class StepConfig
    {
        public TutorialStep step;
        [TextArea(3, 5)]
        public string dialogue;
        public string scroungyAnimTrigger = "Talk";
    }

    [Header("UI Elements")]
    public GameObject instructionPanel;
    public TMP_Text dialogueText;
    public Button nextButton;
    public RectTransform tutorialShakeTarget;

    [Header("Scroungy Mascot")]
    public Animator scroungyAnimator;

    [Header("Tutorial Text Setup")]
    public List<StepConfig> steps = new List<StepConfig>();

    [Header("Specific Test Items")]
    public TrashData tutorialTargetItem;
    public TrashData tutorialJunkItem;
    public TrashData tutorialBagItem;
    public TrashData tutorialNeedleItem;
    public TrashData tutorialCopperItem;
    public TrashData tutorialRaccoonItem;

    [Header("Current State")]
    public TutorialStep currentStep = TutorialStep.Welcome;

    private Vector2 originalShakePos;
    private bool isShaking = false;

    void Awake()
	{
		// Add this failsafe at the very top
		if (!Launcher.IsTutorial)
		{
			Destroy(this);
			return;
		}

		manager = GetComponent<DumpsterManager>();
		BindUI();
	}

	void Start()
    {
        if (manager != null) manager.matchStartTime = Time.time;
        if (instructionPanel != null) instructionPanel.SetActive(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (tutorialShakeTarget != null)
        {
            originalShakePos = tutorialShakeTarget.anchoredPosition;
        }

        SetStep(TutorialStep.Welcome);
    }

    private void BindUI()
    {
        if (instructionPanel == null) instructionPanel = GameObject.Find("TutorialPanel");

        if (instructionPanel != null)
        {
            Canvas uiCanvas = instructionPanel.GetComponent<Canvas>();
            if (uiCanvas == null) uiCanvas = instructionPanel.AddComponent<Canvas>();
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 100; 
            
            if (instructionPanel.GetComponent<GraphicRaycaster>() == null)
            {
                instructionPanel.AddComponent<GraphicRaycaster>();
            }

            if (dialogueText == null) dialogueText = instructionPanel.GetComponentInChildren<TMP_Text>(true);
            if (nextButton == null) nextButton = instructionPanel.GetComponentInChildren<Button>(true);
        }
    }

    public void SetStep(TutorialStep newStep)
    {
        currentStep = newStep;
        ClearDumpster();

        StepConfig config = steps.Find(s => s.step == currentStep);

        if (dialogueText != null)
        {
            dialogueText.text = (config != null && !string.IsNullOrEmpty(config.dialogue)) ? config.dialogue : GetDefaultDialogue(currentStep);
        }

        if (scroungyAnimator != null && config != null && !string.IsNullOrEmpty(config.scroungyAnimTrigger))
        {
            scroungyAnimator.SetTrigger(config.scroungyAnimTrigger);
        }

        SetNextButton(true, currentStep == TutorialStep.Complete ? "EXIT ALLEYWAY" : "NEXT");
        
        switch (currentStep)
        {
            case TutorialStep.Welcome:
            case TutorialStep.FindTarget:
                UpdateTopTargetIcon(GetFallbackTarget());
                TutorialTossIn(GetFallbackTarget());
                break;
            case TutorialStep.SwipeJunk:
                UpdateTopTargetIcon(GetFallbackTarget());
                TutorialTossIn(GetFallbackJunk());
                break;
            case TutorialStep.OpenBag:
                UpdateTopTargetIcon(GetFallbackTarget());
                TutorialTossIn(GetFallbackBag());
                break;
            case TutorialStep.AvoidHazard:
                UpdateTopTargetIcon(GetFallbackTarget());
                TutorialTossIn(GetFallbackNeedle());
                TutorialTossIn(GetFallbackJunk()); 
                TutorialTossIn(GetFallbackJunk());
                break;
            case TutorialStep.CollectRecyclable:
                UpdateTopTargetIcon(GetFallbackCopper());
                TutorialTossIn(GetFallbackCopper());
                break;
            case TutorialStep.UseItem:
                UpdateTopTargetIcon(GetFallbackTarget());
                TutorialTossIn(GetFallbackRaccoon());
                TutorialTossIn(GetFallbackJunk()); 
                TutorialTossIn(GetFallbackJunk());
                break;
            case TutorialStep.Complete:
                // Dumpster remains clean and empty.
                break;
        }
    }

    private void SetNextButton(bool isVisible, string buttonText)
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(isVisible);
            TMP_Text btnTextLabel = nextButton.GetComponentInChildren<TMP_Text>();
            if (btnTextLabel != null) btnTextLabel.text = buttonText;
        }
    }

    private void OnNextClicked()
    {
        if (currentStep == TutorialStep.Welcome) SetStep(TutorialStep.FindTarget);
        else if (currentStep == TutorialStep.FindTarget) SetStep(TutorialStep.SwipeJunk);
        else if (currentStep == TutorialStep.SwipeJunk) SetStep(TutorialStep.OpenBag);
        else if (currentStep == TutorialStep.OpenBag) SetStep(TutorialStep.AvoidHazard);
        else if (currentStep == TutorialStep.AvoidHazard) SetStep(TutorialStep.CollectRecyclable);
        else if (currentStep == TutorialStep.CollectRecyclable) SetStep(TutorialStep.UseItem);
        else if (currentStep == TutorialStep.UseItem) SetStep(TutorialStep.Complete);
        else if (currentStep == TutorialStep.Complete) SceneManager.LoadScene("Lobby");
    }

    private void UpdateTopTargetIcon(TrashData itemData)
    {
        if (itemData != null)
        {
            manager.myTargetItem = itemData.itemName;
            GameSync gs = Object.FindObjectOfType<GameSync>();
            if (gs != null) gs.UpdateLocalTargetUI(itemData);
        }
    }

    private void ClearDumpster()
    {
        if (manager != null && manager.spawnArea != null)
        {
            foreach (Transform child in manager.spawnArea) Destroy(child.gameObject);
        }
    }

    private void TutorialTossIn(TrashData itemData)
    {
        if (itemData == null || manager == null) return;
        float w = manager.areaRect.rect.width / 2f;
        float h = manager.areaRect.rect.height / 2f;

        Vector2 spawnCeilingOrigin = new Vector2(0f, h + 150f);
        Vector2 topHalfLandingTarget = new Vector2(Random.Range(-w * 0.5f, w * 0.5f), Random.Range(50f, h * 0.5f));

        manager.SpawnItemUI(itemData, true, spawnCeilingOrigin, topHalfLandingTarget);
    }

    // --- Standalone Shake & Jumpscare Logic ---

    public void TriggerLocalShake()
    {
        if (tutorialShakeTarget != null && !isShaking) StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;
        float duration = 0.35f;
        float magnitude = 75f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1.0f - (elapsed / duration); 
            tutorialShakeTarget.anchoredPosition = new Vector2(originalShakePos.x + Random.Range(-1f, 1f) * magnitude * damper, originalShakePos.y + Random.Range(-1f, 1f) * magnitude * damper);
            yield return null;
        }

        tutorialShakeTarget.anchoredPosition = originalShakePos;
        isShaking = false;
    }

    private IEnumerator RunJumpscareAnimation(GameObject raccoonObj, TrashData itemData)
    {
        if (raccoonObj == null) yield break;
        if (raccoonObj.TryGetComponent(out Button btn)) btn.enabled = false;
        
        RectTransform rt = raccoonObj.GetComponent<RectTransform>();
        Image img = raccoonObj.GetComponentInChildren<Image>();
        
        if (raccoonObj.TryGetComponent(out CanvasGroup cg)) Destroy(cg); 
        if (!raccoonObj.TryGetComponent(out Canvas canvas)) canvas = raccoonObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999; 

        if (itemData != null && itemData.angryRaccoonSprite != null && img != null) img.sprite = itemData.angryRaccoonSprite;

        Vector3 startScale = rt.localScale;
        Vector3 maxJumpScale = Vector3.one * 6.5f; 
        Vector2 baseAnchoredPos = rt.anchoredPosition;
        
        float elapsed = 0f;
        float rushDuration = 0.25f; 
        
        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / rushDuration;
            rt.localScale = Vector3.Lerp(startScale, maxJumpScale, percent);
            float shakeIntensity = 20f * (1f + percent * 2f);
            rt.anchoredPosition = Vector2.Lerp(baseAnchoredPos, Vector2.zero, percent) + new Vector2(Random.Range(-shakeIntensity, shakeIntensity), Random.Range(-shakeIntensity, shakeIntensity));
            yield return null;
        }

        rt.localScale = maxJumpScale;
        TriggerLocalShake();

        yield return new WaitForSeconds(1.0f);

        float slideDuration = 0.5f;
        elapsed = 0f;
        Vector2 slideStart = rt.anchoredPosition;
        float canvasBottomCeiling = -(manager.areaRect.rect.height / 2f) - 300f; 
        Vector2 slideDestination = new Vector2(rt.anchoredPosition.x, canvasBottomCeiling); 
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(slideStart, slideDestination, (elapsed / slideDuration) * (elapsed / slideDuration));
            yield return null;
        }
        
        Destroy(raccoonObj);
        TutorialTossIn(GetFallbackRaccoon()); // Respawn so they can try to swipe it properly
    }

    // --- IDumpsterMode Interface Implementation ---

    public void HandleItemClicked(string itemName, GameObject itemObject, TrashData data)
    {
        if (data == null || currentStep == TutorialStep.Complete) return;

        Vector2 clickPos = (itemObject != null) ? itemObject.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        // HAZARD CLICK: Punish & Respawn Hazard
        if (data.type == TrashData.ItemType.Needle || (data.itemName != null && data.itemName.ToLower().Contains("needle")))
        {
            if (itemObject != null) Destroy(itemObject);
            TriggerLocalShake();
            
            if (DumpsterJuiceFX.Instance != null) DumpsterJuiceFX.Instance.TriggerScreenShake();
            CameraShake shaker = Object.FindObjectOfType<CameraShake>();
            if (shaker != null) shaker.InduceShake(0.5f, 22f);
            
            if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("OUCH! Swipe hazards away!", Color.red);
            TutorialTossIn(GetFallbackNeedle());
            return; 
        }

        // RACCOON CLICK: Jumpscare & Respawn Raccoon
        if (data.type == TrashData.ItemType.Raccoon)
        {
            StartCoroutine(RunJumpscareAnimation(itemObject, data));
            if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("YIKES! Swipe animals away!", Color.red);
            return;
        }

        // BAG CLICK: Pop and spawn the Find Item
        if (data.type == TrashData.ItemType.Bag)
        {
            if (itemObject != null) Destroy(itemObject);
            
            TrashData targetToSpill = GetFallbackTarget();
            for (int i = 0; i < 3; i++)
            {
                Vector2 offsetTarget = clickPos + (Random.insideUnitCircle * 150f);
                manager.SpawnItemUI(targetToSpill, true, clickPos, offsetTarget);
            }
            return;
        }

        // STANDARD BEHAVIORS
        if (itemObject != null) Destroy(itemObject);

        switch (currentStep)
        {
            case TutorialStep.Welcome:
            case TutorialStep.FindTarget:
                TutorialTossIn(GetFallbackTarget());
                break;
            case TutorialStep.SwipeJunk:
                TutorialTossIn(GetFallbackJunk()); 
                break;
            case TutorialStep.OpenBag:
                if (manager.spawnArea.GetComponentsInChildren<DraggableTrash>().Length <= 1) 
                {
                    TutorialTossIn(GetFallbackBag());
                }
                break;
            case TutorialStep.AvoidHazard:
                TutorialTossIn(GetFallbackJunk());
                break;
            case TutorialStep.CollectRecyclable:
                manager.currentSessionRewards.copperEarned += data.resourceValueAmount;
                TutorialTossIn(GetFallbackCopper());
                break;
            case TutorialStep.UseItem:
                TutorialTossIn(GetFallbackJunk());
                break;
        }
    }

    public void HandleItemTossed(TrashData data)
    {
        if (data == null || currentStep == TutorialStep.Complete) return;

        switch (currentStep)
        {
            case TutorialStep.Welcome:
            case TutorialStep.FindTarget:
                TutorialTossIn(GetFallbackTarget()); 
                break;
                
            case TutorialStep.SwipeJunk:
                if (data.type == TrashData.ItemType.Junk) TutorialTossIn(GetFallbackTarget());
                else TutorialTossIn(GetFallbackTarget());
                break;

            case TutorialStep.OpenBag:
                TutorialTossIn(GetFallbackTarget());
                break;

            case TutorialStep.AvoidHazard:
                // Swipe the needle correctly -> Needle comes back to keep practicing
                if (data.type == TrashData.ItemType.Needle || (data.itemName != null && data.itemName.ToLower().Contains("needle"))) 
                {
                    TutorialTossIn(GetFallbackNeedle());
                }
                else 
                {
                    TutorialTossIn(GetFallbackTarget());
                }
                break;

            case TutorialStep.CollectRecyclable:
                TutorialTossIn(GetFallbackCopper());
                break;
                
            case TutorialStep.UseItem:
                // Swipe the raccoon correctly -> Raccoon comes back to keep practicing
                if (data.type == TrashData.ItemType.Raccoon) 
                {
                    TutorialTossIn(GetFallbackRaccoon());
                }
                else 
                {
                    TutorialTossIn(GetFallbackTarget());
                }
                break;
        }
    }

    // --- Fallback Data Helpers ---
    private TrashData GetFallbackTarget() => tutorialTargetItem != null ? tutorialTargetItem : manager.trashLibrary.Find(t => t.classification == TrashData.GameItemClassification.StandardTarget);
    private TrashData GetFallbackJunk() => tutorialJunkItem != null ? tutorialJunkItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Junk);
    private TrashData GetFallbackBag() => tutorialBagItem != null ? tutorialBagItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Bag);
    private TrashData GetFallbackNeedle() => tutorialNeedleItem != null ? tutorialNeedleItem : manager.trashLibrary.Find(t => t.itemName.ToLower().Contains("needle"));
    private TrashData GetFallbackCopper() => tutorialCopperItem != null ? tutorialCopperItem : manager.trashLibrary.Find(t => t.materialCategory == TrashData.ResourceCategory.Copper);
    private TrashData GetFallbackRaccoon() => tutorialRaccoonItem != null ? tutorialRaccoonItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Raccoon);

    private string GetDefaultDialogue(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Welcome: return "Yo, Im Scroungy! Let's learn to dumpster dive!";
            case TutorialStep.FindTarget: return "Tap the exact item your thinking of, shown at the top of your screen!";
            case TutorialStep.SwipeJunk: return "Swipe bullshit junk out of the dumpster to dig deeper for your item.";
            case TutorialStep.OpenBag: return "Tap bags to rip them open. there is stuff inside!";
            case TutorialStep.AvoidHazard: return "Watch out for needles and broken glass. That shit hurts";
            case TutorialStep.CollectRecyclable: return "Recycling that you find like copper can be traded in for cash!";
            case TutorialStep.UseItem: return " Animals can fuck you up, be careful!";
            case TutorialStep.Complete: return "Don't forget to trade in your recycling for cash to spend at the thrift store. Now fuck off!";
            default: return "";
        }
    }
}