using UnityEngine;

public class AirBubble : MonoBehaviour
{
    [Tooltip("How much oxygen to restore (absolute value, e.g. 20 = 20 units)")]
    public float oxygenRestoreAmount = 20f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerOxygen playerOxygen = other.GetComponent<PlayerOxygen>();
            if (playerOxygen != null)
            {
                playerOxygen.RefillOxygen(oxygenRestoreAmount);
            }
            Destroy(gameObject);
        }
    }
} 