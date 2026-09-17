using UnityEngine;
using UnityEngine.UI; // Required for RawImage

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Texture itemTexture; // Changed from Sprite to Texture
}

public class TargetDisplay : MonoBehaviour
{
    [Header("UI References")]
    public RawImage targetRawImageSlot; // Drag your Raw Image object here
    
    [Header("Current Mission")]
    public InventoryItem goalItem;

    void Start()
    {
        if (goalItem != null)
        {
            UpdateTargetUI(goalItem);
        }
    }

    public void UpdateTargetUI(InventoryItem newItem)
    {
        goalItem = newItem;
        
        if (targetRawImageSlot != null && newItem.itemTexture != null)
        {
            // RawImages use 'texture' instead of 'sprite'
            targetRawImageSlot.texture = newItem.itemTexture;
            
            // Set alpha to full
            Color c = targetRawImageSlot.color;
            c.a = 1f;
            targetRawImageSlot.color = c;
        }
    }

    public void FoundAnItem(InventoryItem foundItem)
    {
        if (foundItem.itemName == goalItem.itemName)
        {
            // Hide the Raw Image
            Color c = targetRawImageSlot.color;
            c.a = 0f;
            targetRawImageSlot.color = c;
        }
    }
}