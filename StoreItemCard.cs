using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreItemCard : MonoBehaviour
{
    [Header("UI Component References")]
    public Image itemIcon;
    public TMP_Text itemLabel;
    public TMP_Text priceLabel; // Leave unassigned on your Wardrobe Prefab
    public Button actionButton;
}