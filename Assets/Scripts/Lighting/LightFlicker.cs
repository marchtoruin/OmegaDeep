using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    public Light2D light2D;
    public float flickerSpeed = 0.1f;
    public float intensityMin = 0.5f;
    public float intensityMax = 1.2f;

    private float timer;

    void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            light2D.intensity = Random.Range(intensityMin, intensityMax);
            timer = flickerSpeed;
        }
    }
}
