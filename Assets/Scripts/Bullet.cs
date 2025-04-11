using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Collision Effects")]
    [SerializeField] private GameObject hitEffectPrefab; // Optional hit effect prefab
    [SerializeField] private bool debugCollisions = true; // Enable collision debugging

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers = ~0; // Default to all layers
    [SerializeField] private float ignoreShooterTime = 0.3f; // Time to ignore shooter collisions (increased for safety)
    
    private bool hasCollided = false; // Prevent multiple collisions
    private GameObject shooter; // Reference to the object that fired this bullet
    private float creationTime; // When this bullet was created
    private Collider2D[] shooterColliders; // Cache of shooter colliders
    
    private void Awake()
    {
        creationTime = Time.time;
    }

    private void Start()
    {
        // Debug component setup
        if (debugCollisions)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            Collider2D col = GetComponent<Collider2D>();
            
            if (rb == null)
            {
                Debug.LogError("Bullet is missing a Rigidbody2D component!", this);
            }
            else
            {
                Debug.Log($"Bullet Rigidbody2D - BodyType: {rb.bodyType}, Simulated: {rb.simulated}, " +
                          $"Interpolation: {rb.interpolation}, CollisionDetection: {rb.collisionDetectionMode}", this);
            }
            
            if (col == null)
            {
                Debug.LogError("Bullet is missing a Collider2D component!", this);
            }
            else
            {
                Debug.Log($"Bullet Collider2D - IsTrigger: {col.isTrigger}, Enabled: {col.enabled}, " +
                          $"Layer: {LayerMask.LayerToName(gameObject.layer)}, Size: {(col is CircleCollider2D ? (col as CircleCollider2D).radius.ToString() : "unknown")}", this);
                
                // Check if on the same layer as other objects you want to collide with
                Debug.Log($"Current layer ({LayerMask.LayerToName(gameObject.layer)}) is in collision mask: {(((1 << gameObject.layer) & collisionLayers) != 0)}", this);
            }
            
            // Draw a debug trail to visualize bullet path
            StartCoroutine(DebugTrail());
        }
        
        // Find the parent Player object to ignore collisions with
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && shooter == null)
        {
            SetShooter(player);
        }
    }
    
    // Call this from DiverShooter when instantiating the bullet
    public void SetShooter(GameObject shooterObject)
    {
        shooter = shooterObject;
        
        // Cache shooter colliders for quick access in collision checks
        if (shooter != null)
        {
            shooterColliders = shooter.GetComponentsInChildren<Collider2D>();
            
            if (debugCollisions)
            {
                Debug.Log($"Set shooter to {shooter.name}, cached {shooterColliders.Length} colliders", this);
            }
            
            // If we are using physical collisions rather than triggers, use IgnoreCollision
            Collider2D bulletCol = GetComponent<Collider2D>();
            if (bulletCol != null && !bulletCol.isTrigger)
            {
                IgnoreCollisionsWithShooter(shooter);
            }
        }
    }
    
    private void IgnoreCollisionsWithShooter(GameObject shooterObject)
    {
        if (shooterObject == null) return;
        
        // Get all colliders on the bullet
        Collider2D[] bulletColliders = GetComponents<Collider2D>();
        
        // Ignore collisions between all bullet colliders and all shooter colliders
        foreach (Collider2D bulletCol in bulletColliders)
        {
            if (bulletCol.isTrigger) continue; // Skip triggers as IgnoreCollision doesn't work with them
            
            foreach (Collider2D shooterCol in shooterColliders)
            {
                if (shooterCol.isTrigger) continue; // Skip triggers
                
                Physics2D.IgnoreCollision(bulletCol, shooterCol, true);
                
                if (debugCollisions)
                {
                    Debug.Log($"Ignoring collisions between bullet and {shooterCol.gameObject.name}", this);
                }
            }
        }
    }
    
    private IEnumerator DebugTrail()
    {
        Vector3 lastPos = transform.position;
        while (!hasCollided)
        {
            Debug.DrawLine(lastPos, transform.position, Color.red, 1f);
            lastPos = transform.position;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we're hitting the shooter within the ignore time
        if (ShouldIgnoreCollision(collision.gameObject))
        {
            if (debugCollisions)
            {
                Debug.Log($"Ignored collision with shooter: {collision.gameObject.name}", this);
            }
            return;
        }
        
        if (hasCollided) return;
        hasCollided = true;
        
        if (debugCollisions)
        {
            Debug.Log($"Bullet collided with {collision.gameObject.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}", this);
        }
        
        // Optionally spawn a hit effect at the collision point
        if (hitEffectPrefab != null && collision.contactCount > 0)
        {
            // Get the collision point
            ContactPoint2D contact = collision.GetContact(0);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
            
            // Instantiate the hit effect
            Instantiate(hitEffectPrefab, contact.point, rotation);
        }
        
        // Destroy the bullet
        Destroy(gameObject);
    }
    
    // Add trigger handling as a fallback
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we're hitting the shooter within the ignore time
        if (ShouldIgnoreCollision(other.gameObject))
        {
            if (debugCollisions)
            {
                Debug.Log($"Ignored trigger with shooter: {other.gameObject.name}", this);
            }
            return;
        }
        
        if (hasCollided) return;
        hasCollided = true;
        
        if (debugCollisions)
        {
            Debug.Log($"Bullet triggered with {other.gameObject.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}", this);
        }
        
        // Optionally spawn a hit effect at the trigger point
        if (hitEffectPrefab != null)
        {
            // Just use the current position since we don't have contact points with triggers
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Destroy the bullet
        Destroy(gameObject);
    }
    
    private bool ShouldIgnoreCollision(GameObject other)
    {
        if (shooter == null) return false;
        
        // Check if we're hitting the shooter (or its children)
        bool isShooterOrChild = (other == shooter || other.transform.IsChildOf(shooter.transform));
        
        // Check if we're still within the ignore time window
        bool withinIgnoreTime = (Time.time - creationTime) < ignoreShooterTime;
        
        // Fast check using reference comparison
        if (isShooterOrChild && withinIgnoreTime)
        {
            return true;
        }
        
        // Deeper check using component matching (more reliable but slower)
        if (withinIgnoreTime && shooterColliders != null)
        {
            Collider2D[] otherColliders = other.GetComponents<Collider2D>();
            foreach (Collider2D otherCol in otherColliders)
            {
                foreach (Collider2D shooterCol in shooterColliders)
                {
                    if (otherCol == shooterCol)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    // Also check for continuous collisions
    private void FixedUpdate()
    {
        if (hasCollided) return;
        
        // Use a raycast to detect if we would hit something this frame
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.velocity.sqrMagnitude > 0)
        {
            float distance = rb.velocity.magnitude * Time.fixedDeltaTime;
            
            // Get the collider component
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) return;
            
            // Cast the collider in the direction of movement
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position,
                col is CircleCollider2D ? (col as CircleCollider2D).radius * 0.9f : 0.1f,
                rb.velocity.normalized,
                distance,
                collisionLayers
            );
            
            // Ignore shooter in raycasts too
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                // Check if we should ignore this hit
                if (ShouldIgnoreCollision(hit.collider.gameObject))
                {
                    if (debugCollisions)
                    {
                        Debug.Log($"Ignored raycast hit with shooter: {hit.collider.gameObject.name}", this);
                    }
                    return;
                }
                
                if (debugCollisions)
                {
                    Debug.Log($"Bullet detected collision with {hit.collider.gameObject.name} via CircleCast", this);
                    Debug.DrawLine(transform.position, hit.point, Color.yellow, 1f);
                }
                
                // Optionally spawn a hit effect
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                }
                
                hasCollided = true;
                Destroy(gameObject);
            }
        }
    }
}
