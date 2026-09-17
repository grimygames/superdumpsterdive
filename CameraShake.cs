using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeTimeRemaining;
    private float shakePower;
    private float shakeFadeTime;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;

            // Generate a random offset
            float x = Random.Range(-1f, 1f) * shakePower;
            float y = Random.Range(-1f, 1f) * shakePower;

            transform.localPosition = new Vector3(x, y, originalPos.z);

            // Gradually reduce the shake power
            shakePower = Mathf.MoveTowards(shakePower, 0f, shakeFadeTime * Time.deltaTime);
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    // Call this from other scripts!
    public void InduceShake(float length, float power)
    {
        shakeTimeRemaining = length;
        shakePower = power;
        shakeFadeTime = power / length;
    }
}