using UnityEngine;

public class SurfaceAir : MonoBehaviour
{
    [Tooltip("How much oxygen to restore per second while player is surfaced")]
    public float oxygenPerSecond = 15f;

    // Use OnTriggerStay2D for continuous refill while player is in the trigger
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerOxygen playerOxygen = other.GetComponent<PlayerOxygen>();
            if (playerOxygen != null)
            {
                // Refill oxygen based on time spent in trigger
                playerOxygen.RefillOxygen(oxygenPerSecond * Time.deltaTime);
            }
            // --- Do NOT Destroy(gameObject) here --- 
        }
    }
} 