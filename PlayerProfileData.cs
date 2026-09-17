using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfileData
{
    [Header("Authentication & Meta")]
    public bool isGuestAccount = true;
    public int highestScore;

    [Header("💵 Raw Materials Wallet (Unsold Inventory)")]
    public int aluminumCount;
    public int glassCount;
    public int plasticCount;
    public int otherCount; 
	public int copperCount;

    [Header("💰 Cash Balance")]
    public float cashBalance = 0.00f; // Tracked in dollars and cents!

    [Header("Custom Profile Customization Assets")]
    public string activeAvatarBase64 = "";
    public string selectedBorderId = "default_border";
    public string selectedSprayColorHex = "#FFFFFF";

    [Header("Purchased Inventory System")]
    public List<string> unlockedBorders = new List<string>() { "default_border" };
    public List<string> unlockedSprays = new List<string>() { "white", "green" }; 
    public bool ownsCustomColorWheel = false;
}