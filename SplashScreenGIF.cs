using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreenGIF : MonoBehaviour
{
    public Texture2D[] gifFrames; // Assign the individual frames of your GIF in the Inspector
    public float frameRate = 10f; // Adjust the frame rate as needed
    public string nextSceneName = "Lobby";

    private RawImage rawImage;
    private int currentFrame = 0;
    private float timer = 0f;
    private AudioListener audioListener;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage == null)
        {
            rawImage = gameObject.AddComponent<RawImage>();
        }

        rawImage.texture = gifFrames[0]; // Set the initial frame

        StartCoroutine(LoadSceneAfterGIF());
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            currentFrame = (currentFrame + 1) % gifFrames.Length;
            rawImage.texture = gifFrames[currentFrame];
        }
    }

    IEnumerator LoadSceneAfterGIF()
    {
        yield return new WaitForSeconds((float)gifFrames.Length / frameRate); // wait for gif to play.

        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

}