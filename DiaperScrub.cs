using UnityEngine;
using UnityEngine.UI;

public class DiaperScrub : MonoBehaviour
{
    public RawImage smearImage;
    public CanvasGroup canvasGroup; 
    public float brushSize = 0.1f;
    public float fadeSpeed = 2f;
    public float autoClearTime = 20f;
    
    [Header("Slide Settings")]
    [Tooltip("How fast the image slides down the screen in pixels per second.")]
    public float slideSpeed = 25f;
    [Tooltip("Maximum distance down it can slide before freezing or stopping.")]
    public float maxSlideDistance = 400f;

    private Texture2D maskTexture;
    private float totalPixels;
    private float erasedPixels;
    private bool isEffectActive = false;
    private float targetAlpha = 0f;
    private float currentTimer = 0f;

    // Cache layout coordinate vectors to cleanly handle slide logic
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private float currentSlideAmount = 0f;

    void Awake()
    {
        if (smearImage != null)
        {
            rectTransform = smearImage.rectTransform;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    void Start()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        InitializeMask();
    }

    public void SmearScreen() => ActivateSmear();

    public void ActivateSmear()
    {
        InitializeMask();
        
        // Reset positioning tracking before starting the downward crawl
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
            currentSlideAmount = 0f;
        }

        isEffectActive = true;
        targetAlpha = 1f; 
        canvasGroup.blocksRaycasts = true; 
        currentTimer = autoClearTime;
    }

    void InitializeMask()
    {
        if (smearImage == null) return;
        maskTexture = new Texture2D(128, 128, TextureFormat.Alpha8, false);
        Color32[] resetColors = new Color32[128 * 128];
        for (int i = 0; i < resetColors.Length; i++) resetColors[i] = new Color32(255, 255, 255, 255);
        maskTexture.SetPixels32(resetColors);
        maskTexture.Apply();

        if (smearImage.material != null) smearImage.material.SetTexture("_MaskTex", maskTexture);
        totalPixels = 128 * 128;
        erasedPixels = 0;
    }

    void Update()
    {
        if (canvasGroup == null) return;
        if (canvasGroup.alpha != targetAlpha) canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        if (!isEffectActive) return;

        // Handle the slow slide downward
        HandleDownwardSlide();

        if (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer < 3f) targetAlpha = currentTimer / 3f; 
        }
        else ClearEffect();

        if (canvasGroup.alpha > 0.5f && Input.GetMouseButton(0)) WipeAtPosition(Input.mousePosition);
    }

    private void HandleDownwardSlide()
    {
        if (rectTransform == null || currentSlideAmount >= maxSlideDistance) return;

        // Calculate frame step movement values
        float slideStep = slideSpeed * Time.deltaTime;
        currentSlideAmount += slideStep;

        // Cap the distance so it doesn't leave the screen bounds entirely
        currentSlideAmount = Mathf.Min(currentSlideAmount, maxSlideDistance);

        // Displace the layout along the negative Y axis
        rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0f, -currentSlideAmount);
    }

    void ClearEffect()
    {
        isEffectActive = false;
        targetAlpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // Snappy return back to standard position hiding bounds
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    void WipeAtPosition(Vector2 screenPos)
    {
        if (smearImage == null || maskTexture == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(smearImage.rectTransform, screenPos, null, out Vector2 localPos);
        
        float x = (localPos.x / smearImage.rectTransform.rect.width + 0.5f) * 128;
        float y = (localPos.y / smearImage.rectTransform.rect.height + 0.5f) * 128;

        int brushRadius = 6; 
        bool changed = false;

        for (int ix = -brushRadius; ix < brushRadius; ix++)
        {
            for (int iy = -brushRadius; iy < brushRadius; iy++)
            {
                if (ix * ix + iy * iy > brushRadius * brushRadius) continue;
                int px = (int)x + ix; int py = (int)y + iy;
                if (px >= 0 && px < 128 && py >= 0 && py < 128)
                {
                    if (maskTexture.GetPixel(px, py).a > 0)
                    {
                        maskTexture.SetPixel(px, py, new Color(0, 0, 0, 0));
                        erasedPixels++;
                        changed = true;
                    }
                }
            }
        }
        if (changed) { maskTexture.Apply(); UpdateCleanliness(); }
    }

    void UpdateCleanliness()
    {
        if (totalPixels == 0) return;
        float cleanPercentage = (erasedPixels / totalPixels);
        if (cleanPercentage > 0.5f && canvasGroup.blocksRaycasts) canvasGroup.blocksRaycasts = false;
        if (cleanPercentage > 0.95f) ClearEffect();
    }

    public bool IsScreenTooDirty() => isEffectActive && (erasedPixels / totalPixels < 0.5f);
}