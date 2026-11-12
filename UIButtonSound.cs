using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Tooltip("The sound effect to play when this button is clicked.")]
    public AudioClip clickSound;

    // Static variables ensure ALL buttons share the exact same optimized pool
    private static List<AudioSource> audioPool;
    private static GameObject poolContainer;
    private const int POOL_SIZE = 5; // Keeps 5 audio players ready at all times

    void Start()
    {
        InitializeSharedPool();
        
        // Automatically hook into this button's click event
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    private void InitializeSharedPool()
    {
        // If the pool was already created by another button, skip initialization
        if (audioPool != null) return; 

        audioPool = new List<AudioSource>();
        
        // Create an invisible GameObject to hold the AudioSources
        poolContainer = new GameObject("UIButton_SharedAudioPool");
        DontDestroyOnLoad(poolContainer); // Keeps the pool alive when switching scenes

        for (int i = 0; i < POOL_SIZE; i++)
        {
            AudioSource src = poolContainer.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // Forces 2D UI sound
            audioPool.Add(src);
        }
    }

    public void PlaySound()
    {
        if (clickSound == null || audioPool == null) return;

        // Look for an inactive audio player in the shared pool
        foreach (AudioSource src in audioPool)
        {
            if (!src.isPlaying)
            {
                src.clip = clickSound;
                src.pitch = Random.Range(0.95f, 1.05f); // Slight pitch variance so clicks don't sound robotic
                src.Play();
                return;
            }
        }

        // Failsafe: If all 5 players are currently making noise, hijack the first one
        audioPool[0].clip = clickSound;
        audioPool[0].pitch = Random.Range(0.95f, 1.05f);
        audioPool[0].Play();
    }
}