using UnityEngine;

public class PlayerBoundsClamp : MonoBehaviour
{
    [Tooltip("Reference to the WorldBounds BoxCollider2D")]
    public BoxCollider2D worldBoundsCollider;

    void Start()
    {
        if (worldBoundsCollider == null)
        {
            GameObject wb = GameObject.Find("WorldBounds");
            if (wb != null)
                worldBoundsCollider = wb.GetComponent<BoxCollider2D>();
        }
    }

    void LateUpdate()
    {
        if (worldBoundsCollider == null) return;

        Bounds bounds = worldBoundsCollider.bounds;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
        transform.position = pos;
    }
} 