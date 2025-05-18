using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class JellyChargePickup : MonoBehaviour
{
    public string playerTag = "Player";
    // Optional: Add effects for pickup (sound, particles)
    // public GameObject pickupEffectPrefab;
    // public FMODUnity.EventReference pickupSound;

    private void Awake()
    {
        // Ensure the collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"Collider on {gameObject.name} was not set to trigger. Setting it now.", this);
        }

        // Ensure a Rigidbody2D exists so trigger events work
        if (GetComponent<Rigidbody2D>() == null)
        {
            gameObject.AddComponent<Rigidbody2D>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Support colliders on child objects: get the root or attachedRigidbody
        GameObject hitObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!hitObj.CompareTag(playerTag)) return;

        // Try to find the flashlight controller on the player
        FlashlightController flashlight = hitObj.GetComponentInChildren<FlashlightController>();
        if (flashlight == null)
        {
            flashlight = FindObjectOfType<FlashlightController>();
        }

        if (flashlight != null)
        {
            flashlight.RechargeFully();
            Debug.Log($"JellyChargePickup: Recharged flashlight for {hitObj.name}.", this);

            // Play pickup effects (optional)
            // if (pickupEffectPrefab != null) Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            // if (!pickupSound.IsNull) FMODUnity.RuntimeManager.PlayOneShot(pickupSound, transform.position);

            // Destroy the pickup
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("JellyChargePickup: No FlashlightController found on player.", this);
        }
    }
} 