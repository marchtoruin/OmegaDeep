using UnityEngine;

public class DestructibleSpriteCycle : MonoBehaviour
{
    [Header("Visuals & Animator")]
    [Tooltip("The Animator component that controls the damage visuals.")]
    [SerializeField] private Animator animator;
    [Tooltip("The name of the FLOAT parameter in the Animator that controls the normalized time of the damage animation (e.g., \"DamageProgress\").")]
    [SerializeField] private string animatorFloatParameterName = "DamageProgress";
    [SerializeField] private GameObject destructionEffectPrefab;

    [Header("Settings")]
    [Tooltip("How many hits to destroy. The animation clip should have visual states for hits 0 to (maxHits - 1).")]
    [SerializeField] private int maxHits = 6;

    private int currentHitCount = 0;
    // SpriteRenderer is no longer directly controlled by this script if using Animator.
    // It's still good to have if you want to access it for other reasons, but not strictly necessary for damage state changes.
    // private SpriteRenderer spriteRenderer; 

    void Awake()
    {
        // spriteRenderer = GetComponent<SpriteRenderer>(); // Optional: if you need it for other things

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError("DestructibleSpriteCycle requires an Animator component on the same GameObject or assigned in the Inspector.", this);
            enabled = false; // Disable script if no Animator
            return;
        }

        if (string.IsNullOrEmpty(animatorFloatParameterName))
        {
            Debug.LogError("Animator Float Parameter Name is not set in DestructibleSpriteCycle.", this);
            enabled = false;
            return;
        }

        // Set animator speed to 0 so it doesn't play on its own.
        // The script will control the frame by setting the float parameter.
        if (animator != null) animator.speed = 0;

        UpdateAnimatorState();
    }

    public void TakeHit()
    {
        if (currentHitCount >= maxHits)
        {
            return; // Already destroyed or in process
        }

        currentHitCount++;

        if (currentHitCount >= maxHits)
        {
            ProcessDestruction();
        }
        else
        {
            UpdateAnimatorState();
        }
    }

    private void UpdateAnimatorState()
    {
        if (animator != null && !string.IsNullOrEmpty(animatorFloatParameterName))
        {
            // Calculate normalized time. 
            // If maxHits is 6, currentHitCount goes 0 (intact) to 5 (most damaged).
            // This maps 0 to 0.0 and 5 to 1.0.
            float normalizedTime = 0f;
            if (maxHits > 1) // Avoid division by zero if maxHits is 1 or less
            {
                normalizedTime = (float)currentHitCount / (maxHits - 1);
            }
            else if (maxHits == 1) // If it's 1 hit destroy, show the first frame (or last if that's preferred before destruction)
            {
                 normalizedTime = 0f; // Or 1f depending on which frame represents the pre-destruction state for 1 hit.
            }
            
            animator.SetFloat(animatorFloatParameterName, normalizedTime);
        }
    }

    private void ProcessDestruction()
    {
        // Instantiate destruction effect if assigned
        if (destructionEffectPrefab != null)
        { 
            Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
        }

        // Destroy this GameObject
        Destroy(gameObject);
    }
}
