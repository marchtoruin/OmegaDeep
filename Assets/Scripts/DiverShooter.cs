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
    }

    private void Start()
    {
        // Validation checks
        if (firePoint == null)
        {
            Debug.LogError("DiverShooter: FirePoint not assigned! Please assign the FirePoint Transform in the inspector.", this);
        }

        if (bulletPrefab == null)
        {
            Debug.LogError("DiverShooter: Bullet prefab not assigned! Please assign a bullet prefab in the inspector.", this);
        }

        // Auto-find references if not set
        if (playerSprite == null)
        {
            // First look for player sprite in the parent hierarchy
            Transform playerTransform = transform.root.Find("Player");
            if (playerTransform != null)
            {
                playerSprite = playerTransform.GetComponent<SpriteRenderer>();
                if (playerSprite == null)
                {
                    // If not found directly on Player, try to find it on a child
                    playerSprite = playerTransform.GetComponentInChildren<SpriteRenderer>();
                }
            }
            
            // If still not found, try a broader search
            if (playerSprite == null)
            {
                Debug.LogWarning("DiverShooter: Player sprite not found in hierarchy - attempting broader search");
                playerSprite = FindObjectOfType<SpriteRenderer>();
            }
            
            if (playerSprite != null && showDebugInfo)
            {
                Debug.Log($"DiverShooter: Auto-found player sprite: {playerSprite.name}");
            }
        }
        
        // Find ArmAim script if not assigned
        if (armAimScript == null)
        {
            armAimScript = GetComponentInParent<ArmAim>();
            if (armAimScript == null)
            {
                armAimScript = FindObjectOfType<ArmAim>();
            }
            if (armAimScript != null && showDebugInfo)
            {
                Debug.Log($"DiverShooter: Auto-found ArmAim script on: {armAimScript.name}");
            }
        }
        
        // Make sure the player has the "Player" tag for bullet collision detection
        if (shooterRoot != null && shooterRoot.tag != "Player")
        {
            shooterRoot.tag = "Player";
            if (showDebugInfo)
            {
                Debug.Log($"DiverShooter: Set {shooterRoot.name} tag to 'Player'");
            }
        }
    }

    private void Update()
    {
        // Check if we can fire
        bool fireInput = canAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        
        if (fireInput && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
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

    private void Fire()
    {
        // Safety checks
        if (bulletPrefab == null || firePoint == null) return;

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

    // Public method to allow other scripts to trigger firing
    public void TriggerFire()
    {
        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
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
