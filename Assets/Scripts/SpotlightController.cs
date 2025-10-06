using UnityEngine;

// [ExecuteAlways]
public class SpotlightController : MonoBehaviour
{
    [Header("X轴旋转参数")]
    [SerializeField] private float amplitudeX = 30f;
    [SerializeField] private float frequencyX = 1f;

    [Header("Y轴旋转参数")]
    [SerializeField] private float amplitudeY = 30f;
    [SerializeField] private float frequencyY = 1f;

    private Vector3 initialLocalEuler;

    void Start()
    {
        initialLocalEuler = transform.localEulerAngles;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * frequencyX) * amplitudeX;
        float y = Mathf.Sin(Time.time * frequencyY) * amplitudeY;
        transform.localEulerAngles = initialLocalEuler + new Vector3(x, y, 0f);
    }
}

