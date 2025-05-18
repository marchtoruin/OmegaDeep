using UnityEngine;

public class RedOrbCollect : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] private int healAmount = 25;
    [SerializeField] private string playerTag = "Player";

    // OnTriggerEnter2D removed - Logic moved to PlayerHealth.OnCollisionEnter2D
    /*
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[RedOrbCollect] >>> OnTriggerEnter2D called by {other.gameObject.name} <<< Layer: {LayerMask.LayerToName(other.gameObject.layer)}", this);

        if (string.IsNullOrEmpty(playerTag) || !other.CompareTag(playerTag))
        {
            Debug.Log($"[RedOrbCollect] Object {other.name} did not match playerTag '{playerTag}'. Ignoring.", this);
            return;
        }

        Debug.Log($"[RedOrbCollect] Player tag matched. Searching for PlayerHealth on {other.name}...", this);
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.Log($"[RedOrbCollect] PlayerHealth not found on {other.name}, searching in parent...", this);
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            Debug.Log($"[RedOrbCollect] PlayerHealth FOUND! Healing player {other.name} for {healAmount} HP and destroying orb.", this);
            playerHealth.TakeDamage(-healAmount); // Negative damage heals
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"[RedOrbCollect] PlayerHealth component NOT FOUND on {other.name} or its parents! Orb not collected.", this);
        }
    }
    */

    // Public getter for the heal amount
    public int GetHealAmount() {
        return healAmount;
    }
}