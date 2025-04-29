using UnityEngine;
using UnityEngine.Rendering.Universal; // Needed for 2D lights!

public class LightPulse : MonoBehaviour
{
    public Light2D light2D;
    public float pulseSpeed = 2f;
    public float intensityMin = 0.5f;
    public float intensityMax = 1.5f;

    private float originalIntensity;

    void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        originalIntensity = light2D.intensity;
    }

    void Update()
    {
        float pulse = Mathf.Lerp(intensityMin, intensityMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        light2D.intensity = pulse;
    }
}
