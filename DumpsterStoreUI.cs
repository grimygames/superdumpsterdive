using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DumpsterStoreUI : MonoBehaviour
{
    public static DumpsterStoreUI Instance;

    [System.Serializable]
    public class BorderAssetMapping
    {
        public string borderId;
        public Sprite borderSprite;
        public string borderDisplayName;
        public float purchasePriceCash; 
    }

    [Header("🖼️ Border Asset Library Fallback")]
    public List<BorderAssetMapping> borderLibrary = new List<BorderAssetMapping>();
    public Sprite defaultEmptyBorder;

    [Header("💥 UI Canvas Settings")]
    public CanvasGroup panelCanvasGroup;
    public GameObject parentProfilePanel; 
    public Button logoButton;
    public Button closeButton;

    [Header("🏢 Dynamic Tabs")]
    public GameObject marketTabGrid;    // Recycle Center
    public GameObject shopTabGrid;      // Border Shop
    public GameObject premiumTabGrid;   // Premium Shop (Color Wheel)
    public GameObject wardrobeTabGrid;  // Wardrobe / Inventory

    [Header("🎨 Premium Tab (Color Wheel Store Card)")]
    public Button colorWheelBuyButton;
    public TMP_Text colorWheelPriceText;
    public GameObject colorWheelOwnedIndicator;

    [Header("📊 Persistent Dashboard Displays")]
    public TMP_Text accountTypeField;
    public TMP_Text cashDisplayField; 
    public TMP_Text highestScoreField;
    public TMP_Text aluminumCountField;
    public TMP_Text glassCountField;
    public TMP_Text plasticCountField;
    public TMP_Text otherCountField; 
    public TMP_Text copperCountField;
    public TMP_Text borderIdField;

    [Header("📈 Market Price Displays (Recycle Tab)")]
    public TMP_Text alumPriceText;
    public TMP_Text glassPriceText;
    public TMP_Text plastPriceText;
    public TMP_Text copperPriceText;
    public TMP_Text marketTimerText; 

    [Header("🎨 Cosmetic Customization Slots")]
    public Image avatarDisplayImage;
    public Image borderFrameGraphic;
    public Image sprayCanColorIndicator;

    [Header("🗂️ Wardrobe & Shop Prefabs")]
    public GameObject wardrobeItemPrefab; 
    public Transform wardrobeContentHolder;
    public GameObject shopItemPrefab; 
    public Transform shopContentHolder;

    [Header("🎵 Audio Feedback Triggers")]
    public AudioSource uiAudioSource;
    public AudioClip openSound;
    public AudioClip tickSound;
    public AudioClip cashRegisterSound; // Used for Border purchases
    
    [Header("♻️ Recycle Sell Sounds")]
    public AudioClip sellAluminumSound;
    public AudioClip sellGlassSound;
    public AudioClip sellPlasticSound;
    public AudioClip sellCopperSound;

    private float currentAlumPrice;
    private float currentGlassPrice;
    private float currentPlastPrice;
    private float currentCopperPrice;

    private bool isGamePaused = false;

    void Awake()
    {
        Instance = this;

        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.interactable = false;
    }

    void Start()
    {
        if (logoButton != null) logoButton.onClick.AddListener(TogglePauseStateEngine);
        if (closeButton != null) closeButton.onClick.AddListener(TogglePauseStateEngine);
        if (colorWheelBuyButton != null) colorWheelBuyButton.onClick.AddListener(BuyColorWheelRealMoney);
    }

    void OnEnable()
    {
        if (panelCanvasGroup == null) panelCanvasGroup = GetComponent<CanvasGroup>();

        StopAllCoroutines();
        StartCoroutine(FadePanel(1f)); 

        CalculateHourlyMarketPrices();
        EnsureDefaultBorderUnlocked();
        RefreshPersistentDashboard(false);
        SwitchToRecycleTab(); 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePauseStateEngine();
        }

        if (gameObject.activeSelf)
        {
            UpdateMarketTimerUI();
        }
    }

    private List<BorderAssetMapping> GetActiveBorderLibrary()
    {
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.borderLibrary != null && PlayFabAuthBridge.Instance.borderLibrary.Count > 0)
        {
            return PlayFabAuthBridge.Instance.borderLibrary;
        }
        return borderLibrary;
    }

    private Sprite GetActiveDefaultBorder()
    {
        if (PlayFabAuthBridge.Instance != null)
        {
            return PlayFabAuthBridge.Instance.defaultEmptyBorder;
        }
        return defaultEmptyBorder;
    }

    private void EnsureDefaultBorderUnlocked()
    {
        if (PlayFabAuthBridge.Instance == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;
        if (profile == null) return;

        if (profile.unlockedBorders == null)
        {
            profile.unlockedBorders = new List<string>();
        }

        if (!profile.unlockedBorders.Contains("default_border"))
        {
            profile.unlockedBorders.Add("default_border");
            PlayFabAuthBridge.Instance.SaveProgressToCloud();
        }
    }

    // --- 🛒 PURCHASING SYSTEM ---

    public void BuyBorder(string borderId)
    {
        if (PlayFabAuthBridge.Instance == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;
        if (profile == null) return;

        var activeLib = GetActiveBorderLibrary();
        BorderAssetMapping border = activeLib.Find(b => b.borderId == borderId);
        if (border == null) return;

        if (profile.unlockedBorders.Contains(borderId)) return;

        if (profile.cashBalance >= border.purchasePriceCash)
        {
            profile.cashBalance -= border.purchasePriceCash;
            profile.unlockedBorders.Add(borderId);
            
            PlayFabAuthBridge.Instance.SaveProgressToCloud();
            
            if (uiAudioSource != null && cashRegisterSound != null)
                uiAudioSource.PlayOneShot(cashRegisterSound);

            RefreshPersistentDashboard(false);
            PopulateShopGrid(); 
            Debug.Log($"[STORE] Purchased {border.borderDisplayName} for ${border.purchasePriceCash:F2}!");
        }
    }

    // --- Real-Money Color Wheel Purchase Trigger ---
    public void BuyColorWheelRealMoney()
    {
        if (PlayFabAuthBridge.Instance == null || PlayFabAuthBridge.Instance.profile == null)
        {
            Debug.LogError("[STORE] PlayFab profile not connected. Cannot start purchase.");
            return;
        }

        if (PlayFabAuthBridge.Instance.profile.ownsCustomColorWheel)
        {
            Debug.Log("[STORE] Player already owns the Custom Color Wheel!");
            return;
        }

        string trackingId = !string.IsNullOrEmpty(PlayFabAuthBridge.Instance.MyPlayFabId) 
            ? PlayFabAuthBridge.Instance.MyPlayFabId 
            : SystemInfo.deviceUniqueIdentifier;

        string stripeCheckoutUrl = $"https://buy.stripe.com/test_aFa8wQ2eEe0e7uC2fJ87K00?client_reference_id={trackingId}";
        Application.OpenURL(stripeCheckoutUrl);
        Debug.Log($"[STORE] Opening real-money Checkout for player ID: {trackingId}");
    }

    public void RefreshColorWheelTabState()
    {
        if (PlayFabAuthBridge.Instance == null || PlayFabAuthBridge.Instance.profile == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;

        bool ownsWheel = profile.ownsCustomColorWheel;

        if (colorWheelBuyButton != null) colorWheelBuyButton.gameObject.SetActive(!ownsWheel);
        if (colorWheelOwnedIndicator != null) colorWheelOwnedIndicator.SetActive(ownsWheel);
        if (colorWheelPriceText != null && !ownsWheel) colorWheelPriceText.text = "$1.00";
    }

    // --- 🗂️ GRID RENDERERS ---

    public void PopulateShopGrid()
    {
        if (PlayFabAuthBridge.Instance == null || PlayFabAuthBridge.Instance.profile == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;

        if (shopContentHolder == null || shopItemPrefab == null) return;

        ClearContainer(shopContentHolder);

        var activeLib = GetActiveBorderLibrary();
        int count = 0;

        foreach (var border in activeLib)
        {
            if (border.borderId == "default_border") continue;

            GameObject cardObj = Instantiate(shopItemPrefab, shopContentHolder);
            count++;

            Image itemIcon = null;
            TMP_Text label = null;
            TMP_Text priceLabel = null;
            Button buyButton = null;

            if (cardObj.TryGetComponent(out StoreItemCard card))
            {
                itemIcon = card.itemIcon;
                label = card.itemLabel;
                priceLabel = card.priceLabel;
                buyButton = card.actionButton;
            }
            else
            {
                Transform iconTrans = cardObj.transform.Find("ItemIcon");
                Transform labelTrans = cardObj.transform.Find("ItemLabel");
                Transform priceTrans = cardObj.transform.Find("PriceLabel");

                itemIcon = iconTrans != null ? iconTrans.GetComponent<Image>() : null;
                label = labelTrans != null ? labelTrans.GetComponent<TMP_Text>() : null;
                priceLabel = priceTrans != null ? priceTrans.GetComponent<TMP_Text>() : null;
                buyButton = cardObj.GetComponent<Button>();
            }

            if (itemIcon != null)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.enabled = true;
                itemIcon.sprite = border.borderSprite;
                itemIcon.color = Color.white; 
            }

            if (label != null) label.text = border.borderDisplayName;

            if (profile.unlockedBorders == null) profile.unlockedBorders = new List<string>();

            if (profile.unlockedBorders.Contains(border.borderId))
            {
                if (priceLabel != null) priceLabel.text = "<color=#00FF66>Owned</color>";
                if (buyButton != null) buyButton.interactable = false;
            }
            else
            {
                if (priceLabel != null) priceLabel.text = $"${border.purchasePriceCash:F2}";
                if (buyButton != null)
                {
                    buyButton.interactable = true;
                    buyButton.onClick.RemoveAllListeners();
                    string targetId = border.borderId;
                    buyButton.onClick.AddListener(() => BuyBorder(targetId));
                }
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(shopContentHolder as RectTransform);
    }

    public void PopulateWardrobeGrid()
    {
        if (PlayFabAuthBridge.Instance == null || PlayFabAuthBridge.Instance.profile == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;

        if (wardrobeContentHolder == null || wardrobeItemPrefab == null) return;

        ClearContainer(wardrobeContentHolder);

        var activeLib = GetActiveBorderLibrary();

        foreach (var border in activeLib)
        {
            GameObject cardObj = Instantiate(wardrobeItemPrefab, wardrobeContentHolder);
            
            Image itemIcon = null;
            TMP_Text label = null;
            Button actionButton = null;

            if (cardObj.TryGetComponent(out StoreItemCard card))
            {
                itemIcon = card.itemIcon;
                label = card.itemLabel;
                actionButton = card.actionButton;
            }
            else
            {
                Transform iconTrans = cardObj.transform.Find("ItemIcon");
                Transform labelTrans = cardObj.transform.Find("ItemLabel");

                itemIcon = iconTrans != null ? iconTrans.GetComponent<Image>() : null;
                label = labelTrans != null ? labelTrans.GetComponent<TMP_Text>() : null;
                actionButton = cardObj.GetComponent<Button>();
            }

            if (profile.unlockedBorders == null) profile.unlockedBorders = new List<string>();

            if (profile.unlockedBorders.Contains(border.borderId))
            {
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.enabled = true;
                    itemIcon.sprite = border.borderSprite;
                    itemIcon.color = Color.white;
                }
                if (label != null) label.text = border.borderDisplayName;
                
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                    actionButton.onClick.RemoveAllListeners();
                    string targetId = border.borderId;
                    actionButton.onClick.AddListener(() => EquipBorder(targetId));
                }
            }
            else
            {
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.enabled = true;
                    itemIcon.sprite = GetActiveDefaultBorder();
                    itemIcon.color = new Color(0.15f, 0.15f, 0.15f, 0.6f); 
                }
                if (label != null) label.text = "Locked Border";
                if (actionButton != null) actionButton.interactable = false; 
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(wardrobeContentHolder as RectTransform);
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    public void EquipBorder(string borderId)
    {
        if (PlayFabAuthBridge.Instance == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;
        if (profile == null) return;

        if (profile.unlockedBorders != null && profile.unlockedBorders.Contains(borderId))
        {
            profile.selectedBorderId = borderId;

            PlayerPrefs.SetString("SelectedBorder", borderId);
            PlayerPrefs.Save();

            PlayFabAuthBridge.Instance.SaveProgressToCloud();
            RefreshPersistentDashboard(false);
            
            if (uiAudioSource != null && tickSound != null) 
                uiAudioSource.PlayOneShot(tickSound);
        }
    }

    private void CalculateHourlyMarketPrices()
    {
        DateTime nowUtc = DateTime.UtcNow;
        int currentHourSeed = nowUtc.Year + nowUtc.DayOfYear + nowUtc.Hour;
        
        DateTime prevHourUtc = nowUtc.AddHours(-1);
        int prevHourSeed = prevHourUtc.Year + prevHourUtc.DayOfYear + prevHourUtc.Hour;

        currentAlumPrice = GetSeededPrice(currentHourSeed, 1);
        currentGlassPrice = GetSeededPrice(currentHourSeed, 2);
        currentPlastPrice = GetSeededPrice(currentHourSeed, 3);
        currentCopperPrice = GetSeededPrice(currentHourSeed, 4);

        float prevAlum = GetSeededPrice(prevHourSeed, 1);
        float prevGlass = GetSeededPrice(prevHourSeed, 2);
        float prevPlast = GetSeededPrice(prevHourSeed, 3);
        float prevCopper = GetSeededPrice(prevHourSeed, 4);

        if (alumPriceText != null) alumPriceText.text = $"Aluminum Price: ${currentAlumPrice:F2} " + GetTrendString(currentAlumPrice, prevAlum);
        if (glassPriceText != null) glassPriceText.text = $"Glass Price: ${currentGlassPrice:F2} " + GetTrendString(currentGlassPrice, prevGlass);
        if (plastPriceText != null) plastPriceText.text = $"Plastic Price: ${currentPlastPrice:F2} " + GetTrendString(currentPlastPrice, prevPlast);
        if (copperPriceText != null) copperPriceText.text = $"Copper Price: ${currentCopperPrice:F2} " + GetTrendString(currentCopperPrice, prevCopper);
    }

    private float GetSeededPrice(int baseSeed, int offset)
    {
        UnityEngine.Random.InitState(baseSeed + offset);
        float price = UnityEngine.Random.Range(0.50f, 1.00f);
        return (float)Math.Round(price, 2);
    }

    private string GetTrendString(float current, float previous)
    {
        if (current > previous) return "<color=#00FF66>▲ High</color>";
        if (current < previous) return "<color=#FF3333>▼ Low</color>";
        return "<color=#CCCCCC>▬ Stable</color>";
    }

    private void UpdateMarketTimerUI()
    {
        if (marketTimerText == null) return;
        DateTime nowUtc = DateTime.UtcNow;
        int minutesLeft = 59 - nowUtc.Minute;
        int secondsLeft = 59 - nowUtc.Second;
        marketTimerText.text = $"Next Market Shift In: {minutesLeft:00}:{secondsLeft:00}";

        if (minutesLeft == 0 && secondsLeft == 0)
        {
            CalculateHourlyMarketPrices();
            RefreshPersistentDashboard(false);
        }
    }

    public void SellMaterial(string materialType)
    {
        if (PlayFabAuthBridge.Instance == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;
        if (profile == null) return;

        float earnedAmount = 0f;
        int itemAmount = 0;
        AudioClip soundToPlay = null;

        if (materialType == "Aluminum" && profile.aluminumCount > 0)
        {
            itemAmount = profile.aluminumCount;
            earnedAmount = itemAmount * currentAlumPrice;
            profile.aluminumCount = 0;
            soundToPlay = sellAluminumSound;
        }
        else if (materialType == "Glass" && profile.glassCount > 0)
        {
            itemAmount = profile.glassCount;
            earnedAmount = itemAmount * currentGlassPrice;
            profile.glassCount = 0;
            soundToPlay = sellGlassSound;
        }
        else if (materialType == "Plastic" && profile.plasticCount > 0)
        {
            itemAmount = profile.plasticCount;
            earnedAmount = itemAmount * currentPlastPrice;
            profile.plasticCount = 0;
            soundToPlay = sellPlasticSound;
        }
        else if (materialType == "Copper" && profile.copperCount > 0) 
        {
            itemAmount = profile.copperCount;                       
            earnedAmount = itemAmount * currentCopperPrice;
            profile.copperCount = 0;                                 
            soundToPlay = sellCopperSound;
        }

        if (earnedAmount > 0)
        {
            profile.cashBalance += earnedAmount;
            PlayFabAuthBridge.Instance.SaveProgressToCloud();
            
            if (uiAudioSource != null && soundToPlay != null) 
                uiAudioSource.PlayOneShot(soundToPlay);

            RefreshPersistentDashboard(false);
        }
    }

    public void RefreshPersistentDashboard(bool useJuice)
    {
        if (PlayFabAuthBridge.Instance == null) return;
        PlayerProfileData profile = PlayFabAuthBridge.Instance.profile;
        if (profile == null) return;

        string activeBorderId = profile.selectedBorderId;
        if (string.IsNullOrEmpty(activeBorderId) || activeBorderId == "default_border")
        {
            activeBorderId = PlayerPrefs.GetString("SelectedBorder", "default_border");
            profile.selectedBorderId = activeBorderId;
        }

        if (accountTypeField != null)
            accountTypeField.text = profile.isGuestAccount ? "<color=#FFCC00>GUEST PROFILE</color>" : "<color=#00FF66>VERIFIED PROFILE</color>";

        if (borderIdField != null) 
            borderIdField.text = "Border: " + activeBorderId;

        if (cashDisplayField != null)
            cashDisplayField.text = $"Cash: <color=#00FF66>${profile.cashBalance:F2}</color>";

        if (useJuice)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateTextCounter(highestScoreField, 0, profile.highestScore, "HIGH SCORE: "));
            StartCoroutine(AnimateTextCounter(aluminumCountField, 0, profile.aluminumCount, "ALUMINUM: "));
            StartCoroutine(AnimateTextCounter(glassCountField, 0, profile.glassCount, "GLASS: "));
            StartCoroutine(AnimateTextCounter(plasticCountField, 0, profile.plasticCount, "PLASTIC: "));
            StartCoroutine(AnimateTextCounter(copperCountField, 0, profile.copperCount, "COPPER: "));
        }
        else
        {
            if (highestScoreField != null) highestScoreField.text = "HIGH SCORE: " + profile.highestScore;
            if (aluminumCountField != null) aluminumCountField.text = "" + profile.aluminumCount;
            if (glassCountField != null) glassCountField.text = "" + profile.glassCount;
            if (plasticCountField != null) plasticCountField.text = "" + profile.plasticCount;
            if (copperCountField != null) copperCountField.text = "" + profile.copperCount;
        }

        Sprite selectedSprite = FindBorderSpriteById(activeBorderId);
        Sprite defaultSprite = GetActiveDefaultBorder();

        if (borderFrameGraphic != null)
        {
            if (selectedSprite != null)
            {
                borderFrameGraphic.gameObject.SetActive(true);
                borderFrameGraphic.enabled = true;
                borderFrameGraphic.sprite = selectedSprite;
                borderFrameGraphic.color = Color.white;
            }
            else if (defaultSprite != null)
            {
                borderFrameGraphic.gameObject.SetActive(true);
                borderFrameGraphic.enabled = true;
                borderFrameGraphic.sprite = defaultSprite;
                borderFrameGraphic.color = Color.white;
            }
            else
            {
                borderFrameGraphic.gameObject.SetActive(false);
            }
        }

        if (ColorUtility.TryParseHtmlString(profile.selectedSprayColorHex, out Color activeSprayColor))
        {
            if (sprayCanColorIndicator != null) sprayCanColorIndicator.color = activeSprayColor;
        }

        string avatarBase64 = !string.IsNullOrEmpty(profile.activeAvatarBase64) 
            ? profile.activeAvatarBase64 
            : PlayerPrefs.GetString("SavedAvatar", "");

        if (!string.IsNullOrEmpty(avatarBase64) && avatarDisplayImage != null)
        {
            try
            {
                byte[] imageBytes = System.Convert.FromBase64String(avatarBase64);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(imageBytes))
                {
                    avatarDisplayImage.gameObject.SetActive(true);
                    avatarDisplayImage.enabled = true;
                    avatarDisplayImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    avatarDisplayImage.color = Color.white;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DumpsterStoreUI] Failed to decode active avatar: {ex.Message}");
            }
        }

        RefreshColorWheelTabState();
    }

    private Sprite FindBorderSpriteById(string id)
    {
        if (PlayFabAuthBridge.Instance != null)
        {
            return PlayFabAuthBridge.Instance.GetBorderSpriteById(id);
        }

        if (string.IsNullOrEmpty(id)) return null;
        foreach (var mapping in borderLibrary)
        {
            if (mapping.borderId == id) return mapping.borderSprite;
        }
        return null;
    }

    private void EnsureParentsActive()
    {
        if (parentProfilePanel != null)
        {
            parentProfilePanel.SetActive(true);
        }

        Transform current = transform;
        while (current != null && current.GetComponent<Canvas>() == null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    public void OpenStoreToTab(string tabName)
    {
        EnsureParentsActive();

        gameObject.SetActive(true);

        if (panelCanvasGroup == null) panelCanvasGroup = GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.blocksRaycasts = true;
        panelCanvasGroup.interactable = true;

        StopAllCoroutines();
        StartCoroutine(FadePanel(1f));

        switch (tabName.ToLower())
        {
            case "premium":
                SwitchToPremiumTab();
                break;
            case "shop":
                SwitchToShopTab();
                break;
            case "wardrobe":
                SwitchToWardrobeTab();
                break;
            default:
                SwitchToRecycleTab();
                break;
        }

        RefreshPersistentDashboard(false);
    }

    public void TogglePauseStateEngine()
    {
        EnsureParentsActive();

        bool isInGameplay = (DumpsterManager.Instance != null);

        if (isInGameplay)
        {
            isGamePaused = !isGamePaused;

            if (isGamePaused)
            {
                StopAllCoroutines();
                gameObject.SetActive(true);

                if (uiAudioSource != null && openSound != null) uiAudioSource.PlayOneShot(openSound);

                StartCoroutine(FadePanel(1f));
                RefreshPersistentDashboard(true); 
                StartCoroutine(DelayTimePause(true));
            }
            else
            {
                StopAllCoroutines();
                Time.timeScale = 1f;
                StartCoroutine(FadePanel(0f));
                StartCoroutine(DelayTimePause(false));
            }
        }
        else
        {
            StopAllCoroutines();
            if (gameObject.activeSelf && panelCanvasGroup.alpha > 0.5f)
            {
                StartCoroutine(FadePanel(0f)); 
            }
            else
            {
                gameObject.SetActive(true);
                if (uiAudioSource != null && openSound != null) uiAudioSource.PlayOneShot(openSound);
                StartCoroutine(FadePanel(1f));
                RefreshPersistentDashboard(false);
            }
        }
    }

    private void SafeSetActive(GameObject targetObject, bool state)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(state);
        }
    }

    public void SwitchToRecycleTab() 
    {
        SafeSetActive(marketTabGrid, true); 
        SafeSetActive(shopTabGrid, false); 
        SafeSetActive(premiumTabGrid, false); 
        SafeSetActive(wardrobeTabGrid, false);
    }
    public void SwitchToShopTab()
    {
        SafeSetActive(marketTabGrid, false); 
        SafeSetActive(shopTabGrid, true); 
        SafeSetActive(premiumTabGrid, false); 
        SafeSetActive(wardrobeTabGrid, false);
        PopulateShopGrid(); 
    }
    public void SwitchToPremiumTab()
    {
        SafeSetActive(marketTabGrid, false); 
        SafeSetActive(shopTabGrid, false); 
        SafeSetActive(premiumTabGrid, true); 
        SafeSetActive(wardrobeTabGrid, false);
        RefreshColorWheelTabState();
    }
    public void SwitchToWardrobeTab()
    {
        SafeSetActive(marketTabGrid, false); 
        SafeSetActive(shopTabGrid, false); 
        SafeSetActive(premiumTabGrid, false); 
        SafeSetActive(wardrobeTabGrid, true);
        PopulateWardrobeGrid(); 
    }

    private IEnumerator AnimateTextCounter(TMP_Text targetField, int startVal, int endVal, string prefixLabel)
    {
        if (targetField == null) yield break;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);
            int currentCount = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, percent));
            targetField.text = prefixLabel + currentCount.ToString();

            if (uiAudioSource != null && tickSound != null && currentCount % 2 == 0)
                uiAudioSource.PlayOneShot(tickSound, 0.2f);

            yield return null;
        }
        targetField.text = prefixLabel + endVal.ToString();
    }

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
        if (targetAlpha == 0f) gameObject.SetActive(false);
    }

    private IEnumerator DelayTimePause(bool shouldPause)
    {
        yield return new WaitForEndOfFrame();
        if (shouldPause && isGamePaused)
        {
            Time.timeScale = 0f;
        }
    }
}