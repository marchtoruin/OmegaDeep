using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiverShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private float fireRate = 0.25f; // Time between shots in seconds
    [SerializeField] private bool canAutoFire = false; // Hold mouse to fire continuously

    [Header("References")]
    [SerializeField] private SpriteRenderer playerSprite; // Reference to the player's sprite renderer
    [SerializeField] private ArmAim armAimScript; // Reference to the ArmAim script to check facing direction
    [SerializeField] private GameObject shooterRoot; // Reference to the player GameObject (to ignore collisions)

    // Audio settings - optional
    [Header("Audio Settings")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float shootVolume = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool wasFlipped = false; // Track the last flip state
    
    // Store original positions like the flashlight does
    private Vector3 originalFirePointLocalPosition;
    private Quaternion originalFirePointLocalRotation;

    private PlayerOxygen playerOxygen;

    private void Awake()
    {
        // Get or add AudioSource if we have shoot sounds
        if (shootSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Store the original fire point position/rotation if it exists
        if (firePoint != null)
        {
            originalFirePointLocalPosition = firePoint.localPosition;
            originalFirePointLocalRotation = firePoint.localRotation;
            
            if (showDebugInfo)
            {
                Debug.Log($"DiverShooter: Stored original FirePoint position: {originalFirePointLocalPosition}, rotation: {originalFirePointLocalRotation.eulerAngles}");
            }
        }
        
        // Auto-find root player object if not set
        if (shooterRoot == null)
        {
            shooterRoot = transform.root.gameObject;
            if (showDebugInfo)
            {
                Debug.Log($"DiverShooter: Auto-assigned shooterRoot to {shooterRoot.name}");
            }
        }

        // Attempt initial find - might be null if PlayerOxygen isn't ready
        playerOxygen = GetComponentInParent<PlayerOxygen>(); 
    }

    // Added Update back for input checking
    private void Update()
    {
       // Check if we can fire
       bool fireInput = canAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
       
       if (fireInput)
       {
           Fire(); // Attempt to fire, Fire() handles cooldown/oxygen
       }
    }

    private void LateUpdate()
    {
        // Update firePoint position based on player flip state
        // This follows the same approach as FlashlightController
        bool isFlipped = DeterminePlayerFlip();
        
        // Only update if the flip state has changed to avoid constant reassignment
        if (isFlipped != wasFlipped)
        {
            wasFlipped = isFlipped;
            
            if (firePoint != null)
            {
                if (isFlipped)
                {
                    // Flip the fire point position and adjust rotation if needed
                    firePoint.localPosition = new Vector3(-originalFirePointLocalPosition.x, 
                                                         originalFirePointLocalPosition.y, 
                                                         originalFirePointLocalPosition.z);
                    
                    // Also flip rotation to match
                    firePoint.localRotation = Quaternion.Euler(
                        originalFirePointLocalRotation.eulerAngles.x,
                        originalFirePointLocalRotation.eulerAngles.y + 180f, 
                        originalFirePointLocalRotation.eulerAngles.z);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"DiverShooter: Flipping FirePoint to LEFT. Position: {firePoint.localPosition}, Rotation: {firePoint.localRotation.eulerAngles}");
                    }
                }
                else
                {
                    // Restore original position and rotation
                    firePoint.localPosition = originalFirePointLocalPosition;
                    firePoint.localRotation = originalFirePointLocalRotation;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"DiverShooter: Flipping FirePoint to RIGHT. Position: {firePoint.localPosition}, Rotation: {firePoint.localRotation.eulerAngles}");
                    }
                }
            }
        }
    }

    // Kept Fire() public, but could be private again if nothing else calls it.
    // Kept oxygen/cooldown checks within Fire().
    public bool Fire() // Return bool indicating if fire was successful (passed cooldown)
    {
        // Check cooldown first
        if (Time.time < nextFireTime)
        {
            return false; // Cooldown active
        }

        // --- Ensure PlayerOxygen Reference --- 
        // If reference is null, try to find it again
        if (playerOxygen == null)
        {
             playerOxygen = GetComponentInParent<PlayerOxygen>();
             if (playerOxygen == null) 
             {
                 // Still couldn't find it
                 Debug.LogError("DiverShooter: Cannot find PlayerOxygen component in parent hierarchy! Oxygen check/drain will fail.", this);
                 // Optionally prevent firing entirely if oxygen is required
                 // return false; 
             }
        }
        // --- End Ensure Reference ---

        // Check oxygen before firing
        if (playerOxygen != null && playerOxygen.GetCurrentOxygen() <= 0f)
        {
            // Optionally play an empty click sound?
            if (showDebugInfo) Debug.Log("DiverShooter: Cannot fire, no oxygen!", this);
            return false;
        }

        // Safety checks
        if (bulletPrefab == null || firePoint == null) 
        {
             if (showDebugInfo) Debug.LogWarning("DiverShooter: Firing prevented - Bullet Prefab or Fire Point not assigned.", this);
             return false;
        }

        // Deduct oxygen *before* firing to prevent firing with exactly 0
        if (playerOxygen != null)
        {
            float oxygenBefore = playerOxygen.GetCurrentOxygen(); // Log before
            playerOxygen.DrainOxygenForShot();
            float oxygenAfter = playerOxygen.GetCurrentOxygen(); // Log after
            if (showDebugInfo) Debug.Log($"[DiverShooter] Attempted O2 drain for shot. Before: {oxygenBefore}, After: {oxygenAfter}", this);

            // Double-check after draining (in case shootDrain makes it exactly zero)
            if (oxygenAfter <= 0f && playerOxygen.GetMaxOxygen() > 0) // Check max > 0 prevents drain on instant death
            {
                 // Don't actually fire if the shot drained the last bit
                 if (showDebugInfo) Debug.Log("DiverShooter: Firing prevented, shot drained last oxygen.", this);
                 // Optionally undo the drain? Or just let it be zero.
                 return false;
            }
        }
        else 
        { 
             // Log if we fired without oxygen component
             if (showDebugInfo) Debug.LogWarning("DiverShooter: Fired without PlayerOxygen component reference.", this);
        }

        // Determine if the player is flipped
        bool isFlipped = DeterminePlayerFlip();

        // Get the position and rotation for the bullet
        Vector3 spawnPosition = firePoint.position;
        Quaternion bulletRotation = firePoint.rotation;
        
        // Create the bullet
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, bulletRotation);

        // Tell the bullet who fired it so it can ignore collisions
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null && shooterRoot != null)
        {
            bulletComponent.SetShooter(shooterRoot);
            
            if (showDebugInfo)
            {
                Debug.Log($"DiverShooter: Told bullet that {shooterRoot.name} is the shooter");
            }
        }

        // Get the bullet's Rigidbody2D
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Get direction from firePoint's right vector (already adjusted by our LateUpdate position flipping)
            Vector2 direction = firePoint.right;
            
            // Apply velocity to the bullet
            rb.velocity = direction * bulletSpeed;
            
            if (showDebugInfo)
            {
                Debug.Log($"DiverShooter: Fired bullet at position {spawnPosition} with velocity: {rb.velocity}, isFlipped: {isFlipped}, rotation: {bulletRotation.eulerAngles}");
            }
        }
        else
        {
            Debug.LogWarning("DiverShooter: Bullet prefab doesn't have a Rigidbody2D component!", this);
        }

        // Play shoot sound if available
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }

        // Destroy the bullet after a certain time
        Destroy(bullet, bulletLifetime);
        
        // Optionally draw a debug ray to visualize the bullet path
        if (showDebugInfo)
        {
            Debug.DrawRay(spawnPosition, firePoint.right * 2f, Color.red, 0.5f);
        }

        // Update next fire time
        nextFireTime = Time.time + fireRate;
        return true; // Fired successfully
    }

    // Determine if the player sprite is flipped
    private bool DeterminePlayerFlip()
    {
        bool isFlipped = false;
        
        // First try to determine flip state from the sprite renderer
        if (playerSprite != null)
        {
            isFlipped = playerSprite.flipX;
        }
        // Fallback to ArmAim script if sprite reference is missing or not working
        else if (armAimScript != null)
        {
            isFlipped = !armAimScript.IsFacingRight;
        }
        
        return isFlipped;
    }

    // Optional: Method to visualize the fire point in the editor
    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
            
            // Draw direction ray based on firePoint's current direction
            Gizmos.DrawRay(firePoint.position, firePoint.right * 0.5f);
        }
    }
}
