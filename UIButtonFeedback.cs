using UnityEngine;
using UnityEngine.UI;

public class UIButtonFeedback : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 selectedScale;

    void Awake()
    {
        // Cache the starting size
        originalScale = transform.localScale;
        
        // 🌟 FIX: Multiply by 1.15f to make it 15% larger (instead of shrinking it to 0.1)
        selectedScale = originalScale * 1.15f; 
    }

    public void Select()
    {
        transform.localScale = selectedScale;
    }

    public void Deselect()
    {
        transform.localScale = originalScale;
    }
}