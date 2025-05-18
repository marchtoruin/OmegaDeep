using UnityEngine;

public class CollisionLogger : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Collision entered: {collision.gameObject.name}");
    }
} 