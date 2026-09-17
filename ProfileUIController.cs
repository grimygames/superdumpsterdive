using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ProfileUIController : MonoBehaviour
{
    public static ProfileUIController Instance;

    [Header("💥 Visual Panel Animations")]
    public GameObject profileOverlayPanel;
    public CanvasGroup panelCanvasGroup;
    public Button logoButton;
    public Button closeButton;

    [Header("📊 Data Text Fields")]
    public TMP_Text accountTypeField;
    public TMP_Text highestScoreField;
    public TMP_Text aluminumCountField;
    public TMP_Text glassCountField;
    public TMP_Text plasticCountField;
    public TMP_Text otherCountField;

    [Header("🎨 Profile Customization Frames")]
    public Image avatarDisplayImage;
    public Image borderFrameGraphic;
    public Image sprayCanColorIndicator;

    [Header("🎵 Audio Feedback Triggers")]
    public AudioSource uiAudioSource;
    public AudioClip openSound;
    public AudioClip tickSound;

    private bool isGamePaused = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Set up clean interface initializations
        if (profileOverlayPanel != null) profileOverlayPanel.SetActive(false);
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

        // Auto-wire buttons to keep the inspector clean
        if (logoButton != null) logoButton.onClick.AddListener(TogglePauseStateEngine);
        if (closeButton != null) closeButton.onClick.AddListener(TogglePauseStateEngine);
    }

    void Update()
    {
        // Listen for universal Pause inputs (Escape or P)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePauseStateEngine();
        }
    }

    // --- 💥 THE JUICE: OPEN & CLOSE PANEL TRANSITIONS ---
    public void TogglePauseStateEngine()
    {
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            StopAllCoroutines();
            profileOverlayPanel.SetActive(true);
            
            // Play a satisfying open sound
            if (uiAudioSource != null && openSound != null) uiAudioSource.PlayOneShot(openSound);
            
            // Animate panel fade-in and counting ticks at the end of the frame layout
            StartCoroutine(FadePanel(1f));
            PopulateAndJuiceDetails();
            StartCoroutine(DelayTimePause(true));
        }
        else
        {
            StopAllCoroutines();
            Time.timeScale = 1f; // Restore baseline time ticks instantly
            StartCoroutine(FadePanel(0f));
            StartCoroutine(DelayTimePause(false));
            Debug.Log("[SYSTEM] Gameplay resumed.");
        }
    }

    private void PopulateAndJuiceDetails()
    {
        PlayerProfileData profileData = PlayFabAuthBridge.Instance.profile;
        if (profileData == null) return;

       if (accountTypeField != null) 
		accountTypeField.text = "<color=#00FF66>ACTIVE DIVER</color>";

        // 2. 🌟 JUICED TICK COUNTERS: Animate numbers counting up rather than instantly snapping!
        StartCoroutine(AnimateTextCounter(highestScoreField, 0, profileData.highestScore, "HIGH SCORE: "));
        StartCoroutine(AnimateTextCounter(aluminumCountField, 0, profileData.aluminumCount, "ALUMINUM: "));
        StartCoroutine(AnimateTextCounter(glassCountField, 0, profileData.glassCount, "GLASS: "));
        StartCoroutine(AnimateTextCounter(plasticCountField, 0, profileData.plasticCount, "PLASTIC: "));
        StartCoroutine(AnimateTextCounter(otherCountField, 0, profileData.otherCount, "OTHER RES: "));

        // 3. Apply Cosmetic Theme Syncs
        if (ColorUtility.TryParseHtmlString(profileData.selectedSprayColorHex, out Color activeSprayColor))
        {
            if (sprayCanColorIndicator != null) sprayCanColorIndicator.color = activeSprayColor;
        }

        // Apply active base64 layout graphics if it exists
        if (!string.IsNullOrEmpty(profileData.activeAvatarBase64))
        {
            // Convert back to Texture2D when loading avatar icons
            byte[] imageBytes = System.Convert.FromBase64String(profileData.activeAvatarBase64);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(imageBytes) && avatarDisplayImage != null)
            {
                avatarDisplayImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
    }

    // --- 📊 TEXT TICK ANIMATION ---
    private IEnumerator AnimateTextCounter(TMP_Text targetField, int startVal, int endVal, string prefixLabel)
    {
        if (targetField == null) yield break;

        float elapsed = 0f;
        float duration = 0.4f; // Complete the whole counting loop in less than half a second

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled delta time since Time.timeScale will hit 0!
            float percent = Mathf.Clamp01(elapsed / duration);
            int currentCount = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, percent));
            
            targetField.text = prefixLabel + currentCount.ToString();
            
            // Soft mechanical click noises while numbers roll
            if (uiAudioSource != null && tickSound != null && currentCount % 2 == 0) 
                uiAudioSource.PlayOneShot(tickSound, 0.2f);

            yield return null;
        }

        targetField.text = prefixLabel + endVal.ToString();
    }

    // --- 🖼️ SMOOTH COROUTINE PANEL LERPS ---
    private IEnumerator FadePanel(float targetAlpha)
    {
        if (panelCanvasGroup == null) yield break;

        float elapsed = 0f;
        float duration = 0.15f;
        float startAlpha = panelCanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0f) profileOverlayPanel.SetActive(false);
    }

    private IEnumerator DelayTimePause(bool shouldPause)
    {
        yield return new WaitForEndOfFrame();
        if (shouldPause && isGamePaused)
        {
            Time.timeScale = 0f;
            Debug.Log("[SYSTEM] Core engine scales frozen safely.");
        }
    }
}