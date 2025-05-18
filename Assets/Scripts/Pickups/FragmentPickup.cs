using UnityEngine;
using TMPro;

public class FragmentPickup : MonoBehaviour
{
    // Static counter for fragments collected
    public static int FragmentsCollected = 0;
    public static int TotalFragments = 4;

    private bool playerInRange = false;
    private float fadeSpeed = 2f;
    private float targetAlpha = 0f;
    
    [SerializeField] private TextMeshProUGUI pressEText;

    void Start()
    {
        // Find the TextMeshProUGUI component if not assigned in inspector
        if (pressEText == null)
        {
            pressEText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (pressEText != null)
        {
            // Start with text fully transparent
            Color color = pressEText.color;
            color.a = 0f;
            pressEText.color = color;
        }
        else
        {
            Debug.LogWarning("FragmentPickup: TextMeshProUGUI component not found in children", this);
        }
        
        // Ensure we have a valid collider that is set as a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("FragmentPickup: Collider2D should be set as a trigger", this);
        }
    }

    void Update()
    {
        // Update text visibility
        if (pressEText != null)
        {
            Color color = pressEText.color;
            color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * fadeSpeed);
            pressEText.color = color;
        }

        // Check for collection
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Increment fragment counter
            FragmentsCollected++;
            Debug.Log($"Fragment collected! Total: {FragmentsCollected}/{TotalFragments}");
            
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            targetAlpha = 1f; // Fade in
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            targetAlpha = 0f; // Fade out   
        }
    }
    
    // Helper method to check if all fragments are collected
    public static bool HasAllFragments()
    {
        return FragmentsCollected >= TotalFragments;
    }
    
    // Helper method to reset fragments (call this when starting a new game)
    public static void ResetFragments()
    {
        FragmentsCollected = 0;
    }
}