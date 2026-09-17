using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Photon.Pun;

public class NetworkedLeaveManager : MonoBehaviour, IPointerClickHandler
{
    private GameSync gameSync;

    [Header("UI Component Routing")]
    [Tooltip("The 'Are you sure?' Yeah/Nah Panel.")]
    public GameObject confirmationPanel;

    [Header("Scene Settings")]
    public string lobbySceneName = "MainMenu";

    void Start()
    {
        gameSync = FindObjectOfType<GameSync>();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RequestPauseAndLeave();
    }

    public void RequestPauseAndLeave()
    {
        if (!Launcher.IsTimeAttack && PhotonNetwork.InRoom)
        {
            // MULTIPLAYER: Pass the request to the network hub
            if (gameSync != null)
            {
                gameSync.RequestPause(true);
            }
        }
        else
        {
            // SOLO: Pause locally
            Time.timeScale = 0f;
            if (confirmationPanel != null) confirmationPanel.SetActive(true);
        }
    }

    public void CancelLeave()
    {
        if (!Launcher.IsTimeAttack && PhotonNetwork.InRoom)
        {
            // MULTIPLAYER: Pass the unpause request to the network hub
            if (gameSync != null) gameSync.RequestPause(false);
        }
        else
        {
            // SOLO: Unpause locally
            Time.timeScale = 1f;
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }
    }

	public void ConfirmLeave()
		{
			Time.timeScale = 1f; // Resets time logic safely
			if (confirmationPanel != null) confirmationPanel.SetActive(false);

			if (!Launcher.IsTimeAttack && PhotonNetwork.InRoom)
			{
				PhotonNetwork.LeaveRoom(); // Will automatically strip objects and exit the loop cleanly
			}
			else
			{
				// --- THE FIX IS HERE ---
				if (GlobalLoadingScreen.Instance != null)
				{
					GlobalLoadingScreen.Instance.LoadScene(lobbySceneName);
				}
				else
				{
					Debug.LogWarning("NetworkedLeaveManager: No GlobalLoadingScreen found! Loading normally.");
					SceneManager.LoadScene(lobbySceneName);
				}
			}
		}
}