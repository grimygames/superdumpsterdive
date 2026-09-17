using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;
using System.Text;

public class PlayFabAuthBridge : MonoBehaviour
{
    public static PlayFabAuthBridge Instance;
    public PlayerProfileData profile = new PlayerProfileData();
    public string MyPlayFabId { get; private set; }

    [Header("Persistent Border Library")]
    public List<DumpsterStoreUI.BorderAssetMapping> borderLibrary = new List<DumpsterStoreUI.BorderAssetMapping>();
    public Sprite defaultEmptyBorder;

    private const string PREF_DEVICE_ID = "Diver_Device_UUID";
    private const string CLOUD_DATA_KEY = "DumpsterProfile";

    void Awake()
    {
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

    void Start()
    {
        // 1. Load locally cached data first for instant responsiveness
        LoadLocalData();

        // 2. Silently connect to PlayFab in the background
        LoginSilently();
    }

    public Sprite GetBorderSpriteById(string borderId)
    {
        if (string.IsNullOrEmpty(borderId)) return defaultEmptyBorder;
        var mapping = borderLibrary?.Find(b => b.borderId == borderId);
        return (mapping != null && mapping.borderSprite != null) ? mapping.borderSprite : defaultEmptyBorder;
    }

    // --- 🔑 SILENT AUTHENTICATION ---

    private void LoginSilently()
    {
        if (!PlayerPrefs.HasKey(PREF_DEVICE_ID))
        {
            PlayerPrefs.SetString(PREF_DEVICE_ID, Guid.NewGuid().ToString());
            PlayerPrefs.Save();
        }

        string customId = PlayerPrefs.GetString(PREF_DEVICE_ID);

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, 
            result => {
                MyPlayFabId = result.PlayFabId;
                Debug.Log($"[Auth] Silently connected. ID: {MyPlayFabId}");
                DownloadCloudData();
            },
            error => {
                Debug.LogWarning($"[Auth] PlayFab offline: {error.ErrorMessage}. Using local cache.");
            }
        );
    }

    // --- 💾 SAVE & LOAD SYSTEM ---

    public void DownloadCloudData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest { PlayFabId = MyPlayFabId }, result =>
        {
            if (result.Data != null && result.Data.ContainsKey(CLOUD_DATA_KEY))
            {
                profile = JsonUtility.FromJson<PlayerProfileData>(result.Data[CLOUD_DATA_KEY].Value);
                SaveLocalData();
                RefreshGameUI();
            }
            else
            {
                // First time user on cloud: upload initial profile
                SaveProgressToCloud();
            }
        }, error => Debug.LogWarning("[Auth] Failed to download cloud data. Using local."));
    }

    public void SaveProgressToCloud()
    {
        SaveLocalData();

        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        string payload = JsonUtility.ToJson(profile);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { CLOUD_DATA_KEY, payload } }
        };

        PlayFabClientAPI.UpdateUserData(request, null, null);
    }

    private void SaveLocalData()
    {
        PlayerPrefs.SetInt("OwnsColorWheel", profile.ownsCustomColorWheel ? 1 : 0);
        PlayerPrefs.SetInt("CopperCount", profile.copperCount);
        PlayerPrefs.SetInt("AluminumCount", profile.aluminumCount);
        PlayerPrefs.SetInt("GlassCount", profile.glassCount);
        PlayerPrefs.SetInt("PlasticCount", profile.plasticCount);
        PlayerPrefs.SetInt("OtherCount", profile.otherCount);
        PlayerPrefs.SetString("LocalProfileJSON", JsonUtility.ToJson(profile));
        PlayerPrefs.Save();
    }

    private void LoadLocalData()
    {
        if (PlayerPrefs.HasKey("LocalProfileJSON"))
        {
            string json = PlayerPrefs.GetString("LocalProfileJSON");
            profile = JsonUtility.FromJson<PlayerProfileData>(json);
        }
    }

    public void RefreshGameUI()
    {
        AvatarPainter painter = FindObjectOfType<AvatarPainter>(true);
        if (painter != null)
        {
            painter.RefreshColorWheelButtonVisuals();
            if (painter.colorWheelScript != null) painter.colorWheelScript.RefreshLockState();
        }

        if (DumpsterStoreUI.Instance != null)
        {
            DumpsterStoreUI.Instance.RefreshPersistentDashboard(false);
        }
    }

    public void SubmitHighScoreToLeaderboard(int score)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "HighScore", Value = score }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, null, null);
    }

    // --- 📋 SAVE CODE EXPORT & IMPORT ---

    public string ExportSaveCode()
    {
        string json = JsonUtility.ToJson(profile);
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(plainTextBytes);
    }

    public bool ImportSaveCode(string saveCode)
    {
        try
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(saveCode.Trim());
            string json = Encoding.UTF8.GetString(base64EncodedBytes);
            PlayerProfileData loadedData = JsonUtility.FromJson<PlayerProfileData>(json);

            if (loadedData != null)
            {
                profile = loadedData;
                SaveProgressToCloud();
                RefreshGameUI();
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveCode] Corrupt or invalid save code: " + e.Message);
        }
        return false;
    }
}