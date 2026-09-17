using UnityEngine;
using UnityEngine.UI;

public class MenuNavigationController : MonoBehaviour
{
    [Header("🏢 UI Panel Groupings")]
    public GameObject mainMenuPanel;
    public GameObject authTestPanel;
    public GameObject storePanel;

    [Header("🔘 Trigger Buttons")]
    public Button openAuthSuiteButton;
    public Button openStoreButton;
    public Button closeAuthButton;
    public Button closeStoreButton;

    void Start()
    {
        // Establish our baseline boot layout state
        ShowPanel(mainMenuPanel);

        // Auto-wire navigation events to avoid Inspector dragging mistakes
        if (openAuthSuiteButton != null) openAuthSuiteButton.onClick.AddListener(() => ShowPanel(authTestPanel));
        if (openStoreButton != null) openStoreButton.onClick.AddListener(OpenStoreAndRefresh);
        
        if (closeAuthButton != null) closeAuthButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
        if (closeStoreButton != null) closeStoreButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
    }

    public void ShowPanel(GameObject panelToActivate)
    {
        // Deactivate all core panels first to eliminate visual overlaps
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (authTestPanel != null) authTestPanel.SetActive(false);
        if (storePanel != null) storePanel.SetActive(false);

        // Safely bring the requested layout forward
        if (panelToActivate != null) panelToActivate.SetActive(true);
    }

    public void OpenStoreAndRefresh()
    {
        ShowPanel(storePanel);
        
        // Automatically ping the store interface to update its resource displays 
        // using the currency values currently sitting inside PlayFab's local profile memory
       DumpsterStoreUI storeUI = FindObjectOfType<DumpsterStoreUI>();
        if (storeUI != null) storeUI.RefreshPersistentDashboard(false);
    }
}