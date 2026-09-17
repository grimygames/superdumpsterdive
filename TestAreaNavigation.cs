using UnityEngine;
using UnityEngine.UI;

public class TestAreaNavigation : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject authTestPanel;

    [Header("Navigation Buttons")]
    public Button openTestAreaButton;
    public Button cancelBackButton;

    void Start()
    {
        // Force the correct baseline state when the main menu boots up
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (authTestPanel != null) authTestPanel.SetActive(false);

        // Auto-wire the click events so you don't have to drag them manually in the inspector
        if (openTestAreaButton != null)
            openTestAreaButton.onClick.AddListener(NavigateToTestArea);

        if (cancelBackButton != null)
            cancelBackButton.onClick.AddListener(NavigateToMainMenu);
    }

    public void NavigateToTestArea()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (authTestPanel != null) authTestPanel.SetActive(true);
        Debug.Log("[NAV] Switched to the Dev Authentication Test Area Panel.");
    }

    public void NavigateToMainMenu()
    {
        if (authTestPanel != null) authTestPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        Debug.Log("[NAV] Returned safely back to the Main Menu.");
    }
}