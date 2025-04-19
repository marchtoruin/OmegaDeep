using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public int segments = 100;
    public float width = 300f;
    public float amplitude = 0.5f;
    public float speed = 3f;
    public float baselineY = 1f;

    [Header("Noise Settings")]
    public float noiseAmplitude = 0.2f;
    public float noiseScale = 0.5f;
    public float noiseSpeed = 0.5f;

    private LineRenderer lineRenderer;
    private MeshFilter meshFilter;
    private Vector3[] points;
    private Mesh mesh;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "WaterMeshBaked";
        meshFilter.mesh = mesh;

        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.generateLightingData = true;

        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = "Water";

        points = new Vector3[segments + 1];
        lineRenderer.positionCount = points.Length;
    }

    void Update()
    {
        float dt = Time.time * speed;
        float dx = width / segments;
        for (int i = 0; i <= segments; i++)
        {
            float x = i * dx;
            float sine = Mathf.Sin(dt + i * Mathf.PI * 2f / segments) * amplitude;
            float noise = (Mathf.PerlinNoise(i * noiseScale, Time.time * noiseSpeed) - 0.5f) * 2f * noiseAmplitude;
            float y = baselineY + sine + noise;
            points[i] = new Vector3(x, y, 0f);
        }
        lineRenderer.SetPositions(points);

        lineRenderer.BakeMesh(mesh, Camera.main, true);

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Baked mesh: {mesh.vertexCount} verts, {mesh.triangles.Length} tris. Bounds: {mesh.bounds}");
        }
    }

    public float GetSurfaceHeight(float worldX)
    {
        float localX = worldX - transform.position.x;
        localX = Mathf.Clamp(localX, 0f, width);

        float t = localX / width * segments;
        int i = Mathf.FloorToInt(t);
        float frac = t - i;

        int index0 = Mathf.Min(i, segments);
        int index1 = Mathf.Min(i + 1, segments);

        float y0 = points[index0].y;
        float y1 = points[index1].y;

        return transform.position.y + Mathf.Lerp(y0, y1, frac);
    }
} 