using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TugOfWarJuicer : MonoBehaviour
{
    public static TugOfWarJuicer Instance;

    [Header("Top Meter Object")]
    [Tooltip("Assign the GameObject with the hands/slider at the top so it can shake")]
    public Transform topMeterContainer;

    [Header("Responsive Layout Prefab")]
    [Tooltip("Create a UI prefab containing a RawImage component (Ensure Raycast Target is UNCHECKED)")]
    public GameObject messageFeedbackPrefab;

    [Header("Spawn Anchors")]
    public Transform playerTextSpawnAnchor;
    public Transform opponentTextSpawnAnchor;

    [Header("Randomized Message Textures")]
    [Tooltip("Add different 'Good' graphics here (e.g., +1, NICE, GOT IT)")]
    public Texture2D[] playerSuccessTextures;
    [Tooltip("Add different 'Bad' graphics here (e.g., OUCH, -5s, SMASHED)")]
    public Texture2D[] playerFailureTextures;

    private Vector3 originalMeterScale;
    private Vector3 originalMeterPosition;
    private Coroutine activePulseCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (topMeterContainer != null)
        {
            originalMeterScale = topMeterContainer.localScale;
            originalMeterPosition = topMeterContainer.localPosition;
        }
    }

    /// <summary>
    /// Triggers punchy visual feedback on the top meter using random image callouts.
    /// </summary>
    /// <param name="isPlayerSuccess">True if local player gains, False if penalty/opponent gains.</param>
    public void TriggerScoreFeedback(bool isPlayerSuccess)
    {
        // 1. Shake and punch the top meter container
        if (activePulseCoroutine != null) StopCoroutine(activePulseCoroutine);
        activePulseCoroutine = StartCoroutine(PulseMeterRoutine(isPlayerSuccess));

        // 2. Select the correct side anchor and texture array
        Transform targetAnchor = isPlayerSuccess ? playerTextSpawnAnchor : opponentTextSpawnAnchor;
        Texture2D[] targetArray = isPlayerSuccess ? playerSuccessTextures : playerFailureTextures;

        // Safety verification check
        if (messageFeedbackPrefab == null || targetAnchor == null || targetArray == null || targetArray.Length == 0) return;

        // 3. Spawn and pick a random asset texture signature
        GameObject popObj = Instantiate(messageFeedbackPrefab, targetAnchor);
        popObj.transform.localPosition = Vector3.zero;

        RawImage rawImg = popObj.GetComponent<RawImage>();
        if (rawImg != null)
        {
            Texture2D randomTexture = targetArray[Random.Range(0, targetArray.Length)];
            rawImg.texture = randomTexture;
            rawImg.SetNativeSize(); // Automatically scales the boundary to match your drawing's dimensions
        }
        
        StartCoroutine(AnimateFloatingTexture(popObj.transform, isPlayerSuccess));
    }

    private IEnumerator PulseMeterRoutine(bool shiftRight)
    {
        if (topMeterContainer == null) yield break;

        float elapsed = 0f;
        float duration = 0.2f; 

        Vector3 targetPulseScale = originalMeterScale * 1.15f;
        float sidePunch = shiftRight ? 25f : -25f; 

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easedT = Mathf.Sin(t * Mathf.PI); 

            topMeterContainer.localScale = Vector3.Lerp(originalMeterScale, targetPulseScale, easedT);
            topMeterContainer.localPosition = originalMeterPosition + new Vector3(sidePunch * easedT, 0f, 0f);

            yield return null;
        }

        topMeterContainer.localScale = originalMeterScale;
        topMeterContainer.localPosition = originalMeterPosition;
    }

    private IEnumerator AnimateFloatingTexture(Transform popTransform, bool isPlayerSuccess)
    {
        float elapsed = 0f;
        float duration = 0.65f;
        
        Vector3 startPos = popTransform.localPosition;
        
        // Drift downwards and slightly outwards to emphasize entry momentum
        float horizontalDrift = isPlayerSuccess ? -45f : 45f;
        Vector3 driftDirection = new Vector3(horizontalDrift, -140f, 0f); 
        Vector3 targetPos = startPos + driftDirection;

        RawImage img = popTransform.GetComponent<RawImage>();
        Color startColor = img != null ? img.color : Color.white;
        Vector3 nativeScale = popTransform.localScale;
        
        // Give it a subtle chaotic tilt so it feels loose and hand-drawn
        popTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            popTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            
            // Initial pop animation scale curve
            popTransform.localScale = Vector3.Lerp(nativeScale * 1.4f, nativeScale, t * 2.5f);

            // Fade out texture transparency smoothly at the end
            if (img != null)
            {
                img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
            }

            yield return null;
        }

        Destroy(popTransform.gameObject);
    }
}