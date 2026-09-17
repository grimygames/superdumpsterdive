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
        FlickJunk,
        OpenBag,
        AvoidHazard,
        CollectRecyclable,
        Complete
    }

    [System.Serializable]
    public class StepConfig
    {
        public TutorialStep step;
        [TextArea(3, 5)]
        public string dialogue;
        [Tooltip("Trigger name for Scroungy's Animator (e.g., 'Talk', 'Point')")]
        public string scroungyAnimTrigger = "Talk";
    }

    [Header("UI Elements")]
    public GameObject instructionPanel;
    public TMP_Text dialogueText;
    public Button nextButton;

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

    [Header("Spawn Settings")]
    [Tooltip("Adjust X and Y to move where tutorial items spawn on the Canvas")]
    public Vector2 centerSpawnPosition = new Vector2(0f, 300f);

    [Header("Current State")]
    public TutorialStep currentStep = TutorialStep.Welcome;

    private int junkFlickCount = 0;
    private const int JUNK_FLICK_GOAL = 2;

    void Awake()
    {
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

        SetStep(TutorialStep.Welcome);
    }

    private void BindUI()
    {
        if (instructionPanel == null)
            instructionPanel = GameObject.Find("TutorialPanel");

        if (instructionPanel != null)
        {
            if (dialogueText == null)
                dialogueText = instructionPanel.GetComponentInChildren<TMP_Text>(true);

            if (nextButton == null)
                nextButton = instructionPanel.GetComponentInChildren<Button>(true);
        }
    }

    public void SetStep(TutorialStep newStep)
    {
        currentStep = newStep;

        StepConfig config = steps.Find(s => s.step == currentStep);

        // Update Text
        if (dialogueText != null)
        {
            dialogueText.text = (config != null && !string.IsNullOrEmpty(config.dialogue)) 
                ? config.dialogue 
                : GetDefaultDialogue(currentStep);
        }

        // Trigger Scroungy Animation
        if (scroungyAnimator != null && config != null && !string.IsNullOrEmpty(config.scroungyAnimTrigger))
        {
            scroungyAnimator.SetTrigger(config.scroungyAnimTrigger);
        }

        // Handle Step Logic & Button Visibility
        switch (currentStep)
        {
            case TutorialStep.Welcome:
                SetNextButton(true, "NEXT");
                break;

            case TutorialStep.FindTarget:
                SetNextButton(false, "NEXT");
                SpawnTarget();
                break;

            case TutorialStep.FlickJunk:
                junkFlickCount = 0;
                SetNextButton(false, "NEXT");
                SpawnJunk();
                break;

            case TutorialStep.OpenBag:
                SetNextButton(false, "NEXT");
                SpawnBag();
                break;

            case TutorialStep.AvoidHazard:
                SetNextButton(true, "NEXT");
                SpawnHazard();
                break;

            case TutorialStep.CollectRecyclable:
                SetNextButton(false, "NEXT");
                SpawnScrap();
                break;

            case TutorialStep.Complete:
                SetNextButton(true, "EXIT");
                break;
        }
    }

    private void SetNextButton(bool isVisible, string buttonText)
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(isVisible);
            
            TMP_Text btnTextLabel = nextButton.GetComponentInChildren<TMP_Text>();
            if (btnTextLabel != null)
            {
                btnTextLabel.text = buttonText;
            }
        }
    }

    private void OnNextClicked()
    {
        if (currentStep == TutorialStep.Welcome) SetStep(TutorialStep.FindTarget);
        else if (currentStep == TutorialStep.AvoidHazard) SetStep(TutorialStep.CollectRecyclable);
        else if (currentStep == TutorialStep.Complete) SceneManager.LoadScene("Lobby");
    }

    // --- Target Icon Helper ---
    // Forces the UI at the top of the screen to change to whatever the current objective is
    private void UpdateTopTargetIcon(TrashData itemData)
    {
        if (itemData != null)
        {
            manager.myTargetItem = itemData.itemName;
            GameSync gs = Object.FindObjectOfType<GameSync>();
            if (gs != null) gs.UpdateLocalTargetUI(itemData);
        }
    }

    // --- IDumpsterMode Interface Implementation ---

    public void HandleItemClicked(string itemName, GameObject itemObject, TrashData data)
    {
        if (data == null) return;
        Vector2 clickPos = (itemObject != null) ? itemObject.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        // Catch Hazard clicks globally during the tutorial
        if (data == tutorialNeedleItem || (data.itemName != null && data.itemName.ToLower().Contains("needle")))
        {
            if (itemObject != null) Destroy(itemObject);
            
            if (GameFeed.Instance != null) 
                GameFeed.Instance.LogMessage("OUCH! Watch out for hazards!", Color.red);

            return; // Stop further logic so we don't accidentally progress a step
        }

        switch (currentStep)
        {
            case TutorialStep.FindTarget:
                if (itemName == manager.myTargetItem)
                {
                    if (itemObject != null) Destroy(itemObject);
                    SetStep(TutorialStep.FlickJunk);
                }
                break;

            case TutorialStep.OpenBag:
                // Check if they clicked the exact tutorial bag OR a generic bag
                if (data == tutorialBagItem || data.type == TrashData.ItemType.Bag)
                {
                    if (itemObject != null) Destroy(itemObject);
                    
                    for (int i = 0; i < 3; i++)
                    {
                        TrashData drop = manager.GetRandomTrashData();
                        int loopSafety = 0;
                        
                        while (drop != null && drop.type == TrashData.ItemType.Bag && loopSafety < 10)
                        {
                            drop = manager.GetRandomTrashData();
                            loopSafety++;
                        }
                        
                        if (drop != null)
                        {
                            manager.SpawnItemUI(drop, true, clickPos, clickPos + (Random.insideUnitCircle * 120f));
                        }
                    }
                    SetStep(TutorialStep.AvoidHazard);
                }
                break;

            case TutorialStep.CollectRecyclable:
                // Check if they clicked the exact tutorial copper OR generic copper
                if (data == tutorialCopperItem || data.materialCategory == TrashData.ResourceCategory.Copper)
                {
                    manager.currentSessionRewards.copperEarned += data.resourceValueAmount;
                    if (itemObject != null) Destroy(itemObject);
                    SetStep(TutorialStep.Complete);
                }
                break;
        }
    }

    public void HandleItemTossed(TrashData data)
    {
        if (data == null) return;

        // SPAWN NEW TRASH WHEN FLICKED: This replaces the item they just threw away
        TrashData replacementJunk = tutorialJunkItem != null ? tutorialJunkItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Junk);
        if (replacementJunk != null)
        {
            manager.SpawnItemUI(replacementJunk, true, centerSpawnPosition);
        }

        if (currentStep == TutorialStep.FlickJunk)
        {
            junkFlickCount++;
            if (junkFlickCount >= JUNK_FLICK_GOAL)
            {
                SetStep(TutorialStep.OpenBag);
            }
        }
    }

    // --- Spawners ---

    private void SpawnTarget()
    {
        TrashData target = tutorialTargetItem != null ? tutorialTargetItem : manager.trashLibrary.Find(t => t.classification == TrashData.GameItemClassification.StandardTarget);
        if (target != null)
        {
            UpdateTopTargetIcon(target);
            manager.SpawnItemUI(target, false, centerSpawnPosition);
            StartCoroutine(FlashSpawnedItems(target.itemName, TutorialStep.FindTarget));
        }
    }

    private void SpawnJunk()
    {
        TrashData junk = tutorialJunkItem != null ? tutorialJunkItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Junk);
        if (junk != null)
        {
            UpdateTopTargetIcon(junk);
            manager.SpawnItemUI(junk, false, centerSpawnPosition + new Vector2(-130f, 0));
            manager.SpawnItemUI(junk, false, centerSpawnPosition + new Vector2(130f, 0));
            
            StartCoroutine(FlashSpawnedItems(junk.itemName, TutorialStep.FlickJunk));
        }
    }

    private void SpawnBag()
    {
        TrashData bag = tutorialBagItem != null ? tutorialBagItem : manager.trashLibrary.Find(t => t.type == TrashData.ItemType.Bag);
        if (bag != null)
        {
            UpdateTopTargetIcon(bag);
            manager.SpawnItemUI(bag, false, centerSpawnPosition);
            StartCoroutine(FlashSpawnedItems(bag.itemName, TutorialStep.OpenBag));
        }
    }

    private void SpawnHazard()
    {
        TrashData hazard = tutorialNeedleItem;
        if (hazard == null) hazard = manager.trashLibrary.Find(t => t.itemName.ToLower().Contains("needle"));

        if (hazard != null)
        {
            UpdateTopTargetIcon(hazard);
            manager.SpawnItemUI(hazard, false, centerSpawnPosition);
            StartCoroutine(FlashSpawnedItems(hazard.itemName, TutorialStep.AvoidHazard));
        }
    }

    private void SpawnScrap()
    {
        TrashData copper = tutorialCopperItem != null ? tutorialCopperItem : manager.trashLibrary.Find(t => t.materialCategory == TrashData.ResourceCategory.Copper);
        if (copper != null)
        {
            UpdateTopTargetIcon(copper);
            manager.SpawnItemUI(copper, false, centerSpawnPosition);
            StartCoroutine(FlashSpawnedItems(copper.itemName, TutorialStep.CollectRecyclable));
        }
    }

    // --- Enhanced Flashing Coroutine ---
    private IEnumerator FlashSpawnedItems(string targetName, TutorialStep requiredStep)
    {
        // Wait longer to ensure items are fully spawned in the hierarchy
        yield return new WaitForSeconds(0.1f);
        
        List<Image> targetsToFlash = new List<Image>();
        Image[] allImages = FindObjectsOfType<Image>();
        
        // Make search entirely case-insensitive and ignore "(Clone)"
        string searchName = targetName.ToLower();

        foreach (Image img in allImages)
        {
            if (img.gameObject.name.ToLower().Contains(searchName) && img.GetComponent<Button>() != null)
            {
                targetsToFlash.Add(img);
            }
        }

        // Pulse alpha while we are on the correct step
        while (currentStep == requiredStep && targetsToFlash.Count > 0)
        {
            foreach (Image targetImg in targetsToFlash)
            {
                if (targetImg != null) targetImg.color = new Color(1f, 1f, 1f, 0.3f);
            }
            yield return new WaitForSeconds(0.25f);
            
            foreach (Image targetImg in targetsToFlash)
            {
                if (targetImg != null) targetImg.color = new Color(1f, 1f, 1f, 1f);
            }
            yield return new WaitForSeconds(0.25f);
        }

        // Reset to solid color when the step changes
        foreach (Image targetImg in targetsToFlash)
        {
            if (targetImg != null) targetImg.color = Color.white;
        }
    }

    private string GetDefaultDialogue(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Welcome: return "Hey, Im Scroungy the Dumpster Guide! Let's learn to dive!";
            case TutorialStep.FindTarget: return "Search your thoughts for something to recycle. Tap that exact item in the pile!";
            case TutorialStep.FlickJunk: return "If you dont see it yet, Flick the item out of the dumpster to dive deeper. More trash will show up.";
            case TutorialStep.OpenBag: return "Sometimes you can rip open bags and boxes to find useful junk and recycling. ";
            case TutorialStep.AvoidHazard: return "Watch out for used needles and shit. Fucking hurts to get poked.";
            case TutorialStep.CollectRecyclable: return "Snatch all the recycling you can think of and trade it for CASH at the recycling center.";
            case TutorialStep.Complete: return "Then check out the thrift store and pawn shop to spend some money!";
            default: return "";
        }
    }
}