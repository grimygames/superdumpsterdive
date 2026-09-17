using UnityEngine;
using UnityEngine.UI;

public class AlphaRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private RawImage _rawImage;
    [SerializeField, Range(0f, 1f)] private float alphaThreshold = 0.1f;

    void Awake()
    {
        // Automatically finds the RawImage on this object or its children
        _rawImage = GetComponentInChildren<RawImage>();
		GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
	

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (_rawImage == null || _rawImage.texture == null) return true;

        // Convert screen point to local position inside the RawImage
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImage.rectTransform, sp, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        // Convert local point to normalized 0-1 UV coordinates
        Rect rect = _rawImage.rectTransform.rect;
        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        // Handle RawImage UV Rect offsets
        Rect uvRect = _rawImage.uvRect;
        u = uvRect.x + u * uvRect.width;
        v = uvRect.y + v * uvRect.height;

        // Sample the pixel alpha from the texture
        try
        {
            Texture2D tex2D = _rawImage.texture as Texture2D;
            if (tex2D != null)
            {
                Color pixelColor = tex2D.GetPixelBilinear(u, v);
                return pixelColor.a >= alphaThreshold;
            }
        }
        catch (UnityException)
        {
            Debug.LogError($"AlphaRaycastFilter: '{_rawImage.texture.name}' is not readable. Enable 'Read/Write' in its Texture Import Settings.", this);
        }

        return true;
    }
}