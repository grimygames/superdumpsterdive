using UnityEngine;

public class TrashSpinnerLoad : MonoBehaviour
{
    public float spinSpeed = 200f;
    //public float pulseSpeed = 2f;

    void Update()
    {
        // 1. Spin it round and round
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

        // 2. Make it pulse "breath" slightly
       // float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
        //transform.localScale = new Vector3(scale, scale, 1);
    }
}