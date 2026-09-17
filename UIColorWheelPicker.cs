using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIColorWheelPicker : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    [Header("🎯 Target Painter")]
    public AvatarPainter targetPainter;

    [Header("🔒 Unlock Overlay")]
    public GameObject lockOverlayPanel; // The lock icon image/panel

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 🌟 Force check on frame one
        RefreshLockState();
    }

    void OnEnable()
    {
        // 🌟 Force check whenever the panel opens
        RefreshLockState();
    }

    /// <summary>
    /// Checks local disk cache AND PlayFab profile to instantly hide the lock overlay if owned.
    /// </summary>
    public void RefreshLockState()
    {
        bool ownsWheel = false;

        // 🌟 1. Check local disk cache for instant Frame-1 load
        if (PlayerPrefs.GetInt("OwnsColorWheel", 0) == 1)
        {
            ownsWheel = true;
        }

        // 🌟 2. Check PlayFab cloud profile
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            if (PlayFabAuthBridge.Instance.profile.ownsCustomColorWheel)
            {
                ownsWheel = true;
                PlayerPrefs.SetInt("OwnsColorWheel", 1);
                PlayerPrefs.Save();
            }
        }

        // 🌟 3. Turn off lock overlay if owned, turn on if locked
        if (lockOverlayPanel != null)
        {
            lockOverlayPanel.SetActive(!ownsWheel);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ProcessColorPick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ProcessColorPick(eventData);
    }

    private void ProcessColorPick(PointerEventData eventData)
    {
        bool ownsWheel = (PlayerPrefs.GetInt("OwnsColorWheel", 0) == 1);

        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            if (PlayFabAuthBridge.Instance.profile.ownsCustomColorWheel)
            {
                ownsWheel = true;
            }
        }

        // 🔒 REDIRECT TO STORE IF LOCKED
        if (!ownsWheel)
        {
            Debug.Log("[ColorWheel] Locked! Opening Store Premium tab...");
            
            DumpsterStoreUI store = DumpsterStoreUI.Instance;
            if (store == null) store = FindObjectOfType<DumpsterStoreUI>(true);

            if (store != null)
            {
                store.OpenStoreToTab("premium");
            }
            return;
        }

        // Sampling color logic from wheel click/drag
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint))
        {
            float maxRadius = rectTransform.rect.width / 2f;
            float currentDist = localPoint.magnitude;

            if (currentDist > maxRadius)
            {
                localPoint = localPoint.normalized * maxRadius;
                currentDist = maxRadius;
            }

            float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            float hue = angle / 360f;
            float saturation = Mathf.Clamp01(currentDist / maxRadius);

            Color selectedColor = Color.HSVToRGB(hue, saturation, 1f);

            if (targetPainter != null)
            {
                targetPainter.SetCustomColor(selectedColor);
            }
        }
    }
}