using UnityEngine;

public partial class FloatingScale : MonoBehaviour
{
    [Header("Movement Settings")]
    public float verticalSpeed = 2.0f;
    public float verticalAmplitude = 0.5f;
    public float horizontalSpeed = 1.0f;
    public float horizontalAmplitude = 0.1f;

    [Header("Scale Settings")]
    public float scaleSpeed = 1.5f;
    public float scaleAmplitude = 0.1f;

    private Vector3 startPosition;
    private Vector3 startScale;

    void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
    }

    void Update()
    {
        // 1. Handle Floating Position
        float newY = Mathf.Sin(Time.time * verticalSpeed) * verticalAmplitude;
        float newX = Mathf.Cos(Time.time * horizontalSpeed) * horizontalAmplitude;
        transform.localPosition = startPosition + new Vector3(newX, newY, 0);

        // 2. Handle Pulsing Scale
        // We add 1 to the Sin result so it oscillates around the original scale 
        // (e.g., pulsing between 90% and 110% size)
        float scaleFactor = Mathf.Sin(Time.time * scaleSpeed) * scaleAmplitude;
        transform.localScale = startScale + new Vector3(scaleFactor, scaleFactor, 0);
    }
}