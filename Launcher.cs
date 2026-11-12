using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PlayFab;
using PlayFab.ClientModels;

public class Launcher : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public struct MatchPreset
    {
        public string presetName;
        public float matchDuration;
        public int trashSpawnAmount;
    }

    [Header("Mode Selection")]
    // public static bool IsTimeAttack = false;

    [Header("UI Panels")]
    public GameObject menuPanel;         
    public GameObject multiplayerPanel;  
    public GameObject waitingPanel;      
    public GameObject cancelMatchmakingPanel; 
    public GameObject payphonePanel;     

    [Header("UI References")]
    public TMP_Text statusText;           
    public TMP_Text payphoneDisplayCode;  

    [Header("UI Interactivity Safety")]
    public Button hostButton;            
    public Button joinButton;            

    [Header("Settings")]
    public string gameSceneName = "Lobby"; 

    [Header("🌟 Dumpster Lobby Settings (Max 10)")]
    public GameObject[] dumpsterObjects;       
    public Image[] dumpsterPlayer1Avatars;     
    public Image[] dumpsterPlayer2Avatars;     
    
    [Header("🌟 Option B: Waiting Room Carousel Settings")]
    public TMP_Text presetDisplayMesh;        
    public Button carouselLeftArrow;          
    public Button carouselRightArrow;          
    public Button masterStartGameButton;      

    [Header("🌟 Waiting Area Profile Avatars & Borders")]
    public Image waitingRoomHostAvatar;       
    public Image waitingRoomHostBorder;       
    public Image waitingRoomGuestAvatar;      
    public Image waitingRoomGuestBorder;     

    public MatchPreset[] availablePresets = new MatchPreset[]
    {
        new MatchPreset { presetName = "QUICK TOSS\n(30s / 20 Trash)", matchDuration = 30f, trashSpawnAmount = 20 },
        new MatchPreset { presetName = "STANDARD GRIME\n(60s / 45 Trash)", matchDuration = 60f, trashSpawnAmount = 45 },
        new MatchPreset { presetName = "ALLEYWAY MARATHON\n(120s / 80 Trash)", matchDuration = 120f, trashSpawnAmount = 80 }
    };

    private int currentPresetIndex = 1; 
    private string currentDialedCode = "";
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    
    [Header("🌟 Waiting Room Code Display")]
    public TMP_Text waitingRoomCodeText;
	
	[Header("🌟 Solo Mode Settings")]
    public GameObject soloSettingsPanel;
    public TMP_Text soloPresetDisplayMesh;
    public Transform highscoreContainer;
    public GameObject highscoreEntryPrefab; // Assign a UI prefab with HighScoreEntryUI script
    
    // Static variables to pass data to the game scene
    public static float SoloMatchDuration = 60f;
    public static int SoloTrashSpawnAmount = 45;
    private int currentSoloPresetIndex = 1;
	public static int ActiveSoloPresetIndex = 0;

    void Start()
    {
        IsTimeAttack = false;
		IsTutorial = false;
        currentDialedCode = "";

        if (PhotonNetwork.InRoom)
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
            if (payphonePanel != null) payphonePanel.SetActive(false);
            if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(true);
            
            UpdateWaitingRoomCarouselUI();
            CheckPlayers();
            return; 
        }

        if (menuPanel != null) menuPanel.SetActive(true);
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (payphonePanel != null) payphonePanel.SetActive(false);
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            SetButtonsInteractable(true);
            if (statusText != null) statusText.text = "CONNECTED TO THE GRIME.";
            
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            SetButtonsInteractable(false);
            if (statusText != null) statusText.text = "CONNECTING TO THE ALLEYWAY...";
            PhotonNetwork.ConnectUsingSettings();
        }
        
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void SetButtonsInteractable(bool state)
    {
        if (hostButton != null) hostButton.interactable = state;
        if (joinButton != null) joinButton.interactable = state;
    }

    public void OpenMultiplayerMenu()
    {
        IsTimeAttack = false;
        menuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
        UpdateDumpsterVisualsMatrix();
    }

    public void BackToMainMenu()
    {
        if (payphonePanel != null && payphonePanel.activeSelf)
        {
            payphonePanel.SetActive(false);
            multiplayerPanel.SetActive(true);
            UpdateDumpsterVisualsMatrix(); 
            return;
        }

        multiplayerPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void ClickDumpsterSlot(int dumpsterIndex)
    {
		IsTutorial = false;
        if (!PhotonNetwork.IsConnectedAndReady) return;

        string targetRoomName = (1000 + (dumpsterIndex * 100)).ToString();

        if (cachedRoomList.ContainsKey(targetRoomName) && cachedRoomList[targetRoomName].PlayerCount >= 2)
        {
            if (statusText != null) statusText.text = $"DUMPSTER {dumpsterIndex + 1} IS FULL!";
            return;
        }

        if (statusText != null) statusText.text = $"ENTERING DUMPSTER {targetRoomName}...";

        ForceUploadLocalAvatarProperties();

        float matchDuration = 60f;
        int trashSpawnAmount = 45;

        if (availablePresets != null && availablePresets.Length > currentPresetIndex && currentPresetIndex >= 0)
        {
            matchDuration = availablePresets[currentPresetIndex].matchDuration;
            trashSpawnAmount = availablePresets[currentPresetIndex].trashSpawnAmount;
        }

        Hashtable customRoomProps = new Hashtable 
        { 
            { "MatchDuration", matchDuration }, 
            { "TrashSpawnAmount", trashSpawnAmount },
            { "ActivePresetIdx", currentPresetIndex }
        };

        RoomOptions matchOptions = new RoomOptions { 
            MaxPlayers = 2, 
            CustomRoomProperties = customRoomProps,
            IsVisible = true,
            IsOpen = true
        };
        matchOptions.CustomRoomPropertiesForLobby = new string[] { "MatchDuration", "TrashSpawnAmount", "ActivePresetIdx" };

        PhotonNetwork.JoinOrCreateRoom(targetRoomName, matchOptions, TypedLobby.Default);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList) cachedRoomList.Remove(room.Name);
            else cachedRoomList[room.Name] = room;
        }
        UpdateDumpsterVisualsMatrix();
    }

    private void UpdateDumpsterVisualsMatrix()
    {
        for (int i = 0; i < dumpsterObjects.Length; i++)
        {
            string roomName = (1000 + (i * 100)).ToString();
            int currentOccupants = cachedRoomList.ContainsKey(roomName) ? cachedRoomList[roomName].PlayerCount : 0;

            if (dumpsterPlayer1Avatars.Length > i && dumpsterPlayer1Avatars[i] != null)
                dumpsterPlayer1Avatars[i].gameObject.SetActive(currentOccupants >= 1);

            if (dumpsterPlayer2Avatars.Length > i && dumpsterPlayer2Avatars[i] != null)
                dumpsterPlayer2Avatars[i].gameObject.SetActive(currentOccupants >= 2);

            if (dumpsterObjects.Length > i && dumpsterObjects[i] != null)
            {
                Image dImg = dumpsterObjects[i].GetComponent<Image>();
                if (dImg != null) dImg.color = (currentOccupants >= 2) ? Color.gray : Color.white;
            }
        }
    }

    public void OpenPayphoneInterface()
    {
        multiplayerPanel.SetActive(false);
        if (payphonePanel != null) payphonePanel.SetActive(true);
        currentDialedCode = "";
        UpdatePayphoneScreenDisplay();
    }

    public void PressPayphoneKey(int numericDigit)
    {
        if (currentDialedCode.Length < 4)
        {
            currentDialedCode += numericDigit.ToString();
            UpdatePayphoneScreenDisplay();
        }
    }

    public void ClearPayphoneInput()
    {
        currentDialedCode = "";
        UpdatePayphoneScreenDisplay();
    }

    private void UpdatePayphoneScreenDisplay()
    {
        if (payphoneDisplayCode != null)
        {
            payphoneDisplayCode.text = string.IsNullOrEmpty(currentDialedCode) ? "DIAL CODE..." : currentDialedCode;
        }
    }

    public void PayphoneExecuteCall()
    {
        if (currentDialedCode.Length < 4)
        {
            if (statusText != null) statusText.text = "INVALID CODE NUMBER!";
            return;
        }

        ForceUploadLocalAvatarProperties();
        string targetRoomName = currentDialedCode;

        if (cachedRoomList.ContainsKey(targetRoomName) && cachedRoomList[targetRoomName].PlayerCount >= 2)
        {
            if (statusText != null) statusText.text = "LINE BUSY: ROOM IS FULL!";
            return;
        }

        if (statusText != null) statusText.text = $"DIALING LINE: {currentDialedCode}...";

        float matchDuration = 60f;
        int trashSpawnAmount = 45;

        if (availablePresets != null && availablePresets.Length > currentPresetIndex && currentPresetIndex >= 0)
        {
            matchDuration = availablePresets[currentPresetIndex].matchDuration;
            trashSpawnAmount = availablePresets[currentPresetIndex].trashSpawnAmount;
        }

        Hashtable customGameProps = new Hashtable 
        { 
            { "MatchDuration", matchDuration }, 
            { "TrashSpawnAmount", trashSpawnAmount },
            { "ActivePresetIdx", currentPresetIndex }
        };

        RoomOptions matchOptions = new RoomOptions { 
            MaxPlayers = 2, 
            CustomRoomProperties = customGameProps,
            IsVisible = true,
            IsOpen = true
        };
        matchOptions.CustomRoomPropertiesForLobby = new string[] { "MatchDuration", "TrashSpawnAmount", "ActivePresetIdx" };

        if (payphonePanel != null) payphonePanel.SetActive(false);

        PhotonNetwork.JoinOrCreateRoom(targetRoomName, matchOptions, TypedLobby.Default);
		
		IsTutorial = false;
    }

    public void CycleCarouselLeft()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentPresetIndex--;
        if (currentPresetIndex < 0) currentPresetIndex = availablePresets.Length - 1;
        UpdateLocalCarouselTextOnly();
        BroadcastSelectedPresetToNetwork();
    }

    public void CycleCarouselRight()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentPresetIndex++;
        if (currentPresetIndex >= availablePresets.Length) currentPresetIndex = 0;
        UpdateLocalCarouselTextOnly();
        BroadcastSelectedPresetToNetwork();
    }

    private void UpdateLocalCarouselTextOnly()
    {
        if (presetDisplayMesh != null && availablePresets.Length > currentPresetIndex)
        {
            presetDisplayMesh.text = availablePresets[currentPresetIndex].presetName;
        }

        bool isHost = PhotonNetwork.IsMasterClient;
        if (carouselLeftArrow != null) carouselLeftArrow.gameObject.SetActive(isHost);
        if (carouselRightArrow != null) carouselRightArrow.gameObject.SetActive(isHost);
        if (masterStartGameButton != null) masterStartGameButton.gameObject.SetActive(isHost);
    }

    private void UpdateWaitingRoomCarouselUI()
    {
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ActivePresetIdx", out object indexObj))
            {
                currentPresetIndex = System.Convert.ToInt32(indexObj);
            }
        }

        if (presetDisplayMesh != null && availablePresets.Length > currentPresetIndex)
        {
            presetDisplayMesh.text = availablePresets[currentPresetIndex].presetName;
        }

        bool isHost = PhotonNetwork.IsMasterClient;
        if (carouselLeftArrow != null) carouselLeftArrow.gameObject.SetActive(isHost);
        if (carouselRightArrow != null) carouselRightArrow.gameObject.SetActive(isHost);
        if (masterStartGameButton != null) masterStartGameButton.gameObject.SetActive(isHost);
    }

    private void BroadcastSelectedPresetToNetwork()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
        
        MatchPreset currentPreset = availablePresets[currentPresetIndex];
        Hashtable syncProps = new Hashtable 
        { 
            { "ActivePresetIdx", currentPresetIndex },
            { "MatchDuration", currentPreset.matchDuration },
            { "TrashSpawnAmount", currentPreset.trashSpawnAmount }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(syncProps);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("ActivePresetIdx") || 
            propertiesThatChanged.ContainsKey("MatchDuration") || 
            propertiesThatChanged.ContainsKey("TrashSpawnAmount"))
        {
            UpdateWaitingRoomCarouselUI();
        }
    }

    public void HostManualExecuteStartGame()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            if (statusText != null) statusText.text = "CANNOT START: NEED AN OPPONENT!";
            return;
        }

        if (waitingPanel.TryGetComponent(out CanvasGroup cg)) cg.blocksRaycasts = false;

        MatchPreset finalSelectedSetup = availablePresets[currentPresetIndex];
        
        Hashtable runtimeGameSetupProps = new Hashtable 
        {
            { "MatchDuration", finalSelectedSetup.matchDuration },
            { "TrashSpawnAmount", finalSelectedSetup.trashSpawnAmount }
        };
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(runtimeGameSetupProps);
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    public void RequestCancelMatchmaking()
    {
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(true);
    }

    public void DeclineCancelMatchmaking()
    {
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);
    }

    public void ConfirmCancelMatchmaking()
    {
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            if (statusText != null) statusText.text = "RETURNING TO DIRECTORY...";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            ResetToAbsoluteMainMenu();
        }
    }

    void ResetToAbsoluteMainMenu()
    {
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);     
        if (waitingPanel != null) waitingPanel.SetActive(false);  
        if (payphonePanel != null) payphonePanel.SetActive(false);
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);
        
        if (menuPanel != null) menuPanel.SetActive(true);  
        if (statusText != null) statusText.text = "CONNECTED TO THE GRIME.";
    }

    private void ForceUploadLocalAvatarProperties()
    {
        Hashtable props = new Hashtable();

        if (PlayerPrefs.HasKey("SavedAvatar"))
        {
            string base64String = PlayerPrefs.GetString("SavedAvatar");
            props["Avatar"] = base64String;
        }

        string activeBorderId = "default_border";
        if (PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
        {
            activeBorderId = PlayFabAuthBridge.Instance.profile.selectedBorderId;
        }
        props["Border"] = activeBorderId;

        if (PhotonNetwork.InRoom || PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    public override void OnConnectedToMaster()
    {
        if (statusText != null) statusText.text = "CONNECTED TO THE GRIME.";
        SetButtonsInteractable(true);
        PhotonNetwork.JoinLobby(); 
    }

    public override void OnJoinedLobby()
    {
        UpdateDumpsterVisualsMatrix();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (statusText != null) statusText.text = "LOST CONNECTION: " + cause.ToString();
        SetButtonsInteractable(false);
    }

    public override void OnJoinedRoom()
    {
        ForceUploadLocalAvatarProperties();

        if (statusText != null) statusText.text = "MATCH ROOM SECURED! LOADING INTERFACE...";

        if (waitingPanel == null || multiplayerPanel == null)
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                if (waitingPanel == null) { Transform t = mainCanvas.transform.Find("waitingPanel"); if (t != null) waitingPanel = t.gameObject; }
                if (multiplayerPanel != null) { Transform t = mainCanvas.transform.Find("multiplayerPanel"); if (t != null) multiplayerPanel = t.gameObject; }
            }
        }

        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (payphonePanel != null) payphonePanel.SetActive(false);
        
        if (waitingPanel != null) 
        {
            waitingPanel.SetActive(true);
        }

        UpdateWaitingRoomCarouselUI();
        CheckPlayers();
    }

    public override void OnLeftRoom()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (payphonePanel != null) payphonePanel.SetActive(false);
        if (cancelMatchmakingPanel != null) cancelMatchmakingPanel.SetActive(false);
        
        if (multiplayerPanel != null) multiplayerPanel.SetActive(true);

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CheckPlayers();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CheckPlayers();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Avatar") || changedProps.ContainsKey("Border"))
        {
            CheckPlayers();
        }
    }

    void CheckPlayers()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (waitingPanel != null && waitingPanel.activeSelf)
        {
            Player hostPlayer = PhotonNetwork.MasterClient;
            Player guestPlayer = null;

            foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
            {
                if (!kvp.Value.IsMasterClient)
                {
                    guestPlayer = kvp.Value;
                    break;
                }
            }

            RenderPlayerAvatarAndBorder(hostPlayer, waitingRoomHostAvatar, waitingRoomHostBorder);
            RenderPlayerAvatarAndBorder(guestPlayer, waitingRoomGuestAvatar, waitingRoomGuestBorder);

            if (waitingRoomCodeText != null)
            {
                waitingRoomCodeText.text = " " + PhotonNetwork.CurrentRoom.Name;  
            }
        }

        if (masterStartGameButton != null)
        {
            masterStartGameButton.interactable = (PhotonNetwork.CurrentRoom.PlayerCount == 2);
        }

        if (statusText != null)
        {
            statusText.text = (PhotonNetwork.CurrentRoom.PlayerCount == 2) 
                ? "OPPONENT READY. AWAITING HOST START..." 
                : "WAITING FOR OPPONENT TO CONNECT...";
        }

        UpdateWaitingRoomCarouselUI();
    }

    private void RenderPlayerAvatarAndBorder(Player player, Image avatarSlot, Image borderSlot)
    {
        if (player == null)
        {
            if (avatarSlot != null) avatarSlot.gameObject.SetActive(false);
            if (borderSlot != null) borderSlot.gameObject.SetActive(false);
            return;
        }

        // Render Avatar
        if (avatarSlot != null)
        {
            if (player.IsLocal && PlayerPrefs.HasKey("SavedAvatar"))
            {
                string base64 = PlayerPrefs.GetString("SavedAvatar");
                if (ConvertBase64ToSprite(base64, out Sprite localSprite))
                {
                    avatarSlot.gameObject.SetActive(true);
                    avatarSlot.sprite = localSprite;
                    avatarSlot.color = Color.white;
                }
            }
            else if (player.CustomProperties.TryGetValue("Avatar", out object base64Obj) && base64Obj != null)
            {
                if (ConvertBase64ToSprite(base64Obj.ToString(), out Sprite netSprite))
                {
                    avatarSlot.gameObject.SetActive(true);
                    avatarSlot.sprite = netSprite;
                    avatarSlot.color = Color.white;
                }
            }
            else
            {
                avatarSlot.gameObject.SetActive(false);
            }
        }

        // Render Border
        if (borderSlot != null)
        {
            string borderId = "default_border";
            if (player.CustomProperties.TryGetValue("Border", out object borderObj) && borderObj != null)
            {
                borderId = borderObj.ToString();
            }
            else if (player.IsLocal && PlayFabAuthBridge.Instance != null && PlayFabAuthBridge.Instance.profile != null)
            {
                borderId = PlayFabAuthBridge.Instance.profile.selectedBorderId;
            }

            Sprite borderSprite = GetBorderSpriteById(borderId);
            if (borderSprite != null)
            {
                borderSlot.gameObject.SetActive(true);
                borderSlot.sprite = borderSprite;
                borderSlot.color = Color.white;
            }
            else
            {
                borderSlot.gameObject.SetActive(false);
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

    private bool ConvertBase64ToSprite(string base64String, out Sprite resultSprite)
    {
        resultSprite = null;
        if (string.IsNullOrEmpty(base64String)) return false;
        try
        {
            byte[] textureBytes = System.Convert.FromBase64String(base64String);
            Texture2D reconstructedTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            reconstructedTex.LoadImage(textureBytes);

            resultSprite = Sprite.Create(reconstructedTex, new Rect(0, 0, reconstructedTex.width, reconstructedTex.height), new Vector2(0.5f, 0.5f));
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Launcher Avatar Decoder Failure: {ex.Message}");
            return false;
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (statusText != null) statusText.text = "CONNECTION FAILED: " + message;
        ResetToAbsoluteMainMenu();
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (statusText != null) statusText.text = $"ROOM CREATION FAILED: {message}";
        ResetToAbsoluteMainMenu();
    }
	

	[Header("Mode Selection")]
	public static bool IsTimeAttack = false;
	public static bool IsTutorial = false; // Add static tutorial mode flag

	public void StartTutorialMode()
	{
		IsTutorial = true;
		IsTimeAttack = false;
		
		// Save avatar/border settings safely for local UI components
		ForceUploadLocalAvatarProperties();
		
		if (statusText != null) statusText.text = "ENTERING TUTORIAL...";
		SceneManager.LoadScene(gameSceneName);
	}
	
	public void OpenSoloMenu()
    {
        IsTimeAttack = true;
        IsTutorial = false;
        
        if (menuPanel != null) menuPanel.SetActive(false);
        if (soloSettingsPanel != null) soloSettingsPanel.SetActive(true);
        
        UpdateSoloCarouselUI();
        LoadAndDisplayHighScores();
    }

    public void CycleSoloCarouselLeft()
    {
        currentSoloPresetIndex--;
        if (currentSoloPresetIndex < 0) currentSoloPresetIndex = availablePresets.Length - 1;
        UpdateSoloCarouselUI();
		LoadAndDisplayHighScores();
    }

    public void CycleSoloCarouselRight()
    {
        currentSoloPresetIndex++;
        if (currentSoloPresetIndex >= availablePresets.Length) currentSoloPresetIndex = 0;
        UpdateSoloCarouselUI();
		LoadAndDisplayHighScores();
    }

	private void UpdateSoloCarouselUI()
    {
        if (soloPresetDisplayMesh != null && availablePresets.Length > currentSoloPresetIndex)
        {
            soloPresetDisplayMesh.text = availablePresets[currentSoloPresetIndex].presetName;
            
            // Lock in the variables for the game scene
            SoloMatchDuration = availablePresets[currentSoloPresetIndex].matchDuration;
            SoloTrashSpawnAmount = availablePresets[currentSoloPresetIndex].trashSpawnAmount;
            ActiveSoloPresetIndex = currentSoloPresetIndex; // ADDED THIS LINE
        }
    }

    // Overwrite the original StartSoloTimeAttack
    public void StartSoloTimeAttack()
    {
        IsTutorial = false;
        IsTimeAttack = true;
        ForceUploadLocalAvatarProperties();
        if (statusText != null) statusText.text = "DIVING SOLO...";
        SceneManager.LoadScene(gameSceneName);
    }

    // --- HIGHSCORE LEADERBOARD ---

	private void LoadAndDisplayHighScores()
    {
        if (highscoreContainer == null || highscoreEntryPrefab == null) return;
        foreach (Transform child in highscoreContainer) Destroy(child.gameObject);

        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // Dynamic leaderboard name based on the selected preset (e.g., "SoloScore_0", "SoloScore_1")
        string statName = "SoloScore_" + currentSoloPresetIndex;

        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = statName,
            StartPosition = 0,
            MaxResultsCount = 25 
        }, 
        result => {
            StartCoroutine(FetchLeaderboardDataCoro(result.Leaderboard));
        }, 
        error => {
            // ADDED: Wipe the board clean if this preset has zero logged scores
            foreach (Transform child in highscoreContainer) Destroy(child.gameObject);
            Debug.LogWarning($"Leaderboard {statName} is empty or hasn't been created yet.");
        });
    }

    private IEnumerator FetchLeaderboardDataCoro(List<PlayerLeaderboardEntry> leaderboard)
    {
        // Clear again to prevent duplicates if the player clicked quickly
        foreach (Transform child in highscoreContainer) Destroy(child.gameObject);

        foreach (var entry in leaderboard)
        {
            GameObject go = Instantiate(highscoreEntryPrefab, highscoreContainer);
            HighScoreEntryUI ui = go.GetComponent<HighScoreEntryUI>();
            if (ui == null) continue;

            // Display the score instantly while the graphics load
            ui.Setup(entry.StatValue, null, null);

            // Fetch this specific player's Public Data
            PlayFabClientAPI.GetUserData(new GetUserDataRequest {
                PlayFabId = entry.PlayFabId,
                Keys = new List<string> { "PublicAvatar", "PublicBorder" }
            }, 
            userDataResult => {
                Sprite avatarSprite = null;
                Sprite borderSprite = null;

                if (userDataResult.Data.ContainsKey("PublicAvatar"))
                {
                    ConvertBase64ToSprite(userDataResult.Data["PublicAvatar"].Value, out avatarSprite);
                }
                if (userDataResult.Data.ContainsKey("PublicBorder"))
                {
                    borderSprite = GetBorderSpriteById(userDataResult.Data["PublicBorder"].Value);
                }

                // Update the UI with their loaded graphics
                ui.Setup(entry.StatValue, avatarSprite, borderSprite);
            }, 
            error => {
                Debug.LogWarning("Failed to get graphics for " + entry.PlayFabId);
            });

            // Brief delay to prevent hitting PlayFab API rate limits
            yield return new WaitForSeconds(0.1f);
        }
    }
	
	public void CloseSoloMenu()
    {
        IsTimeAttack = false; // Reset the flag
        
        if (soloSettingsPanel != null) soloSettingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }
	
}