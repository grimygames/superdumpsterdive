using UnityEngine;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // The second the game opens, it triggers the roach and loads the menu!
        // Make sure "MainMenu" matches the exact name of your main menu scene file.
        if (GlobalLoadingScreen.Instance != null)
        {
            GlobalLoadingScreen.Instance.LoadScene("grimygamesIntro"); 
        }
    }
}