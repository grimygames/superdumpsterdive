using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AvatarPainter : MonoBehaviour
{
    [Header("🎨 Drawing Settings")]
    public RawImage drawingCanvas;
    public int textureSize = 256;
    public Color currentColor = Color.black; // Defaulted to Black
    public int brushSize = 8;
    [Range(0.1f, 1f)] public float spraySoftness = 0.5f;

    [Header("🌟 Overlay Color Picker & Indicators")]
    public GameObject colorPickerOverlayPanel;  
    public UIColorWheelPicker colorWheelScript;
    public Image colorWheelButtonImage;        
    public UIButtonFeedback colorWheelButtonFeedback; 
    public Image activeColorIndicatorImage;     

    [Header("🖼️ Cosmetic Border Display")]
    public Image borderDisplayImage; 

    [Header("⚡ Animation Settings")]
    public float selectedScaleMultiplier = 1.25f;
    public float animationSpeed = 0.15f;

    [Header("🔘 UI Tool Groups")]
    public List<UIButtonFeedback> toolButtons; 
    public List<UIButtonFeedback> colorButtons; // Preset color buttons

    private Texture2D drawableTexture;
    private Color32[] clearSlate;
    private Dictionary<RectTransform, Vector3> originalScales = new Dictionary<RectTransform, Vector3>();
    
    private Vector2 lastDrawPos;
    private bool isFirstStroke = true;
    private RectTransform canvasRect; 
	
	[Header("🎵 Audio Feedback")]
    public AudioSource sprayAudioSource;
    public AudioClip spraySound;

    void Start()
    {
        if (drawingCanvas != null) canvasRect = drawingCanvas.GetComponent<RectTransform>();
        
        InitializeCanvas();
        RecordOriginalScales(); 
        RefreshBorderDisplay();
        LoadActiveSprayColor(); 
        
        RefreshColorWheelButtonVisuals();

        Invoke("SetDefaults", 0.1f);
    }

    void OnEnable()
    {
        RefreshBorderDisplay();
        LoadActiveSprayColor();
        RefreshColorWheelButtonVisuals();
        if (colorWheelScript != null) colorWheelScript.RefreshLockState();
    }

    // --- 🔒 BUTTON CLICK & LOCK STATE LOGIC ---

    public void HandleColorWheelIconClick()
    {
        bool ownsWheel = false;

        if (PlayerPrefs.GetInt("OwnsColorWheel", 0) == 1) ownsWheel = true;

        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            if (PlayFabAuthBridge.Instance.profile.ownsCustomColorWheel) ownsWheel = true;
        }

        if (ownsWheel)
        {
            ToggleColorPickerOverlay();
        }
        else
        {
            Debug.Log("[Painter] Color Wheel locked. Opening Store Premium Tab...");
            
            DumpsterStoreUI store = DumpsterStoreUI.Instance;
            if (store == null) store = FindObjectOfType<DumpsterStoreUI>(true);

            if (store != null)
            {
                store.OpenStoreToTab("premium");
            }
        }
    }

    public void RefreshColorWheelButtonVisuals()
    {
        bool ownsWheel = false;

        if (PlayerPrefs.GetInt("OwnsColorWheel", 0) == 1) ownsWheel = true;

        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            if (PlayFabAuthBridge.Instance.profile.ownsCustomColorWheel) ownsWheel = true;
        }

        if (colorWheelButtonImage != null)
        {
            colorWheelButtonImage.color = ownsWheel ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.75f);
        }
    }

    // --- 🎨 COLOR SELECTION ENGINE ---

    public void LoadActiveSprayColor()
    {
        // 🌟 FORCE DEFAULT TO BLACK (#000000)
        string savedHex = "#000000";

        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            if (!string.IsNullOrEmpty(PlayFabAuthBridge.Instance.profile.selectedSprayColorHex))
            {
                savedHex = PlayFabAuthBridge.Instance.profile.selectedSprayColorHex;
            }
        }
        else
        {
            savedHex = PlayerPrefs.GetString("SelectedSprayColor", "#000000");
        }
		
		// 🌟 FORCE BLACK IF SAVED HEX IS WHITE OR EMPTY
		if (string.IsNullOrEmpty(savedHex) || savedHex.ToUpper() == "#FFFFFF" || savedHex.ToUpper() == "FFFFFF")
		{
			savedHex = "#000000";
			PlayerPrefs.SetString("SelectedSprayColor", "#000000");
			PlayerPrefs.Save();
		}

        if (ColorUtility.TryParseHtmlString(savedHex, out Color loadedColor))
        {
            currentColor = loadedColor;
            currentColor.a = 1f;

            if (activeColorIndicatorImage != null)
            {
                activeColorIndicatorImage.color = currentColor;
            }
        }
    }

    public void SetCustomColor(Color newColor)
    {
        currentColor = newColor;
        currentColor.a = 1f;

        if (activeColorIndicatorImage != null)
        {
            activeColorIndicatorImage.color = currentColor;
        }

        string hexColor = "#" + ColorUtility.ToHtmlStringRGB(currentColor);
        SaveSprayColor(hexColor);

        ResetEraserVisualsOnly();

        if (colorWheelButtonFeedback != null)
        {
            UpdateGroupUI(colorButtons, colorWheelButtonFeedback);
        }

        CloseColorPickerOverlay();
    }

    public void UI_SetColorCustom(string hex)
    {
        GameObject clickedGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (clickedGO == null) return;
        
        UIButtonFeedback feedback = clickedGO.GetComponent<UIButtonFeedback>();
        if (feedback == null) return;

        if (ColorUtility.TryParseHtmlString(hex, out Color newColor))
        {
            currentColor = newColor;
            currentColor.a = 1f; 

            if (activeColorIndicatorImage != null)
            {
                activeColorIndicatorImage.color = currentColor;
            }

            UpdateGroupUI(colorButtons, feedback);
            
            SaveSprayColor(hex);
            ResetEraserVisualsOnly();
        }
    }

    private void SaveSprayColor(string hexColor)
    {
        PlayerPrefs.SetString("SelectedSprayColor", hexColor);
        PlayerPrefs.Save();

        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            PlayFabAuthBridge.Instance.profile.selectedSprayColorHex = hexColor;
            PlayFabAuthBridge.Instance.SaveProgressToCloud();
        }

        if (DumpsterStoreUI.Instance != null)
        {
            DumpsterStoreUI.Instance.RefreshPersistentDashboard(false);
        }
    }

    // --- 🪟 OVERLAY POPUP TOGGLE ---

    public void ToggleColorPickerOverlay()
    {
        if (colorPickerOverlayPanel != null)
        {
            bool state = !colorPickerOverlayPanel.activeSelf;
            colorPickerOverlayPanel.SetActive(state);
            if (state && colorWheelScript != null) colorWheelScript.RefreshLockState();
        }
    }

    public void CloseColorPickerOverlay()
    {
        if (colorPickerOverlayPanel != null)
        {
            colorPickerOverlayPanel.SetActive(false);
        }
    }

    // --- 🖼️ COSMETIC BORDER DISPLAY ---

    public void RefreshBorderDisplay()
    {
        if (borderDisplayImage == null) return;

        string borderId = "default_border";
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            borderId = PlayFabAuthBridge.Instance.profile.selectedBorderId;
        }

        Sprite borderSprite = null;
        if (PlayFabAuthBridge.Instance != null)
        {
            borderSprite = PlayFabAuthBridge.Instance.GetBorderSpriteById(borderId);
        }

        if (borderSprite != null)
        {
            borderDisplayImage.gameObject.SetActive(true);
            borderDisplayImage.sprite = borderSprite;
            borderDisplayImage.color = Color.white;
        }
        else
        {
            borderDisplayImage.gameObject.SetActive(false);
        }
    }

    void SetDefaults()
    {
        // 🌟 MATCH INITIAL HIGHLIGHT TO THE ACTIVE SAVED COLOR (Defaults to Black)
        string currentHex = "#" + ColorUtility.ToHtmlStringRGB(currentColor);
        UIButtonFeedback defaultColorBtn = null;

        foreach (var btn in colorButtons)
        {
            // Look for a preset button that matches the black hex code or fallback to index 0
            if (btn.name.ToLower().Contains("black") || btn.name.ToLower().Contains("000000"))
            {
                defaultColorBtn = btn;
                break;
            }
        }

        if (defaultColorBtn == null && colorButtons.Count > 0)
        {
            defaultColorBtn = colorButtons[0];
        }

        if (defaultColorBtn != null) 
        {
            UpdateGroupUI(colorButtons, defaultColorBtn);
        }

        if (toolButtons.Count > 0) UpdateToolGroupUI(toolButtons[0]); 
    }

    // --- 🛠️ BRUSH & TOOL SELECTION ---

    public void UI_SelectBrushSize(int size)
    {
        GameObject clickedGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (clickedGO == null) return;

        UIButtonFeedback feedback = clickedGO.GetComponent<UIButtonFeedback>();
        if (feedback == null) return;

        brushSize = size;
        UpdateToolGroupUI(feedback);
    }

    public void UI_SelectEraser()
    {
        GameObject clickedGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (clickedGO == null) return;

        UIButtonFeedback feedback = clickedGO.GetComponent<UIButtonFeedback>();
        if (feedback == null) return;

        currentColor = new Color(0, 0, 0, 0); 
        UpdateToolGroupUI(feedback);

        foreach (var btn in colorButtons)
        {
            btn.Deselect();
            Image img = btn.GetComponent<Image>();
            if (img != null) img.canvasRenderer.SetColor(new Color(0.4f, 0.4f, 0.4f, 1f));
            RectTransform rt = btn.GetComponent<RectTransform>();
            if(originalScales.ContainsKey(rt)) rt.localScale = originalScales[rt];
        }

        if (colorWheelButtonFeedback != null)
        {
            colorWheelButtonFeedback.Deselect();
            Image img = colorWheelButtonFeedback.GetComponent<Image>();
            if (img != null) img.canvasRenderer.SetColor(new Color(0.4f, 0.4f, 0.4f, 1f));
            RectTransform rt = colorWheelButtonFeedback.GetComponent<RectTransform>();
            if(originalScales.ContainsKey(rt)) rt.localScale = originalScales[rt];
        }
    }

    private void UpdateGroupUI(List<UIButtonFeedback> group, UIButtonFeedback active)
    {
        if (group == null || active == null) return;

        List<UIButtonFeedback> allColorButtons = new List<UIButtonFeedback>(group);
        if (colorWheelButtonFeedback != null && !allColorButtons.Contains(colorWheelButtonFeedback))
        {
            allColorButtons.Add(colorWheelButtonFeedback);
        }

        foreach (var btn in allColorButtons)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            Image img = btn.GetComponent<Image>();

            if (btn == active)
            {
                btn.Select();
                StartCoroutine(AnimateButtonPop(rt));
                if (img != null) img.canvasRenderer.SetColor(Color.white);
            }
            else
            {
                btn.Deselect();
                if(originalScales.ContainsKey(rt)) rt.localScale = originalScales[rt];
                if (img != null) img.canvasRenderer.SetColor(new Color(0.6f, 0.6f, 0.6f, 1f));
            }
        }
    }

    private void UpdateToolGroupUI(UIButtonFeedback active)
    {
        bool isEraserClick = active.name.ToLower().Contains("eraser");

        foreach (var btn in toolButtons)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            Image img = btn.GetComponent<Image>();
            bool isThisBtnEraser = btn.name.ToLower().Contains("eraser");

            if (btn == active)
            {
                btn.Select();
                StartCoroutine(AnimateButtonPop(rt));
                if (img != null) img.canvasRenderer.SetColor(Color.white);
            }
            else
            {
                bool bothAreSizes = !isEraserClick && !isThisBtnEraser;
                if (bothAreSizes)
                {
                    btn.Deselect();
                    if(originalScales.ContainsKey(rt)) rt.localScale = originalScales[rt];
                    if (img != null) img.canvasRenderer.SetColor(new Color(0.6f, 0.6f, 0.6f, 1f));
                }
            }
        }
    }

    void ResetEraserVisualsOnly()
    {
        foreach(var btn in toolButtons)
        {
            if(btn.name.ToLower().Contains("eraser"))
            {
                btn.Deselect();
                RectTransform rt = btn.GetComponent<RectTransform>();
                if(originalScales.ContainsKey(rt)) rt.localScale = originalScales[rt];
                Image img = btn.GetComponent<Image>();
                if (img != null) img.canvasRenderer.SetColor(new Color(0.6f, 0.6f, 0.6f, 1f));
            }
        }
    }

    IEnumerator AnimateButtonPop(RectTransform rt)
    {
        float elapsed = 0f;
        Vector3 baseScale = originalScales.ContainsKey(rt) ? originalScales[rt] : Vector3.one;
        Vector3 targetScale = baseScale * selectedScaleMultiplier;

        while(elapsed < animationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationSpeed;
            rt.localScale = Vector3.Lerp(baseScale, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }
        rt.localScale = targetScale;
    }

    void RecordOriginalScales()
    {
        List<UIButtonFeedback> allButtons = new List<UIButtonFeedback>(toolButtons);
        allButtons.AddRange(colorButtons);
        if (colorWheelButtonFeedback != null) allButtons.Add(colorWheelButtonFeedback);

        foreach(var btn in allButtons)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            if(rt != null && !originalScales.ContainsKey(rt)) originalScales.Add(rt, rt.localScale);
        }
    }

    // --- 🖌️ DRAWING PIPELINE ---

	void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (canvasRect == null) return;

            if (colorPickerOverlayPanel != null && colorPickerOverlayPanel.activeSelf) 
            {
                StopSpraySound(); // 🌟 Stop sound if picker is open
                return;
            }

            if (IsPointerBlockedByUI())
            {
                isFirstStroke = true;
                StopSpraySound(); // 🌟 Stop sound if mouse drags over a UI panel
                return;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(canvasRect, Input.mousePosition, null))
            {
                isFirstStroke = true; 
                StopSpraySound(); // 🌟 Stop sound if mouse drags off the canvas
                return;
            }

            // 🌟 WE ARE ACTIVELY DRAWING: Play the sound!
            PlaySpraySound();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out Vector2 localPos);
            float x = (localPos.x / canvasRect.rect.width + 0.5f) * textureSize;
            float y = (localPos.y / canvasRect.rect.height + 0.5f) * textureSize;
            Vector2 currentDrawPos = new Vector2(x, y);

            if (isFirstStroke) 
            { 
                DrawSprayCircle((int)x, (int)y); 
                isFirstStroke = false; 
            }
            else 
            { 
                DrawSmoothLine(lastDrawPos, currentDrawPos); 
            }

            lastDrawPos = currentDrawPos;
            drawableTexture.Apply();
        }
        
        if (Input.GetMouseButtonUp(0)) 
        {
            isFirstStroke = true;
            StopSpraySound(); // 🌟 Stop sound the moment the mouse is released
            AutoSaveCurrentProgressLocally();
        }
    }
	
	private void PlaySpraySound()
    {
        if (sprayAudioSource != null && spraySound != null)
        {
            if (!sprayAudioSource.isPlaying)
            {
                sprayAudioSource.clip = spraySound;
                sprayAudioSource.loop = true; // Ensures the recording loops while holding
                sprayAudioSource.Play();
            }
        }
    }

    private void StopSpraySound()
    {
        if (sprayAudioSource != null && sprayAudioSource.isPlaying)
        {
            sprayAudioSource.Stop();
        }
    }
	
	private bool IsPointerBlockedByUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;

        UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        // Check the very first UI object hit by the mouse
        GameObject topHit = results[0].gameObject;

        // 🌟 FIX: If we hit the drawing canvas OR the cosmetic border frame, it is safe to draw!
        if (topHit == drawingCanvas.gameObject) return false;
        if (borderDisplayImage != null && topHit == borderDisplayImage.gameObject) return false;

        // If it's anything else (like a scroll view or store panel), block the stroke
        return true;
    }

    private void AutoSaveCurrentProgressLocally()
    {
        byte[] textureBytes = GetTextureData();
        if (textureBytes != null)
        {
            string base64String = System.Convert.ToBase64String(textureBytes);
            PlayerPrefs.SetString("SavedAvatar", base64String);
            PlayerPrefs.Save();
        }
    }

    void DrawSmoothLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        float lerpStep = 1f / Mathf.Max(distance, 1.0f); 
        for (float t = 0; t <= 1; t += lerpStep)
        {
            Vector2 pos = Vector2.Lerp(start, end, t);
            DrawSprayCircle((int)pos.x, (int)pos.y);
        }
    }

    void DrawSprayCircle(int x, int y)
    {
        for (int ix = -brushSize; ix < brushSize; ix++)
        {
            for (int iy = -brushSize; iy < brushSize; iy++)
            {
                float distSq = ix * ix + iy * iy;
                if (distSq > brushSize * brushSize) continue;

                int px = x + ix;
                int py = y + iy;

                if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float alpha = Mathf.Clamp01((1.0f - (dist / brushSize)) / spraySoftness);
                    
                    Color existing = drawableTexture.GetPixel(px, py);
                    if(currentColor.a > 0) 
                    {
                        Color pixelColor = currentColor;
                        pixelColor.a = Mathf.Max(existing.a, alpha);
                        drawableTexture.SetPixel(px, py, pixelColor);
                    }
                    else 
                    {
                        existing.a = Mathf.Lerp(existing.a, 0, alpha);
                        drawableTexture.SetPixel(px, py, existing);
                    }
                }
            }
        }
    }

    void InitializeCanvas()
    {
        drawableTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        clearSlate = new Color32[textureSize * textureSize];
        for (int i = 0; i < clearSlate.Length; i++) clearSlate[i] = new Color32(0, 0, 0, 0);

        if (PlayerPrefs.HasKey("SavedAvatar"))
        {
            string base64String = PlayerPrefs.GetString("SavedAvatar");
            byte[] savedData = System.Convert.FromBase64String(base64String);
            drawableTexture.LoadImage(savedData);

            if (drawableTexture.width != textureSize || drawableTexture.height != textureSize)
            {
                drawableTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
                drawableTexture.SetPixels32(clearSlate);
            }
        }
        else
        {
            drawableTexture.SetPixels32(clearSlate);
        }

        drawableTexture.Apply();
        drawingCanvas.texture = drawableTexture;
    }

    public void ClearCanvas() 
    { 
        if (drawableTexture == null) return;
        drawableTexture.SetPixels32(clearSlate); 
        drawableTexture.Apply(); 
    }

    public byte[] GetTextureData() 
    { 
        if (drawableTexture == null) return null;
        drawableTexture.Apply(); 
        return drawableTexture.EncodeToPNG(); 
    }
}