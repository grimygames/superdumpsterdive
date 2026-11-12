using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MusicToggleButton : MonoBehaviour
{
    [Header("Visual Swap (Optional)")]
    public Image iconImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnToggleClicked);
        
        // Slight delay ensures the BGMManager has fully initialized first
        Invoke("RefreshVisuals", 0.1f);
    }

    void OnEnable()
    {
        RefreshVisuals();
    }

    private void OnToggleClicked()
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.ToggleMusic();
            RefreshVisuals(); // Update this button
            
            // Find and update any other music buttons currently open on screen
            MusicToggleButton[] allToggles = FindObjectsOfType<MusicToggleButton>();
            foreach (var toggle in allToggles) toggle.RefreshVisuals();
        }
    }

    public void RefreshVisuals()
    {
        if (BGMManager.Instance == null) return;

        bool isEnabled = BGMManager.Instance.IsMusicEnabled();
        
        if (iconImage != null && musicOnSprite != null && musicOffSprite != null)
        {
            iconImage.sprite = isEnabled ? musicOnSprite : musicOffSprite;
        }
        else if (iconImage != null)
        {
            // Fallback: If you don't have separate sprites, just dim the button to gray when off
            iconImage.color = isEnabled ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
}