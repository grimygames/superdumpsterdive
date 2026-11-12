using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class SoloScoreEntry 
{
    public int score;
    public string avatarBase64;
}

[System.Serializable]
public class SoloLeaderboard 
{
    public List<SoloScoreEntry> entries = new List<SoloScoreEntry>();
}

public class HighScoreEntryUI : MonoBehaviour
{
    public TMP_Text scoreText;
    public Image avatarImage;
    public Image borderImage; // Moved here so the Inspector and Setup method can see it

    public void Setup(int pScore, Sprite avatar, Sprite border)
    {
        if (scoreText != null) scoreText.text = pScore.ToString();
        
        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(avatar != null);
            if (avatar != null) { avatarImage.sprite = avatar; avatarImage.color = Color.white; }
        }

        if (borderImage != null)
        {
            borderImage.gameObject.SetActive(border != null);
            if (border != null) { borderImage.sprite = border; borderImage.color = Color.white; }
        }
    }
}