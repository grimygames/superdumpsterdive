using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Assuming you use TextMeshPro
using System.Collections;

public class GlobalLoadingScreen : MonoBehaviour
{
    public static GlobalLoadingScreen Instance;

    [Header("UI References")]
    public CanvasGroup loadingCanvasGroup;
    public TextMeshProUGUI funnyText;

    [Header("Funny Terms")]
    private string[] loadingPhrases = { 
        "Rummaging", 
        "Scurrying", 
        "Inspecting Garbage", 
        "Rolling in Filth",
        "Digging Deep",
		"Loading"
    };

    private bool isLoading;

    void Awake()
    {
        // This makes the loading screen survive scene changes!
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from ANY script using: GlobalLoadingScreen.Instance.ShowLoadingScreen("GameScene");
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;
        
        // Pick a random funny phrase
        string chosenPhrase = loadingPhrases[Random.Range(0, loadingPhrases.Length)];
        
        // Fade In
        loadingCanvasGroup.alpha = 1f;
        loadingCanvasGroup.blocksRaycasts = true;

        // Start animating the "..." text
        Coroutine textAnim = StartCoroutine(AnimateText(chosenPhrase));

        // Start loading the actual scene in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until Unity finishes loading the scene
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Add a tiny delay so the player actually sees the roach for a second
        yield return new WaitForSeconds(0.5f);

        // Stop the text animation
        StopCoroutine(textAnim);
        isLoading = false;

        // Fade out smoothly
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }
        loadingCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator AnimateText(string baseText)
    {
        int dotCount = 0;
        while (isLoading)
        {
            // Cycles through baseText, baseText., baseText.., baseText...
            funnyText.text = baseText + new string('.', dotCount);
            dotCount = (dotCount + 1) % 4; 
            yield return new WaitForSeconds(0.4f);
        }
    }
	
	// --- MULTIPLAYER MANUAL CONTROLS ---

    public void ShowManualLoadingScreen()
    {
        isLoading = true;
        string chosenPhrase = loadingPhrases[Random.Range(0, loadingPhrases.Length)];
        
        loadingCanvasGroup.alpha = 1f;
        loadingCanvasGroup.blocksRaycasts = true;
        
        StartCoroutine(AnimateText(chosenPhrase));
    }

    public void HideManualLoadingScreen()
    {
        isLoading = false;
        StartCoroutine(FadeOutManual());
    }

    private IEnumerator FadeOutManual()
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }
        loadingCanvasGroup.blocksRaycasts = false;
    }
}