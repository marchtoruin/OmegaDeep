using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelmetBubbleEmitter : MonoBehaviour
{
    [Header("Bubble Emission Settings")]
    [SerializeField] private int burstCount = 4;     // Number of particles in each burst
    [SerializeField] private float minInterval = 2f; // Minimum time between bursts
    [SerializeField] private float maxInterval = 4f; // Maximum time between bursts

    [Header("Direction Settings")]
    [SerializeField] private Vector3 rightPosition = new Vector3(0, 0, 0);  // Emission position when facing right
    [SerializeField] private Vector3 leftPosition = new Vector3(0, 0, 0);   // Emission position when facing left
    [SerializeField] private bool debugMode = false;  // Show debug visuals and log messages

    [Header("Optional")]
    [SerializeField] private bool emitOnStart = true;        // Start emitting automatically
    [SerializeField] private bool showDebugInfo = false;     // Print debug information

    private ParticleSystem particleSystem;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private Coroutine emissionCoroutine;
    private bool isFacingRight = true;
    private bool isInitialized = false;
    private Transform parentTransform;

    private void Awake()
    {
        // Get the ParticleSystem component
        particleSystem = GetComponent<ParticleSystem>();
        
        // Get parent transform for reference
        parentTransform = transform.parent;
        
        // Verify we have a particle system
        if (particleSystem == null)
        {
            Debug.LogError("HelmetBubbleEmitter: No ParticleSystem component found on this GameObject!", this);
            return;
        }

        // Initialize modules
        shapeModule = particleSystem.shape;
        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;
        
        // Must use world space for particles to stay in place when player moves
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Zero out the initial velocity to prevent particles from inheriting player velocity
        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = 0;
        velocity.y = 0;
        velocity.z = 0;
        
        isInitialized = true;
        
        // Apply initial direction
        SetFacingDirection(true); // Default to facing right
    }

    private void Start()
    {
        if (emitOnStart && particleSystem != null)
        {
            StartEmitting();
        }
    }
    
    // For visualizing the emission points in the editor
    private void OnDrawGizmos()
    {
        if (!debugMode) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position + rightPosition, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + leftPosition, 0.1f);
    }

    /// <summary>
    /// Set which direction the player is facing
    /// </summary>
    /// <param name="facingRight">True if facing right, false if facing left</param>
    public void SetFacingDirection(bool facingRight)
    {
        if (!isInitialized || particleSystem == null) return;
        
        // Store the facing direction
        isFacingRight = facingRight;
        
        // Apply the correct emission settings based on direction
        Vector3 emissionPos = isFacingRight ? rightPosition : leftPosition;
        shapeModule.position = emissionPos;
        
        if (showDebugInfo)
        {
            Debug.Log($"HelmetBubbleEmitter: Player is now facing {(isFacingRight ? "RIGHT" : "LEFT")}, " +
                      $"Position set to {shapeModule.position}", this);
        }
    }

    /// <summary>
    /// Begin the emission coroutine if it's not already running
    /// </summary>
    public void StartEmitting()
    {
        if (emissionCoroutine == null)
        {
            emissionCoroutine = StartCoroutine(EmitBubbles());
            
            if (showDebugInfo)
            {
                Debug.Log($"HelmetBubbleEmitter: Started emitting bubbles. Burst Count: {burstCount}, Interval: {minInterval}-{maxInterval}s", this);
            }
        }
    }

    /// <summary>
    /// Stop the emission coroutine if it's running
    /// </summary>
    public void StopEmitting()
    {
        if (emissionCoroutine != null)
        {
            StopCoroutine(emissionCoroutine);
            emissionCoroutine = null;
            
            if (showDebugInfo)
            {
                Debug.Log("HelmetBubbleEmitter: Stopped emitting bubbles", this);
            }
        }
    }

    /// <summary>
    /// Coroutine that handles the periodic emission of particle bursts
    /// </summary>
    private IEnumerator EmitBubbles()
    {
        while (true)
        {
            // Wait a random time between the min and max interval
            float interval = Random.Range(minInterval, maxInterval);
            
            if (showDebugInfo)
            {
                Debug.Log($"HelmetBubbleEmitter: Waiting {interval:F2} seconds until next burst", this);
            }
            
            yield return new WaitForSeconds(interval);
            
            // Emit a burst of particles at the world position
            if (particleSystem != null)
            {
                // For world space emission, we need to get the current world position
                Vector3 localOffset = isFacingRight ? rightPosition : leftPosition;
                Vector3 worldEmitPosition = transform.TransformPoint(localOffset);
                
                // Create particles at the exact world position
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = worldEmitPosition;
                emitParams.applyShapeToPosition = false; // Ignore shape position and use our position directly
                
                particleSystem.Emit(emitParams, burstCount);
                
                if (showDebugInfo)
                {
                    Debug.Log($"HelmetBubbleEmitter: Emitted burst of {burstCount} particles at position {worldEmitPosition}", this);
                }
            }
        }
    }
    
    /// <summary>
    /// Immediately emit a single burst of particles
    /// </summary>
    public void EmitBurst()
    {
        if (particleSystem != null)
        {
            // For world space emission, we need to get the current world position
            Vector3 localOffset = isFacingRight ? rightPosition : leftPosition;
            Vector3 worldEmitPosition = transform.TransformPoint(localOffset);
            
            // Create particles at the exact world position
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = worldEmitPosition;
            emitParams.applyShapeToPosition = false; // Ignore shape position and use our position directly
            
            particleSystem.Emit(emitParams, burstCount);
            
            if (showDebugInfo)
            {
                Debug.Log($"HelmetBubbleEmitter: Manually emitted burst of {burstCount} particles at position {worldEmitPosition}", this);
            }
        }
    }
    
    private void OnDisable()
    {
        // Make sure to stop the coroutine if the GameObject is disabled
        StopEmitting();
    }
}
