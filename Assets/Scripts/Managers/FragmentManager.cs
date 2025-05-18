using UnityEngine;

public class FragmentManager : MonoBehaviour
{
    // Static instance that persists between scenes
    public static FragmentManager Instance { get; private set; }
    
    // Static variables to track fragment state across scenes
    private static int fragmentsCollectedStatic = 0;
    public int FragmentsCollected 
    { 
        get { return fragmentsCollectedStatic; } 
        private set { fragmentsCollectedStatic = value; } 
    }
    
    public int TotalFragments = 4;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CollectFragment()
    {
        FragmentsCollected++;
        Debug.Log($"Fragment collected! Total: {FragmentsCollected}/{TotalFragments}");
        // You can add events or notifications here if needed
    }

    public bool HasAllFragments()
    {
        return FragmentsCollected >= TotalFragments;
    }

    public void ResetFragments()
    {
        FragmentsCollected = 0;
        Debug.Log("Fragments reset to 0");
    }
}
