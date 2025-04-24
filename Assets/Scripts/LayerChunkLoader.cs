using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[AddComponentMenu("Custom/LayerChunkLoader")]
public class LayerChunkLoader : MonoBehaviour
{
    [Header("Grid Setup")]
    [Tooltip("Name of the Resources subfolder holding your slice PNGs")]
    public string resourceFolder;    // e.g. "Map1_Background" or "Map1_Collision"
    [Tooltip("Columns (X) and Rows (Y) in your grid")]
    public int cols = 4, rows = 6;
    [Tooltip("World‑units per chunk (e.g. sliceSizePx ÷ PPU)")]
    public float chunkSize = 20.48f;

    [Header("Spawner")]
    [Tooltip("Prefab with a SpriteRenderer and/or PolygonCollider2D")]
    public GameObject chunkPrefab;

    [Header("Render Order (sprite layers)")]
    public string sortingLayer = "Default";
    public int    sortingOrder = 0;

    [Header("Camera & Padding")]
    [Tooltip("Your Orthographic camera (optional in Edit mode)")]
    public Camera mainCamera;
    [Tooltip("Extra chunks beyond the camera's view to preload")]
    public int    buffer = 1;

    // internal state
    Sprite[]                           slices;
    Dictionary<Vector2Int, GameObject> active       = new();
    Vector3                            originOffset;

    // Add a flag to defer refresh in edit mode
    private bool needsRefresh = false;

    void Awake()
    {
        // Clean up any existing children (chunks) to avoid leftovers
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        active.Clear();
        LoadSpritesAndComputeOrigin();
    #if UNITY_EDITOR
        RefreshChunks();
    #endif
    }

    // Called whenever you change a value in the Inspector
    void OnValidate()
    {
        LoadSpritesAndComputeOrigin();
        // Instead of calling RefreshChunks() directly, set a flag
        if (!Application.isPlaying)
            needsRefresh = true;
    }

    // In Play mode, keep chunks around the camera streaming
    void Update()
    {
        if (Application.isPlaying)
        {
            RefreshChunks();
        }
        else if (needsRefresh)
        {
            RefreshChunks();
            needsRefresh = false;
        }
    }

    // Load & sort your slice sprites, compute centering offset
    void LoadSpritesAndComputeOrigin()
    {
        slices = Resources
            .LoadAll<Sprite>(resourceFolder)
            .OrderBy(s => s.name)
            .ToArray();

        if (slices.Length == 0)
            Debug.LogError($"[LayerChunkLoader] No sprites found in Resources/{resourceFolder}");

        float mapW = cols * chunkSize;
        float mapH = rows * chunkSize;
        originOffset = new Vector3(
            -mapW / 2f + chunkSize / 2f,
             mapH / 2f - chunkSize / 2f,
            0
        );
    }

    // Clear old chunks & instantiate new ones
    void RefreshChunks()
    {
        // 1) Clear existing
        foreach (var kv in active)
            if (kv.Value) {
                if (Application.isPlaying)
                    Destroy(kv.Value);
                else
                    DestroyImmediate(kv.Value);
            }
        active.Clear();

        // Also destroy any leftover children
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // 2) Decide which coords to load
        var needed = new HashSet<Vector2Int>();

        if (!Application.isPlaying)
        {
            // EDIT MODE: load everything
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    needed.Add(new Vector2Int(x, y));
        }
        else
        {
            // PLAY MODE: only around camera
            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null) return;

            Vector3 camLocal = cam.transform.position - originOffset;
            float camH = 2f * cam.orthographicSize;
            float camW = camH * cam.aspect;

            float left   = camLocal.x - camW/2f;
            float right  = camLocal.x + camW/2f;
            float top    = -(camLocal.y - camH/2f);
            float bottom = -(camLocal.y + camH/2f);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(left   / chunkSize) - buffer, 0, cols-1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt(right  / chunkSize) + buffer, 0, cols-1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(bottom / chunkSize) - buffer, 0, rows-1);
            int y1 = Mathf.Clamp(Mathf.FloorToInt(top    / chunkSize) + buffer, 0, rows-1);

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    needed.Add(new Vector2Int(x, y));
        }

        // 3) Instantiate all needed chunks
        foreach (var coord in needed)
        {
            int idx = coord.y * cols + coord.x;
            if (idx < 0 || idx >= slices.Length) continue;

            Vector3 worldPos = originOffset + new Vector3(
                coord.x * chunkSize,
               -coord.y * chunkSize,
                0
            );

            GameObject go;
        #if UNITY_EDITOR
            if (Application.isPlaying)
                go = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
            else
                go = (GameObject)PrefabUtility.InstantiatePrefab(chunkPrefab, transform);
        #else
            go = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
        #endif
            go.transform.position = worldPos;
            // Force Z=0 in case of prefab/Unity weirdness
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 0f);
            Debug.Log($"[LayerChunkLoader] {go.name} set to {go.transform.position}", go);

            // Assign sprite & sorting
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite           = slices[idx];
                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder     = sortingOrder;
                Debug.Log($"[LayerChunkLoader] {go.name} SpriteRenderer set to sortingLayer='{sr.sortingLayerName}', order={sr.sortingOrder}", go);
            }

            // Copy physics shape if there's a PolygonCollider2D
            var pc = go.GetComponent<PolygonCollider2D>();
            if (pc != null && sr != null)
                CopyPhysicsShape(sr.sprite, pc);

            active[coord] = go;
        }
    }

    // Helper: copy the sprite's baked Physics Shape into the collider
    void CopyPhysicsShape(Sprite sprite, PolygonCollider2D pc)
    {
        int shapeCount = sprite.GetPhysicsShapeCount();
        pc.pathCount = shapeCount;
        for (int i = 0; i < shapeCount; i++)
        {
            var path = new List<Vector2>();
            sprite.GetPhysicsShape(i, path);
            pc.SetPath(i, path.ToArray());
        }
    }
}
