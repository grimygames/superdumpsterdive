using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Photon.Pun;

public class DraggableTrash : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Button itemButton; 
    private Vector2 velocity;

    [Header("Item Data")]
    public TrashData data;

    void Awake() 
    {
		Image img = GetComponent<Image>();
        if (img == null) img = GetComponentInChildren<Image>();

        if (img != null)
        {
            // Forces Unity to ignore clicks on transparent pixels (alpha < 0.1)
            img.alphaHitTestMinimumThreshold = 0.1f;
        }
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Cache the button so we can disable it during animations
        itemButton = GetComponent<Button>();
    }

    public void OnBeginDrag(PointerEventData eventData) 
    {
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
        
        // Smoother flick velocity calculation using raw frame delta
        velocity = eventData.delta; 

        float wobbleRadius = 100f; 
        float maxWobbleAngle = 1.5f; 

        foreach (Transform sibling in transform.parent)
        {
            if (sibling == transform) continue;
            
            RectTransform siblingRT = sibling.GetComponent<RectTransform>();
            if (siblingRT == null) continue;

            float distance = Vector2.Distance(rectTransform.anchoredPosition, siblingRT.anchoredPosition);

            if (distance < wobbleRadius)
            {
                float strength = 1f - (distance / wobbleRadius);
                float randomWobble = Random.Range(-maxWobbleAngle, maxWobbleAngle) * strength;
                sibling.Rotate(0, 0, randomWobble);
                
                Vector2 pushDirection = (siblingRT.anchoredPosition - rectTransform.anchoredPosition).normalized;
                siblingRT.anchoredPosition += pushDirection * strength * 1.2f;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData) 
    {
        canvasGroup.blocksRaycasts = true;
        if (IsOutOfBounds()) 
        {
            TossLogic();
        }
    }

    bool IsOutOfBounds() 
    {
        return !RectTransformUtility.RectangleContainsScreenPoint(
            transform.parent.GetComponent<RectTransform>(), 
            Input.mousePosition);
    }

    void TossLogic()
    {
        if (DumpsterManager.Instance == null || DumpsterManager.Instance.activeModeLogic == null) return;

        // 1. Tell the mode manager to score points, penalize, and alert the network
        DumpsterManager.Instance.activeModeLogic.HandleItemTossed(data);

        // 2. Lock the item so it can't be clicked or grabbed anymore
        if (itemButton != null) itemButton.interactable = false;
        canvasGroup.blocksRaycasts = false;
        this.enabled = false; 

        // 3. Trigger the visual flick animation
        if (DumpsterJuiceFX.Instance != null)
        {
            DumpsterJuiceFX.Instance.PlayItemFlickAway(this.gameObject, velocity);
        }
        else
        {
            // Failsafe: if DumpsterJuiceFX is missing, destroy it so it doesn't freeze on screen
            Destroy(gameObject);
        }
    }
}