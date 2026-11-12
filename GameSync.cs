using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 
using System.Collections.Generic; 
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameSync : MonoBehaviourPunCallbacks
{
    [Header("Multiplayer UI")]
    public Slider tugOfWarSlider;
    public GameObject disconnectScreen;
    public GameObject confirmationPanel;   
    public GameObject opponentPausedPanel;

    [Header("End Game UI Panels")]
    public GameObject winCanvas;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public GameObject tiePanel;

    [Header("Target Icon Displays")]
    public Image myTargetDisplay;       
    public Image opponentTargetDisplay; 
    public GameObject opponentTalkBubblePanel; 

    [Header("Avatar & Border Displays")]
    public RawImage myAvatarDisplay;
    public Image myBorderDisplay;             
    public RawImage opponentAvatarDisplay;
    public Image opponentBorderDisplay;       

    [Header("Power-Up References")]
    public DiaperScrub diaperScrubScript;

    [Header("Scene Settings")]
    public string lobbySceneName = "MainMenu"; 
    public float returnToLobbyDelay = 5f; 

    [Header("Walkie-Talkie Fade Settings")]
    public float talkBubbleFadeSpeed = 4f; 

    private Sprite cachedOpponentSprite;
    private Coroutine activeRevealCoroutine;

    // Simultaneous Pause State Tracking
    private bool isLocalPlayerPaused = false;
    private bool isOpponentPaused = false;

    void Start()
    {
        if (tugOfWarSlider != null) tugOfWarSlider.value = 0.5f;
        if (disconnectScreen != null) disconnectScreen.SetActive(false);
        
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (tiePanel != null) tiePanel.SetActive(false);

        if (opponentTalkBubblePanel != null) opponentTalkBubblePanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (opponentPausedPanel != null) opponentPausedPanel.SetActive(false);

        LoadLocalAvatarSolo();

        if (!Launcher.IsTimeAttack && PhotonNetwork.InRoom)
        {
            Invoke("LoadInitialNetworkStates", 0.5f);
        }
    }

    public void UpdateLocalTargetUI(TrashData targetData)
    {
        if (targetData != null && myTargetDisplay != null)
        {
            myTargetDisplay.gameObject.SetActive(true);
            myTargetDisplay.sprite = targetData.itemSprite;
            myTargetDisplay.color = Color.white;
        }
    }

    void LoadInitialNetworkStates()
    {
        LoadAvatars();
        
        if (PhotonNetwork.PlayerList != null)
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                RefreshPlayerTargetIcon(p);
            }
        }
    }

    public void ReturnToLobbyFromDisconnect()
    {
        if (disconnectScreen != null) disconnectScreen.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); 
        }
        else
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    void LoadLocalAvatarSolo() 
    {
        if (myAvatarDisplay != null)
        {
            myAvatarDisplay.gameObject.SetActive(true);

            if (PlayerPrefs.HasKey("SavedAvatar"))
            {
                string base64String = PlayerPrefs.GetString("SavedAvatar");
                byte[] data = System.Convert.FromBase64String(base64String);
                
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    myAvatarDisplay.texture = tex;
                    myAvatarDisplay.color = Color.white;
                }
            }
        }

        RefreshLocalBorderDisplay();
    }

    private void RefreshLocalBorderDisplay()
    {
        if (myBorderDisplay == null) return;

        string borderId = "default_border";
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            borderId = PlayFabAuthBridge.Instance.profile.selectedBorderId;
        }

        Sprite borderSprite = GetBorderSpriteById(borderId);
        
        if (borderSprite == null && PlayFabAuthBridge.Instance != null)
        {
            borderSprite = PlayFabAuthBridge.Instance.defaultEmptyBorder;
        }

        if (borderSprite != null)
        {
            myBorderDisplay.gameObject.SetActive(true);
            myBorderDisplay.enabled = true;
            myBorderDisplay.sprite = borderSprite;
            myBorderDisplay.color = Color.white;
        }
        else
        {
            myBorderDisplay.gameObject.SetActive(false);
        }
    }

    public void LoadAvatars()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList == null) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.IsLocal)
            {
                LoadLocalAvatarSolo();
            }
            else
            {
                if (p.CustomProperties.TryGetValue("Avatar", out object data) && data != null)
                {
                    try 
                    {
                        string base64 = (string)data;
                        if (!string.IsNullOrEmpty(base64))
                        {
                            byte[] byteStream = System.Convert.FromBase64String(base64);
                            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(byteStream) && opponentAvatarDisplay != null)
                            {
                                opponentAvatarDisplay.gameObject.SetActive(true);
                                opponentAvatarDisplay.texture = tex;
                                opponentAvatarDisplay.color = Color.white;
                            }
                        }
                    } 
                    catch (System.Exception e) 
                    {
                        Debug.LogWarning("Network Icon translation failed: " + e.Message);
                    }
                }

                if (opponentBorderDisplay != null)
                {
                    string oppBorderId = "default_border";
                    if (p.CustomProperties.TryGetValue("Border", out object borderObj) && borderObj != null)
                    {
                        oppBorderId = borderObj.ToString();
                    }

                    Sprite borderSprite = GetBorderSpriteById(oppBorderId);
                    
                    if (borderSprite == null && PlayFabAuthBridge.Instance != null)
                    {
                        borderSprite = PlayFabAuthBridge.Instance.defaultEmptyBorder;
                    }

                    if (borderSprite != null)
                    {
                        opponentBorderDisplay.gameObject.SetActive(true);
                        opponentBorderDisplay.enabled = true;
                        opponentBorderDisplay.sprite = borderSprite;
                        opponentBorderDisplay.color = Color.white;
                    }
                    else
                    {
                        opponentBorderDisplay.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private Sprite GetBorderSpriteById(string borderId)
    {
        if (PlayFabAuthBridge.Instance != null)
        {
            return PlayFabAuthBridge.Instance.GetBorderSpriteById(borderId);
        }
        return null;
    }

    public void ItemFound() {
        if (PhotonNetwork.InRoom) photonView.RPC("RPC_UpdateBar", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, false);
    }

    public void ItemMissed() {
        if (PhotonNetwork.InRoom) photonView.RPC("RPC_UpdateBar", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, true);
    }

    public void ReceiveDiaper() {
        if (PhotonNetwork.InRoom) photonView.RPC("RPC_ApplyDiaperEffect", RpcTarget.Others);
    }

    [PunRPC]
    void RPC_UpdateBar(int playerID, bool isMiss)
    {
        float change = isMiss ? 0.02f : -0.05f;
        if (playerID != PhotonNetwork.LocalPlayer.ActorNumber) change *= -1f;

        if (tugOfWarSlider != null)
        {
            tugOfWarSlider.value = Mathf.Clamp01(tugOfWarSlider.value + change);
            
            if (PhotonNetwork.IsMasterClient && (tugOfWarSlider.value <= 0.001f || tugOfWarSlider.value >= 0.999f))
            {
                photonView.RPC("RPC_DetermineWinner", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_ApplyDiaperEffect() 
    {
        // 🌟 PLAY THE DIAPER SOUND FOR THE VICTIM
        if (DumpsterManager.Instance != null)
        {
            TrashData diaperData = DumpsterManager.Instance.trashLibrary.Find(t => t.type == TrashData.ItemType.Diaper);
            if (diaperData != null && diaperData.specialActionSound != null)
            {
                DumpsterManager.Instance.PlaySound2D(diaperData.specialActionSound);
            }
        }

        if (diaperScrubScript != null) diaperScrubScript.SmearScreen();
    }

    void RefreshPlayerTargetIcon(Player targetPlayer)
    {
        if (targetPlayer.CustomProperties.TryGetValue("TargetIndex", out object indexValue))
        {
            int index = (int)indexValue;
            DumpsterManager dm = FindObjectOfType<DumpsterManager>();
            
            if (dm != null && dm.trashLibrary != null && index >= 0 && index < dm.trashLibrary.Count)
            {
                Sprite itemSprite = dm.trashLibrary[index].itemSprite;

                if (targetPlayer.IsLocal)
                {
                    if (myTargetDisplay != null)
                    {
                        myTargetDisplay.gameObject.SetActive(true);
                        myTargetDisplay.sprite = itemSprite;
                        myTargetDisplay.color = Color.white;
                    }
                }
                else
                {
                    cachedOpponentSprite = itemSprite;
                    if (opponentTargetDisplay != null) opponentTargetDisplay.sprite = itemSprite;
                    
                    if (opponentTalkBubblePanel != null && opponentTalkBubblePanel.activeSelf && opponentTargetDisplay != null)
                    {
                        opponentTargetDisplay.color = Color.white;
                    }
                }
            }
        }
    }

    public void SendFeedEvent(string message)
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_ReceiveFeedEvent", RpcTarget.Others, message);
        }
    }

    [PunRPC]
    void RPC_ReceiveFeedEvent(string message)
    {
        if (GameFeed.Instance != null)
        {
            GameFeed.Instance.LogMessage(message, Color.yellow);
        }
    }
    
    // --- SIMULTANEOUS PAUSE SYSTEM FIX ---

    public void RequestPause(bool isPaused)
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_SetPauseState", RpcTarget.All, isPaused, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    void RPC_SetPauseState(bool isPaused, int initiatorActorNumber)
    {
        Time.timeScale = 1f; // Ensure real-time networking remains unblocked

        bool isInitiatorLocal = (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == initiatorActorNumber);

        if (isInitiatorLocal)
        {
            isLocalPlayerPaused = isPaused;
        }
        else
        {
            isOpponentPaused = isPaused;
        }

        // Apply distinct panel states based on tracked variables
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(isLocalPlayerPaused);
        }

        if (opponentPausedPanel != null)
        {
            opponentPausedPanel.SetActive(isOpponentPaused);
        }
    }

    public void RevealOpponentTarget(float duration)
    {
        if (opponentTalkBubblePanel != null && cachedOpponentSprite != null)
        {
            if (activeRevealCoroutine != null) StopCoroutine(activeRevealCoroutine);
            activeRevealCoroutine = StartCoroutine(OpponentRevealRoutine(duration));
        }
    }

    private IEnumerator OpponentRevealRoutine(float duration)
    {
        opponentTalkBubblePanel.SetActive(true);
        CanvasGroup cg = opponentTalkBubblePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = opponentTalkBubblePanel.AddComponent<CanvasGroup>();

        if (opponentTargetDisplay != null)
        {
            opponentTargetDisplay.sprite = cachedOpponentSprite;
            opponentTargetDisplay.color = Color.white;
        }

        while (cg.alpha < 1f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 1f, talkBubbleFadeSpeed * Time.deltaTime);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSeconds(duration);

        while (cg.alpha > 0f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 0f, talkBubbleFadeSpeed * Time.deltaTime);
            yield return null;
        }
        cg.alpha = 0f;

        opponentTalkBubblePanel.SetActive(false);
        activeRevealCoroutine = null;
    }

    public void ThrowRegularTrash(TrashData data)
    {
        if (PhotonNetwork.InRoom && DumpsterManager.Instance != null)
        {
            int index = DumpsterManager.Instance.trashLibrary.IndexOf(data);
            if (index != -1)
            {
                photonView.RPC("RPC_ReceiveFlickedTrash", RpcTarget.Others, index);
            }
        }
    }

    [PunRPC]
    void RPC_ReceiveFlickedTrash(int trashLibraryIndex)
    {
        if (DumpsterManager.Instance != null && trashLibraryIndex >= 0 && trashLibraryIndex < DumpsterManager.Instance.trashLibrary.Count)
        {
            TrashData exactDataReceived = DumpsterManager.Instance.trashLibrary[trashLibraryIndex];
            DumpsterManager.Instance.SpawnItemUI(exactDataReceived, false, Vector2.zero);
            
            Transform spawnArea = DumpsterManager.Instance.spawnArea;
            if (spawnArea.childCount > 0)
            {
                GameObject newTrash = spawnArea.GetChild(spawnArea.childCount - 1).gameObject;
                if (DumpsterJuiceFX.Instance != null)
                {
                    DumpsterJuiceFX.Instance.PlayIncomingTrashFlyIn(newTrash);
                }
            }
        }
    }

    public void DropJunkOnOpponent(TrashData junkData)
    {
        if (PhotonNetwork.InRoom && DumpsterManager.Instance != null)
        {
            int junkIndex = DumpsterManager.Instance.trashLibrary.IndexOf(junkData);
            if (junkIndex != -1)
            {
                photonView.RPC("RPC_ReceiveJunkDrop", RpcTarget.Others, junkIndex);
            }
        }
    }

	[PunRPC]
    void RPC_ReceiveJunkDrop(int libraryIndex)
    {
        if (DumpsterManager.Instance != null && libraryIndex >= 0 && libraryIndex < DumpsterManager.Instance.trashLibrary.Count)
        {
            TrashData incomingJunk = DumpsterManager.Instance.trashLibrary[libraryIndex];

            // 🌟 PLAY THE IMPACT SOUND FOR THE VICTIM
            if (incomingJunk != null && incomingJunk.specialActionSound != null)
            {
                DumpsterManager.Instance.PlaySound2D(incomingJunk.specialActionSound);
            }
            else if (incomingJunk != null && incomingJunk.dropSound != null)
            {
                // Fallback in case a standard piece of junk is thrown without a special action sound
                DumpsterManager.Instance.PlaySound2D(incomingJunk.dropSound);
            }

            DumpsterManager.Instance.SpawnItemUI(incomingJunk, false, Vector2.zero);

            Transform spawnArea = DumpsterManager.Instance.spawnArea;
            if (spawnArea.childCount > 0)
            {
                GameObject newTrash = spawnArea.GetChild(spawnArea.childCount - 1).gameObject;
                if (DumpsterJuiceFX.Instance != null)
                {
                    DumpsterJuiceFX.Instance.PlayIncomingTrashFlyIn(newTrash);
                }
            }
        }
    }

    public void LaunchVibratorSabotage()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_ReceiveVibratorSabotage", RpcTarget.Others);
        }
    }

    [PunRPC]
    void RPC_ReceiveVibratorSabotage()
    {
        if (DumpsterManager.Instance != null)
        {
            TrashData vibratorData = DumpsterManager.Instance.trashLibrary.Find(t => t.type == TrashData.ItemType.Vibrator);
            
            if (vibratorData != null && DumpsterJuiceFX.Instance != null)
            {
                DumpsterJuiceFX.Instance.PlayVibratorSlam(vibratorData.itemSprite);
            }
        }
    }

    [PunRPC]
    public void RPC_ApplyRaccoonAttack()
    {
        MultiplayerDumpsterMode multiMode = FindObjectOfType<MultiplayerDumpsterMode>();
        if (multiMode != null)
        {
            multiMode.ExecuteNetworkRaccoonAttackStrike();
        }
    }

    [PunRPC]
    public void RPC_DetermineWinner()
    {
        DetermineWinner();
    }

    public void DetermineWinner()
    {
        float matchStart = (DumpsterManager.Instance != null) ? DumpsterManager.Instance.matchStartTime : Time.time;
        float timeTaken = Mathf.Max(0f, Time.time - matchStart);

        List<Sprite> foundItems = (DumpsterManager.Instance != null && DumpsterManager.Instance.sessionItemsFound != null) 
            ? DumpsterManager.Instance.sessionItemsFound : new List<Sprite>();

        List<Sprite> usedItems = (DumpsterManager.Instance != null && DumpsterManager.Instance.sessionItemsUsed != null) 
            ? DumpsterManager.Instance.sessionItemsUsed : new List<Sprite>();

        GameObject activePanel = null;
        int myScore = 0;
        int oppScore = 0;

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (tiePanel != null) tiePanel.SetActive(false);

        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
        }
        else if (victoryPanel != null)
        {
            Canvas parentCanvas = victoryPanel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null) parentCanvas.gameObject.SetActive(true);
        }

        if (tugOfWarSlider != null)
        {
            float sliderVal = Mathf.Clamp01(tugOfWarSlider.value);
            
            myScore = Mathf.RoundToInt((1f - sliderVal) * 1000);
            oppScore = Mathf.RoundToInt(sliderVal * 1000);

            if (sliderVal < 0.499f)
            {
                if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("VICTORY!", Color.green);
                if (victoryPanel != null) { victoryPanel.SetActive(true); activePanel = victoryPanel; }
            }
            else if (sliderVal > 0.501f)
            {
                if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("DEFEAT...", Color.red);
                if (defeatPanel != null) { defeatPanel.SetActive(true); activePanel = defeatPanel; }
            }
            else
            {
                if (GameFeed.Instance != null) GameFeed.Instance.LogMessage("IT'S A DRAW!", Color.yellow);
                if (tiePanel != null) { tiePanel.SetActive(true); activePanel = tiePanel; }
            }
        }

        if (activePanel != null)
        {
            activePanel.transform.SetAsLastSibling();

            EndGameTally tally = activePanel.GetComponentInChildren<EndGameTally>(true);
            if (tally != null)
            {
                tally.BeginTally(timeTaken, myScore, oppScore, foundItems, usedItems);
            }
            else
            {
                Debug.LogWarning("EndGameTally script missing on active result panel: " + activePanel.name);
            }
        }

        if (DumpsterManager.Instance != null) 
        {
            DumpsterManager.Instance.TriggerTrashStorm(); 
        }
        
        if (PlayFabAuthBridge.Instance != null)
        {
            PlayFabAuthBridge.Instance.SubmitHighScoreToLeaderboard(myScore);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (DumpsterManager.Instance == null) return;
        LoadAvatars();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (DumpsterManager.Instance == null) return;
        if (Launcher.IsTimeAttack) return;

        if (DumpsterManager.Instance != null) DumpsterManager.Instance.enabled = false;

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (disconnectScreen != null) disconnectScreen.SetActive(true);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (DumpsterManager.Instance == null) return;

        if (changedProps.ContainsKey("Avatar") || changedProps.ContainsKey("Border"))
        {
            LoadAvatars();
        }
        
        if (changedProps.ContainsKey("TargetIndex"))
        {
            RefreshPlayerTargetIcon(targetPlayer);
        }
    }

    public override void OnLeftRoom()
    {
        if (DumpsterManager.Instance == null) return;
        SceneManager.LoadScene(lobbySceneName);
    }
}