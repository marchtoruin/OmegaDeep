using UnityEngine;

public class JellyfishDamagePlayer : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 15; 
    public string playerTag = "Player";

    // Use OnCollisionEnter2D if your Jellyfish collider is NOT a trigger
    /*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                Debug.Log($"Jellyfish damaged player for {damageAmount}");
                // Add knockback or other effects here if desired
            }
        }
    }
    */

    // Use OnTriggerEnter2D if your Jellyfish collider IS a trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Identify the root player GameObject (in case collider is on a child)
        GameObject playerObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!playerObj.CompareTag(playerTag)) return;
        // Send the hit message to the player to apply damage and knockback
        playerObj.SendMessage("HandleEnemyHit", gameObject, SendMessageOptions.DontRequireReceiver);
    }
} 