using UnityEngine;

public partial class FloatingItem : MonoBehaviour
{
    [Header("Settings")]
    public float verticalSpeed = 2.0f;
    public float verticalAmplitude = 0.5f;
    public float horizontalSpeed = 1.0f;
    public float horizontalAmplitude = 0.1f;

    private Vector3 startPosition;

    void Start()
    {
        // Store the starting position so we hover around it
        startPosition = transform.localPosition;
    }

    void Update()
    {
        // Calculate the new X and Y offsets using Sine waves
        float newY = Mathf.Sin(Time.time * verticalSpeed) * verticalAmplitude;
        float newX = Mathf.Cos(Time.time * horizontalSpeed) * horizontalAmplitude;

        // Apply the offset to the start position
        transform.localPosition = startPosition + new Vector3(newX, newY, 0);
    }
}