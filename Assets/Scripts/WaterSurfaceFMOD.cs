using UnityEngine;
using FMODUnity;

public class WaterSurfaceFMOD : MonoBehaviour
{
    [Tooltip("FMOD event to play when the player surfaces")]
    [SerializeField] private EventReference surfaceFmodEvent;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!surfaceFmodEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(surfaceFmodEvent, transform.position);
        }
    }
} 