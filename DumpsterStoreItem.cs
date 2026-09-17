using UnityEngine;

[System.Serializable]
public class DumpsterStoreItem
{
    public enum ItemType { Border, SprayColor }
    public enum CostType { Aluminum, Plastic, Glass, Other, RealMoney }

    public string itemId;          // e.g., "sludge_border", "color_wheel_premium"
    public string displayName;     // e.g., "Sludge Frame", "Pro Color Wheel"
    public ItemType itemType;
    public CostType costType;
    public int price;
    public string valueHexColor;   // For spray paint choices (#FF0000)
    public Sprite itemPreviewIcon;
}