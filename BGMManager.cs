using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;
    private AudioSource audioSource;
    private bool isMusicEnabled = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true; 
            audioSource.playOnAwake = false; // We handle this manually below
            
            // Load saved preference (Default to 1 = ON)
            isMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
            
            if (isMusicEnabled) audioSource.Play();
        }
        else
        {
            // Destroy duplicate if we reload the Lobby scene
            Destroy(gameObject);
        }
    }

    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;
        
        // Save the setting so it remembers next time they launch the game
        PlayerPrefs.SetInt("MusicEnabled", isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (isMusicEnabled)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }
    }

    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
}