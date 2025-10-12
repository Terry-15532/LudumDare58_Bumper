using UnityEngine;

public class SinWaveShaker : MonoBehaviour
{
    [Header("Shake Magnitude")] 
    public Vector3 magnitude = new Vector3(0.1f, 0.1f, 0.1f);
    [Header("Shake Frequency")] 
    public Vector3 frequency = new Vector3(1f, 1f, 1f);

    private Vector3 originalLocalPosition;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float t = Time.time;
        Vector3 offset = new Vector3(
            Mathf.Sin(t * frequency.x) * magnitude.x,
            Mathf.Sin(t * frequency.y) * magnitude.y,
            Mathf.Sin(t * frequency.z) * magnitude.z
        );
        transform.localPosition = originalLocalPosition + offset;
    }
}

