using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GenerateColorWheelTexture : MonoBehaviour
{
    [Header("⚙️ Texture Resolution")]
    public int textureSize = 512;

    void Awake()
    {
        GenerateWheel();
    }

    [ContextMenu("Re-Generate Wheel")]
    public void GenerateWheel()
    {
        Texture2D wheelTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        wheelTex.wrapMode = TextureWrapMode.Clamp;
        wheelTex.filterMode = FilterMode.Bilinear;

        float radius = textureSize / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float dist = pos.magnitude;

                if (dist <= radius)
                {
                    // Calculate angle (0 to 360) for Hue
                    float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    float hue = angle / 360f;
                    float saturation = dist / radius;

                    // Convert HSV directly to pixel color
                    Color pixelColor = Color.HSVToRGB(hue, saturation, 1f);
                    wheelTex.SetPixel(x, y, pixelColor);
                }
                else
                {
                    // Transparent pixels outside the circle boundary
                    wheelTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }

        wheelTex.Apply();

        // Convert the generated Texture2D into a UI Sprite
        Sprite generatedSprite = Sprite.Create(
            wheelTex, 
            new Rect(0, 0, textureSize, textureSize), 
            new Vector2(0.5f, 0.5f)
        );

        GetComponent<Image>().sprite = generatedSprite;
        GetComponent<Image>().color = Color.white; // Force full visibility
    }
}