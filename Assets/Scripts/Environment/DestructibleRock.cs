using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class DestructibleRock : MonoBehaviour
{
    // Unique identifier for this rock (set in inspector or auto-generated)
    [SerializeField] private string rockId;
    
    // Static collection to track destroyed rocks across scene loads
    private static HashSet<string> destroyedRocks = new HashSet<string>();
    
    // Flag to determine if this is the first load since starting play mode
    private static bool isFirstLoad = true;
    
    // Backup persistence using PlayerPrefs (more reliable across domain reloads)
    private const string DESTROYED_ROCKS_KEY = "DestroyedRocksData";

    // Add these variables for integration with your existing system
    [SerializeField] private int health = 6;
    private bool isDestroyed = false;

    // Event to signal when the rock is actually destroyed (health reaches zero)
    [Header("Events")]
    public UnityEvent OnRockActuallyDestroyed;

    private void Awake()
    {
        // Clear static data when game first starts (only in editor)
        if (isFirstLoad)
        {
            isFirstLoad = false;
            
            #if UNITY_EDITOR
            // In editor, clear the list on first load of any rock
            destroyedRocks.Clear();
            PlayerPrefs.DeleteKey(DESTROYED_ROCKS_KEY);
            Debug.Log("DestructibleRock: First load detected, clearing destroyed rocks list");
            #else
            // In builds, load from PlayerPrefs
            LoadDestroyedRocksData();
            #endif
        }
        
        // Auto-generate ID if not set (more robust using name and scene)
        if (string.IsNullOrEmpty(rockId))
        {
            string sceneName = SceneManager.GetActiveScene().name;
            rockId = $"{sceneName}_{gameObject.name}_{transform.GetSiblingIndex()}";
        }
        
        // Debug.Log to see which rocks are being checked
        Debug.Log($"Rock '{rockId}' checking if destroyed. Result: {destroyedRocks.Contains(rockId)}");
        
        CheckIfDestroyed();
    }
    
    // Also check on enable to handle scene reloading
    private void OnEnable()
    {
        // Double check destruction status when enabled
        CheckIfDestroyed();
    }
    
    // Add this to hook into your existing damage system
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;
        
        // Check for bullet collisions (if your bullets use triggers, this won't work)
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(1); // Assuming each bullet does 2 damage based on your logs
        }
    }
    
    // Add this to hook into your existing trigger system for bullets
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;
        
        if (other.CompareTag("Bullet"))
        {
            TakeDamage(1); // Assuming each bullet does 2 damage
        }
    }
    
    // Method to handle damage
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        health -= damage;
        Debug.Log($"Rock '{rockId}' took {damage} damage. Remaining health: {health}");
        
        if (health <= 0 && !isDestroyed)
        {
            DestroyRock();
        }
    }
    
    private void LoadDestroyedRocksData()
    {
        // If we have stored data, load it
        if (PlayerPrefs.HasKey(DESTROYED_ROCKS_KEY))
        {
            string data = PlayerPrefs.GetString(DESTROYED_ROCKS_KEY);
            string[] rockIds = data.Split('|');
            
            // Re-add any rocks from playerprefs that aren't in memory
            foreach (string id in rockIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    destroyedRocks.Add(id);
                }
            }
        }
    }
    
    private void SaveDestroyedRocksData()
    {
        #if !UNITY_EDITOR
        // Only save to PlayerPrefs in builds, not in editor
        string data = string.Join("|", destroyedRocks);
        PlayerPrefs.SetString(DESTROYED_ROCKS_KEY, data);
        PlayerPrefs.Save();
        #endif
    }
    
    private void CheckIfDestroyed()
    {
        if (destroyedRocks.Contains(rockId))
        {
            isDestroyed = true;
            // This rock was previously destroyed, so disable it
            gameObject.SetActive(false);
            Debug.Log($"Rock '{rockId}' was previously destroyed - hiding it now");
        }
    }
    
    // Call this when the rock is destroyed by the player
    public void DestroyRock()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        // Add to the destroyed list
        destroyedRocks.Add(rockId);
        Debug.Log($"Rock '{rockId}' destroyed and added to destroyed list");
        
        // Save to PlayerPrefs for extra persistence
        SaveDestroyedRocksData();
        
        // Invoke the event to signal that the rock has just been destroyed.
        // FMOD or other systems should listen to this specific event
        // instead of relying on OnDisable or SetActive(false) generally.
        OnRockActuallyDestroyed.Invoke();
        
        // Actually destroy or disable the rock
        gameObject.SetActive(false);
    }
    
    // For debugging - show the ID in the editor
    void OnDrawGizmosSelected()
    {
        if (!string.IsNullOrEmpty(rockId))
        {
            UnityEditor.Handles.Label(transform.position, rockId);
        }
    }
    
    // Method to reset all destroyed rocks (for new game)
    public static void ResetDestroyedRocks()
    {
        destroyedRocks.Clear();
        PlayerPrefs.DeleteKey(DESTROYED_ROCKS_KEY);
    }
} 