using UnityEngine;
using TMPro;
using System.Collections;

public class GameFeed : MonoBehaviour
{
    public static GameFeed Instance;
    
    [Header("UI Reference")]
    public TMP_Text feedText;
    public float displayDuration = 2.5f;

    void Awake()
    {
        Instance = this;
        if (feedText != null) feedText.text = ""; // Clear on start
    }

    public void LogMessage(string message, Color? colorOverride = null)
    {
        if (feedText == null) return;
        
        feedText.text = message;
        feedText.color = colorOverride ?? Color.white;
        
        StopAllCoroutines();
        StartCoroutine(FadeOutText());
    }

    IEnumerator FadeOutText()
    {
        feedText.canvasRenderer.SetAlpha(1f);
        yield return new WaitForSeconds(displayDuration);
        feedText.CrossFadeAlpha(0f, 0.5f, false);
    }
}