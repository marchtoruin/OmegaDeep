using UnityEngine;

public class RedOrbCollect : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] private int healAmount = 25;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[RedOrbCollect] Triggered by {other.name} (tag: {other.tag})", this);
        if (!other.CompareTag("Player")) return;
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }
        if (playerHealth != null)
        {
            Debug.Log($"[RedOrbCollect] Healing player {other.name} for {healAmount} HP", this);
            playerHealth.TakeDamage(-healAmount); // Negative damage heals
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[RedOrbCollect] PlayerHealth not found on {other.name}", this);
        }
    }
}