using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class JellyfishAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 0.5f;         // How fast it drifts
    public float wanderRadius = 3f;        // How far it wanders from its start point
    public float directionChangeInterval = 2f; // How often it picks a new direction
    public float bobAmplitude = 0.1f;      // How much it bobs up and down
    public float bobSpeed = 1f;            // How fast it bobs

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float changeDirectionTimer;
    private float bobTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure dynamic body so physics collisions block movement
        rb.bodyType = RigidbodyType2D.Dynamic;
        startPosition = transform.position;
        PickNewTargetPosition();
        bobTimer = Random.Range(0f, Mathf.PI * 2f); // Randomize bob start
    }

    void FixedUpdate()
    {
        // --- Wandering Movement --- 
        changeDirectionTimer -= Time.fixedDeltaTime;
        if (changeDirectionTimer <= 0)
        {
            PickNewTargetPosition();
        }

        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 wanderVelocity = directionToTarget * moveSpeed;

        // --- Bobbing Movement --- 
        bobTimer += Time.fixedDeltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
        Vector2 bobVelocity = Vector2.up * bobOffset; // Apply bob vertically

        // --- Combine Velocities and apply via physics for collision response ---
        rb.velocity = wanderVelocity + bobVelocity;
    }

    void PickNewTargetPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        targetPosition = startPosition + randomOffset;
        changeDirectionTimer = directionChangeInterval;
    }

    // Optional: Draw gizmos in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
} 