using UnityEngine;
using System.Collections;
using UnityEngine.UI; 
using TMPro;

public class DumpsterJuiceFX : MonoBehaviour
{
    public static DumpsterJuiceFX Instance;

    [Header("UI Shake Settings")]
    public RectTransform uiShakePanel; 
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 15f;

    [Header("UI Vector Destinations")]
    public RectTransform playerAvatarTarget; 
    public RectTransform opponentBinTarget;  
    
    [Header("Fortune Cookie UI")]
    public GameObject fortunePaperPanel; // Assign a UI panel with a Text component
    public TMP_Text fortuneTextDisplay;

    private Vector3 originalLocalPos; // CHANGED: Now using raw Vector3 local space
    private bool isShaking = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (uiShakePanel != null) 
            originalLocalPos = uiShakePanel.localPosition;
    }

    // --- EFFECT 1: SCREEN SHAKE ON ERRORS ---
    public void TriggerScreenShake()
    {
        if (uiShakePanel != null)
        {
            // Failsafe in case Start() hadn't cached it yet
            if (originalLocalPos == Vector3.zero) 
                originalLocalPos = uiShakePanel.localPosition;

            if (!isShaking)
                StartCoroutine(ShakeCoroutine());
        }
        else
        {
            Debug.LogWarning("DUMPSTER JUICE: Screen shake failed because 'UI Shake Panel' is not assigned in the Inspector!");
        }
    }

    IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;
        
        // Boosted the magnitude slightly so you can definitely feel it
        float actualMagnitude = shakeMagnitude * 1.5f; 

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float damper = 1.0f - (elapsed / shakeDuration); 
            
            float x = Random.Range(-1f, 1f) * actualMagnitude * damper;
            float y = Random.Range(-1f, 1f) * actualMagnitude * damper;

            // FIXED: Modifying raw localPosition bypasses UI layout groups!
            uiShakePanel.localPosition = new Vector3(originalLocalPos.x + x, originalLocalPos.y + y, originalLocalPos.z);
            yield return null;
        }

        uiShakePanel.localPosition = originalLocalPos;
        isShaking = false;
    }

    // --- EFFECT 2: FLY UP AT PLAYER CELEBRATION ---
    public void PlayItemFlyToPlayer(GameObject itemObj)
    {
        if (itemObj == null || playerAvatarTarget == null) return;
        
        StartCoroutine(FlyToTargetCoroutine(itemObj.GetComponent<RectTransform>(), playerAvatarTarget, true));
    }

    IEnumerator FlyToTargetCoroutine(RectTransform itemRt, RectTransform targetRt, bool scaleUpAndFade)
    {
        itemRt.SetAsLastSibling(); 
        if (itemRt.TryGetComponent(out CanvasGroup cg)) cg.blocksRaycasts = false;
        if (itemRt.TryGetComponent(out UnityEngine.UI.Button btn)) btn.enabled = false;

        float duration = 0.45f;
        float elapsed = 0f;

        Vector2 startPos = itemRt.anchoredPosition;
        Vector3 startScale = itemRt.localScale;
        float startRotation = itemRt.localEulerAngles.z;
        float targetRotation = startRotation + Random.Range(-180f, 180f);

        Vector2 finalTargetPos = transform.InverseTransformPoint(targetRt.position);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tEased = t * t * (3f - 2f * t); 

            Vector2 currentLerp = Vector2.Lerp(startPos, finalTargetPos, tEased);
            if (!scaleUpAndFade)
            {
                float arcHeight = Mathf.Sin(t * Mathf.PI) * 125f;
                currentLerp.y += arcHeight;
            }
            itemRt.anchoredPosition = currentLerp;
            itemRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, targetRotation, t));

            if (scaleUpAndFade)
            {
                itemRt.localScale = Vector3.Lerp(startScale, startScale * 1.6f, tEased);
                if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, tEased);
            }
            else
            {
                itemRt.localScale = Vector3.Lerp(startScale, Vector3.zero, tEased);
            }
            yield return null;
        }
        Destroy(itemRt.gameObject);
    }

    // --- EFFECT 3: FLICK AWAY OFF-SCREEN ---
    public void PlayItemFlickAway(GameObject itemObj, Vector2 flickVelocity)
    {
        if (itemObj == null) return;
        StartCoroutine(FlickAwayCoroutine(itemObj.GetComponent<RectTransform>(), flickVelocity));
    }

    IEnumerator FlickAwayCoroutine(RectTransform itemRt, Vector2 flickVelocity)
    {
        itemRt.SetAsLastSibling(); 
        if (itemRt.TryGetComponent(out CanvasGroup cg)) cg.blocksRaycasts = false;
        if (itemRt.TryGetComponent(out UnityEngine.UI.Button btn)) btn.enabled = false;

        float duration = 0.4f;
        float elapsed = 0f;

        Vector3 startScale = itemRt.localScale;
        Vector2 throwDirection = flickVelocity.sqrMagnitude > 0.1f ? flickVelocity.normalized : Vector2.up;
        float throwSpeed = Mathf.Max(flickVelocity.magnitude * 2f, 1500f); 
        float spinSpeed = Random.Range(700f, 1200f) * (Random.value > 0.5f ? 1f : -1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            itemRt.anchoredPosition += throwDirection * throwSpeed * Time.deltaTime;
            itemRt.Rotate(0, 0, spinSpeed * Time.deltaTime);

            itemRt.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(itemRt.gameObject);
    }

    // --- EFFECT 4: INCOMING TRASH FROM OPPONENT ---
    public void PlayIncomingTrashFlyIn(GameObject itemObj)
    {
        if (itemObj == null || opponentBinTarget == null) return;
        StartCoroutine(IncomingFlyCoroutine(itemObj.GetComponent<RectTransform>()));
    }

    IEnumerator IncomingFlyCoroutine(RectTransform itemRt)
    {
        if (itemRt.TryGetComponent(out CanvasGroup cg)) cg.blocksRaycasts = false;
        
        float duration = 0.5f;
        float elapsed = 0f;

        Vector2 finalTargetPos = itemRt.anchoredPosition; 
        Vector2 startPos = transform.InverseTransformPoint(opponentBinTarget.position);
        itemRt.anchoredPosition = startPos;
        
        // FIXED: Cache the item's actual native scale so we don't accidentally resize it!
        Vector3 targetScale = itemRt.localScale;
        
        float startRotation = Random.Range(-180f, 180f);
        float targetRotation = Random.Range(-30f, 30f);

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tEased = 1f - (1f - t) * (1f - t); 
            
            itemRt.anchoredPosition = Vector2.Lerp(startPos, finalTargetPos, tEased);
            itemRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, targetRotation, t));
            
            // FIXED: Lerp from a blown-up version of its native scale back down to normal
            itemRt.localScale = Vector3.Lerp(targetScale * 1.8f, targetScale, tEased);
            
            yield return null;
        }
        
        itemRt.anchoredPosition = finalTargetPos;
        itemRt.localScale = targetScale; // Safety snap at the end of the flight
        if (cg != null) cg.blocksRaycasts = true;
    }
    
    // --- EFFECT 5: VIBRATOR SABOTAGE (Moved from GameSync) ---
    public void PlayVibratorSlam(Sprite vibratorSprite)
    {
        // 🌟 PLAY THE SOUND FOR THE VICTIM
        if (DumpsterManager.Instance != null)
        {
            TrashData vibData = DumpsterManager.Instance.trashLibrary.Find(t => t.type == TrashData.ItemType.Vibrator);
            if (vibData != null && vibData.specialActionSound != null)
            {
                DumpsterManager.Instance.PlaySound2D(vibData.specialActionSound);
            }
        }

        StartCoroutine(VibratorSabotageRoutine(vibratorSprite));
    }

    private IEnumerator VibratorSabotageRoutine(Sprite vibratorSprite)
    {
        if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("INCOMING SABOTAGE!", Color.magenta);

        GameObject dummyVisual = new GameObject("VibratorSlamVisual");
        dummyVisual.transform.SetParent(DumpsterManager.Instance.spawnArea.parent, false); 
                
        RectTransform rt = dummyVisual.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; 
        rt.sizeDelta = new Vector2(300, 300); 
                
        Image img = dummyVisual.AddComponent<Image>();
        img.sprite = vibratorSprite;

        float slamDuration = 0.35f;
        float elapsedSlam = 0f;
                
        while (elapsedSlam < slamDuration)
        {
            elapsedSlam += Time.deltaTime;
            float t = elapsedSlam / slamDuration;
            float easeOutBack = Mathf.Sin(t * Mathf.PI * 0.5f); 
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2.5f, easeOutBack);
            yield return null;
        }

        TriggerScreenShake();
        yield return new WaitForSeconds(shakeDuration);

        if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("YOUR DUMPSTER WAS SCRAMBLED!", Color.magenta);
        DumpsterManager.Instance.ScrambleAllTrashPositions();
        TriggerScreenShake();

        float dropDuration = 0.4f;
        float elapsedDrop = 0f;
        Vector3 startPos = rt.anchoredPosition;
                
        while (elapsedDrop < dropDuration)
        {
            elapsedDrop += Time.deltaTime;
            float dropCurve = (elapsedDrop / dropDuration) * (elapsedDrop / dropDuration);
            rt.anchoredPosition = startPos + new Vector3(0, -1500f * dropCurve, 0);
            rt.Rotate(0, 0, 300f * Time.deltaTime);
            yield return null;
        }
                
        Destroy(dummyVisual);
    }

    // --- EFFECT 6: FORTUNE COOKIE REVEAL ---
    
    // THIS LINE RIGHT HERE fixes the error! It must sit outside the methods.
    private Coroutine activeFortuneRoutine; 

    public void PlayFortuneReveal(string message)
    {
        if (fortunePaperPanel == null || fortuneTextDisplay == null) return;
        
        // Stop any currently playing cookie animations before starting a new one
        if (activeFortuneRoutine != null) StopCoroutine(activeFortuneRoutine);
        activeFortuneRoutine = StartCoroutine(FortuneRevealRoutine(message));
    }

    // THE KILL SWITCH: Clears the UI instantly if the game ends
    public void ForceClearFortuneUI()
    {
        if (activeFortuneRoutine != null) StopCoroutine(activeFortuneRoutine);
        if (fortunePaperPanel != null) fortunePaperPanel.SetActive(false);
    }

    private IEnumerator FortuneRevealRoutine(string message)
    {
        fortuneTextDisplay.text = message;
        fortunePaperPanel.SetActive(true);
        
        CanvasGroup cg = fortunePaperPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = fortunePaperPanel.AddComponent<CanvasGroup>();

        // Makes the panel completely click-through!
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Fade In 
        while (cg.alpha < 1f)
        {
            cg.alpha += Time.unscaledDeltaTime * 4f;
            yield return null;
        }
        
        // Fast display time (1.25 seconds)
        yield return new WaitForSecondsRealtime(1.25f); 
        
        // Fade Out
        while (cg.alpha > 0f)
        {
            cg.alpha -= Time.unscaledDeltaTime * 4f;
            yield return null;
        }
        
        fortunePaperPanel.SetActive(false);
        
        // Clears the tracker when the animation finishes naturally
        activeFortuneRoutine = null; 
    }
	
	
}