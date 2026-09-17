using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class PostRoundUIController : MonoBehaviour
{
    [Header("Scene Routing Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Lobby"; 

    private Canvas menuCanvas;

    void Awake()
    {
        menuCanvas = GetComponent<Canvas>();
    }

    void OnEnable()
    {
        // FORCE LAYER SUPERIORITY: Detach this canvas from the background dumpster matrix
        if (menuCanvas != null)
        {
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // Forces it to the absolute front of the camera viewport
            menuCanvas.overrideSorting = true;
            menuCanvas.sortingOrder = 999; // Placed at the highest possible sorting array position
        }

        // Ensure button elements immediately intercept mouse clicks over the falling garbage background
        if (TryGetComponent(out GraphicRaycaster raycaster))
        {
            raycaster.enabled = true;
        }
    }

    public void OnPlayAgainClicked()
    {
        Time.timeScale = 1f;

        if (Launcher.IsTimeAttack)
        {
            // 🌟 SINGLETON SAFETY GUARD:
            // Check if the loading screen actually exists. If it does, use it!
            if (GlobalLoadingScreen.Instance != null)
            {
                GlobalLoadingScreen.Instance.LoadScene(SceneManager.GetActiveScene().name);
            }
            // Otherwise, reload the current scene natively so your local testing doesn't crash.
            else
            {
                Debug.LogWarning("PostRoundUIController: GlobalLoadingScreen.Instance is missing on Play Again! Falling back to standard SceneManager.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            ReturnToLobbyPipeline();
        }
    }

    public void OnLeaveClicked()
    {
        Time.timeScale = 1f;
        ReturnToLobbyPipeline();
    }

    private void ReturnToLobbyPipeline()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // 🌟 SINGLETON SAFETY GUARD:
            if (GlobalLoadingScreen.Instance != null)
            {
                GlobalLoadingScreen.Instance.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogWarning("PostRoundUIController: GlobalLoadingScreen.Instance is missing on Leave! Falling back to standard SceneManager.");
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }
}