using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class DumpsterManager : MonoBehaviour
{
    public static DumpsterManager Instance;

    [Header("Trash Assets & Pools")]
    public List<TrashData> trashLibrary;
    public GameObject itemPrefab;
    public Transform spawnArea;

    [HideInInspector] public IDumpsterMode activeModeLogic;
    private List<TrashData> weightedPool = new List<TrashData>();
    public RectTransform areaRect; // Exposed to public for mode script reference lookups
    private HashSet<string> activeItemsInDumpster = new HashSet<string>();
    
    [Header("UI Reference")]
    public string myTargetItem;

    // GLOBAL TRACKING LISTS
    [HideInInspector] public List<Sprite> sessionItemsFound = new List<Sprite>();
    [HideInInspector] public List<Sprite> sessionItemsUsed = new List<Sprite>();
    [HideInInspector] public float matchStartTime;

    [Header("Optimization: End Round Rain Pool")]
    private List<GameObject> rainPool = new List<GameObject>();
    private const int RAIN_POOL_SIZE = 80; 
    private bool isInfiniteRainActive = false; 
    public Transform postRoundRainContainer;
	
	[Header("Current Session Data Cache")]
	public MatchSessionRewards currentSessionRewards = new MatchSessionRewards();
	
	[Header("Audio Optimization Pool")]
    private List<AudioSource> audioPool = new List<AudioSource>();
    private const int AUDIO_POOL_SIZE = 12;

    void Awake()
    {
        Instance = this;
        areaRect = spawnArea.GetComponent<RectTransform>();

        // Pre-warm the audio pool to eliminate lag spikes
        for (int i = 0; i < AUDIO_POOL_SIZE; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // Forces strictly 2D UI sound
            audioPool.Add(src);
        }
    }

	void Start()
    {
        currentSessionRewards.Clear();
        int initialTrashCount = 250; 

        GameObject tutorialUI = GameObject.Find("TutorialPanel");

        if (Launcher.IsTutorial)
        {
            activeModeLogic = gameObject.AddComponent<TutorialDumpsterMode>();
            initialTrashCount = 0; 
        }
        else 
        {
            if (tutorialUI != null) tutorialUI.SetActive(false);

            if (Launcher.IsTimeAttack)
            {
                activeModeLogic = gameObject.AddComponent<SoloDumpsterMode>();
                
                // PULL THE PRESET TRASH COUNT HERE
                initialTrashCount = Launcher.SoloTrashSpawnAmount; 
            }
            else
            {
                activeModeLogic = gameObject.AddComponent<MultiplayerDumpsterMode>();
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("TrashSpawnAmount", out object spawnCountObj))
                {
                    initialTrashCount = System.Convert.ToInt32(spawnCountObj);
                }
            }
        }

        BuildWeightedPoolByRarity();
        PrewarmRainPool(); 
        StartCoroutine(InitialDumpsterFillPipeline(initialTrashCount));
    }
	
	public void PlaySound2D(AudioClip clip)
    {
        if (clip == null) return;
        
        foreach (AudioSource src in audioPool)
        {
            if (!src.isPlaying)
            {
                src.clip = clip;
                src.Play();
                return;
            }
        }
        
        // Failsafe: If all 12 are currently playing, hijack the oldest one
        audioPool[0].clip = clip;
        audioPool[0].Play();
    }

    void BuildWeightedPoolByRarity()
    {
        weightedPool.Clear();
        foreach (TrashData item in trashLibrary)
        {
            int frequencyCount = Mathf.Max(1, item.spawnRarityWeight);
            for (int i = 0; i < frequencyCount; i++) 
            {
                weightedPool.Add(item);
            }
        }
    }

    void PrewarmRainPool()
    {
        for (int i = 0; i < RAIN_POOL_SIZE; i++)
        {
            GameObject obj = Instantiate(itemPrefab, postRoundRainContainer != null ? postRoundRainContainer : spawnArea);
            
            if (obj.TryGetComponent(out DraggableTrash drag)) Destroy(drag);
            if (obj.TryGetComponent(out Button btn)) Destroy(btn);
            if (obj.TryGetComponent(out CanvasGroup cg))
            {
                cg.blocksRaycasts = false;
            }
            else
            {
                cg = obj.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
            }

            obj.SetActive(false);
            rainPool.Add(obj);
        }
    }

    IEnumerator InitialDumpsterFillPipeline(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnRandomTrash(false, Vector2.zero);
            if (i % 25 == 0) yield return null;
        }

        PickNewTarget();

        if (GlobalLoadingScreen.Instance != null)
        {
            GlobalLoadingScreen.Instance.HideManualLoadingScreen();
        }
    }

    public TrashData GetRandomTrashData() => weightedPool[Random.Range(0, weightedPool.Count)];

    public void SpawnRandomTrash(bool isExplosion, Vector2 origin) 
    {
        SpawnItemUI(GetRandomTrashData(), isExplosion, origin);
    }

    // 🌟 NEW HELPER METHOD: Generates a beautiful animated item drop from above the screen boundary limits
    public void TossInNewTrash(TrashData specificData = null)
    {
        TrashData dataToSpawn = (specificData != null) ? specificData : GetRandomTrashData();

        float w = areaRect.rect.width / 2f;
        float h = areaRect.rect.height / 2f;

        // Position initial birth coordinates safely out-of-bounds above the UI container ceiling
        Vector2 spawnCeilingOrigin = new Vector2(Random.Range(-w * 0.7f, w * 0.7f), h + 150f);
        
        // Formulate a random parabolic landing destination coordinate anywhere within the core box walls
        Vector2 randomInternalTarget = new Vector2(Random.Range(-w * 0.8f, w * 0.8f), Random.Range(-h * 0.8f, h * 0.8f));

        SpawnItemUI(dataToSpawn, true, spawnCeilingOrigin, randomInternalTarget);
    }

	public void SpawnItemUI(TrashData data, bool isSpilling, Vector2 origin, Vector2? targetOverride = null)
    {
        if (data == null) return;

        GameObject obj = Instantiate(itemPrefab, spawnArea);
        RectTransform rt = obj.GetComponent<RectTransform>();
        
        Image img = obj.GetComponentInChildren<Image>();
        if (img != null && data.itemSprite != null)
        {
            img.sprite = data.itemSprite;
            img.preserveAspect = true; 
        }
        
        rt.localScale = Vector3.one * data.size;
        
        if (obj.TryGetComponent(out DraggableTrash drag)) drag.data = data;
        
        if (obj.TryGetComponent(out Button btn))
        {
            btn.onClick.AddListener(() => activeModeLogic.HandleItemClicked(data.itemName, obj, data));
        }

        float w = areaRect.rect.width / 2f;
        float h = areaRect.rect.height / 2f;

        float halfItemWidth = (rt.rect.width * data.size) / 2f;
        float halfItemHeight = (rt.rect.height * data.size) / 2f;

        if (isSpilling && targetOverride.HasValue)
        {
            float clampedOriginX = Mathf.Clamp(origin.x, -w + halfItemWidth, w - halfItemWidth);
            float clampedOriginY = Mathf.Clamp(origin.y, -h + halfItemHeight, h - halfItemHeight);
            rt.anchoredPosition = new Vector2(clampedOriginX, clampedOriginY);

            Vector2 safeTarget = targetOverride.Value;
            safeTarget.x = Mathf.Clamp(safeTarget.x, -w + halfItemWidth, w - halfItemWidth);
            safeTarget.y = Mathf.Clamp(safeTarget.y, -h + halfItemHeight, h - halfItemHeight);

            float speedModifier = Mathf.Clamp(data.weight, 0.5f, 3.0f);
            
            // 🌟 PASSED THE AUDIO CLIP INTO THE ANIMATION
            StartCoroutine(AnimateSpill(rt, safeTarget, 0.6f / speedModifier, halfItemHeight, data.dropSound));
        }
        else
        {
            float randomX = Random.Range(-w + halfItemWidth, w - halfItemWidth);
            float randomY = Random.Range(-h + halfItemHeight, h - halfItemHeight);

            rt.anchoredPosition = new Vector2(randomX, randomY);
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }
    }

    public void PickNewTarget()
    {
        RecalculateActiveDumpsterItems();
        List<TrashData> validSpawnsToFind = new List<TrashData>();
        
        foreach (TrashData item in trashLibrary)
        {
            bool isPhysicallyPresent = activeItemsInDumpster.Contains(item.itemName);
            bool isAllowedTargetType = (item.classification == TrashData.GameItemClassification.StandardTarget || 
                                        item.classification == TrashData.GameItemClassification.HybridEffectTarget);

            if (isPhysicallyPresent && isAllowedTargetType)
            {
                validSpawnsToFind.Add(item);
            }
        }

        // 🌟 CRITICAL SCENE PRESENCE ENFORCEMENT FIX:
        // If there are absolutely NO findable items physically present in the scene,
        // intercept the code flow, choose a valid target configuration, and force-toss it into play instantly!
        if (validSpawnsToFind.Count == 0)
        {
            List<TrashData> targetClassificationsMasterList = trashLibrary.FindAll(item => 
                item.classification == TrashData.GameItemClassification.StandardTarget || 
                item.classification == TrashData.GameItemClassification.HybridEffectTarget);

            if (targetClassificationsMasterList.Count > 0)
            {
                TrashData fallbackTarget = targetClassificationsMasterList[Random.Range(0, targetClassificationsMasterList.Count)];
                
                // Toss the missing target selection directly down into view layout fields
                TossInNewTrash(fallbackTarget);
                validSpawnsToFind.Add(fallbackTarget);
            }
        }

        if (validSpawnsToFind.Count > 0)
        {
            TrashData selected = validSpawnsToFind[Random.Range(0, validSpawnsToFind.Count)];
            myTargetItem = selected.itemName;

            GameSync gs = FindObjectOfType<GameSync>();
            if (gs != null)
            {
                int index = trashLibrary.IndexOf(selected);
                if (PhotonNetwork.InRoom)
                {
                    var networkProps = new ExitGames.Client.Photon.Hashtable { { "TargetIndex", index } };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(networkProps);
                }
                gs.UpdateLocalTargetUI(selected);
            }
        }
    }

    private void RecalculateActiveDumpsterItems()
    {
        activeItemsInDumpster.Clear();
        DraggableTrash[] itemsInScene = spawnArea.GetComponentsInChildren<DraggableTrash>(false);
        
        foreach (DraggableTrash trash in itemsInScene)
        {
            if (trash.data != null && !activeItemsInDumpster.Contains(trash.data.itemName))
            {
                activeItemsInDumpster.Add(trash.data.itemName);
            }
        }
    }

	IEnumerator AnimateSpill(RectTransform rt, Vector2 target, float dur, float halfHeight, AudioClip landSound)
    {
        float t = 0;
        Vector2 start = rt.anchoredPosition;
        float h = areaRect.rect.height / 2f;

        while (t < dur)
        {
            if (rt == null) yield break;

            t += Time.deltaTime;
            float p = t / dur;
            
            float arc = Mathf.Sin(p * Mathf.PI) * 60f;
            Vector2 currentLerpPos = Vector2.Lerp(start, target, p) + new Vector2(0, arc);

            currentLerpPos.y = Mathf.Clamp(currentLerpPos.y, -h + halfHeight, h - halfHeight);

            rt.anchoredPosition = currentLerpPos;
            rt.localRotation = Quaternion.Euler(0, 0, p * 360f);
            yield return null;
        }

        if (rt != null) rt.anchoredPosition = target;

        // 🌟 PLAYS THE SOUND EXACTLY WHEN IT HITS THE FLOOR
        if (landSound != null && Time.timeSinceLevelLoad > 1.5f)
        {
            PlaySound2D(landSound);
        }
    }

    public void TriggerTrashStorm()
    {
        if (isInfiniteRainActive) return;
        isInfiniteRainActive = true;
        StartCoroutine(InfiniteTrashStormCoroutine());
    }

    IEnumerator InfiniteTrashStormCoroutine()
    {
        float spawnWidth = (areaRect.rect.width / 2f) * 1.5f; 
        float spawnHeightTop = (areaRect.rect.height / 2f) + 250f; 
        float killHeightBottom = -(areaRect.rect.height / 2f) - 250f; 

        for (int i = 0; i < rainPool.Count; i++)
        {
            if (!isInfiniteRainActive) yield break;

            GameObject item = rainPool[i];
            if (item != null)
            {
                LaunchPooledTrash(item, spawnWidth, spawnHeightTop, killHeightBottom);
            }

            if (i % 2 == 0) yield return null;
        }
    }

    void LaunchPooledTrash(GameObject item, float spawnWidth, float spawnHeightTop, float killHeightBottom)
    {
        TrashData data = GetRandomTrashData(); 
        
        Image img = item.GetComponentInChildren<Image>(); 
        if (img != null && data.itemSprite != null)
        {
            img.sprite = data.itemSprite;
            img.preserveAspect = true;
        }
        
        RectTransform rt = item.GetComponent<RectTransform>(); 
        rt.anchoredPosition = new Vector2(Random.Range(-spawnWidth, spawnWidth), spawnHeightTop); 
        rt.localScale = Vector3.one * data.size; 
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f)); 

        rt.SetAsLastSibling(); 
        item.SetActive(true); 

        float baseSpeed = Random.Range(1100f, 1800f); 
        float fallSpeed = baseSpeed * Mathf.Max(0.8f, data.weight); 
        float spinSpeed = Random.Range(300f, 800f) * (Random.value > 0.5f ? 1f : -1f); 
        
        StartCoroutine(AnimateAndRecycleRainDrop(item, rt, fallSpeed, spinSpeed, spawnWidth, spawnHeightTop, killHeightBottom)); 
    }

    IEnumerator AnimateAndRecycleRainDrop(GameObject item, RectTransform rt, float speed, float spin, float spawnWidth, float spawnHeightTop, float killHeightBottom)
    {
        while (isInfiniteRainActive && rt != null && rt.anchoredPosition.y > killHeightBottom)
        {
            rt.anchoredPosition += Vector2.down * speed * Time.deltaTime;
            rt.Rotate(0, 0, spin * Time.deltaTime);
            yield return null;
        }

        if (isInfiniteRainActive && rt != null && item != null)
        {
            LaunchPooledTrash(item, spawnWidth, spawnHeightTop, killHeightBottom);
        }
        else if (item != null)
        {
            item.SetActive(false);
        }
    }

    void OnDestroy()
    {
        isInfiniteRainActive = false;
    }
    
    public void ScrambleAllTrashPositions()
    {
        float w = areaRect.rect.width / 2f;
        float h = areaRect.rect.height / 2f;

        DraggableTrash[] activeTrash = spawnArea.GetComponentsInChildren<DraggableTrash>(false);

        foreach (DraggableTrash trash in activeTrash)
        {
            if (trash != null)
            {
                RectTransform rt = trash.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float halfItemWidth = (rt.rect.width * trash.data.size) / 2f;
                    float halfItemHeight = (rt.rect.height * trash.data.size) / 2f;

                    Vector2 newPos = new Vector2(Random.Range(-w + halfItemWidth, w - halfItemWidth), Random.Range(-h + halfItemHeight, h - halfItemHeight));
                    
                    if (Launcher.IsTimeAttack && (trash.data.type == TrashData.ItemType.Needle || trash.data.type == TrashData.ItemType.Diaper))
                    {
                        TrashData cleanData = GetRandomTrashData();
                        while(cleanData.type == TrashData.ItemType.Needle || cleanData.type == TrashData.ItemType.Diaper)
                        {
                            cleanData = GetRandomTrashData();
                        }
                        trash.data = cleanData;
                        
                        rt.localScale = Vector3.one * cleanData.size; 
                        Image img = trash.GetComponentInChildren<Image>();
                        if (img != null && cleanData.itemSprite != null)
                        {
                            img.sprite = cleanData.itemSprite;
                            img.preserveAspect = true;
                        }
                    }

                    rt.anchoredPosition = newPos;
                    rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                }
            }
        }

        PickNewTarget();
    }
	
	public void ClearJunkForFortune()
	{
		DraggableTrash[] activeTrash = spawnArea.GetComponentsInChildren<DraggableTrash>(false);
		foreach (DraggableTrash trash in activeTrash)
		{
			if (trash.data != null && trash.data.itemName != myTargetItem && trash.data.type == TrashData.ItemType.Junk)
			{
				// Add a little poof/fade if you have a particle system, otherwise just destroy
				Destroy(trash.gameObject); 
			}
		}
	}
	
	/// <summary>
	/// Safely purges non-target items from the active dumpster spawn area.
	/// </summary>
	public void PurgeNonTargetTrash(int countToRemove)
	{
		DraggableTrash[] activeTrash = spawnArea.GetComponentsInChildren<DraggableTrash>(false);
		int removed = 0;

		foreach (DraggableTrash trash in activeTrash)
		{
			if (trash != null && trash.data != null)
			{
				if (trash.data.itemName != myTargetItem)
				{
					Destroy(trash.gameObject);
					removed++;
					if (removed >= countToRemove) break;
				}
			}
		}
	}
}

[System.Serializable]
public class MatchSessionRewards
{
    public int aluminumEarned;
    public int glassEarned;
    public int plasticEarned;
    public int otherEarned;
	public int copperEarned;

    public void Clear()
    {
        aluminumEarned = 0;
        glassEarned = 0;
        plasticEarned = 0;
        otherEarned = 0;
		copperEarned = 0;
    }
}