using UnityEngine;
using UnityEngine.UI;

public class UIButtonFeedback : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 selectedScale;

    void Awake()
    {
        originalScale = Vector3.one;
        selectedScale = Vector3.one * 0.1f; // Scale up by 1.5x (2x might be too huge, but you can change this to 2f!)
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