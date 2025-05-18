using UnityEngine;

/// <summary>
/// Parallax effect based on the camera's movement from its starting position.
/// Attach to any object (foreground, midground, etc). Works on both X and Y axes.
/// </summary>
public class PlayerRelativeParallax : MonoBehaviour
{
    [Tooltip("0 = static, 1 = follows camera exactly")] 
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;

    [Tooltip("Name of the camera GameObject (default: Main Camera)")]
    public string cameraObjectName = "Main Camera";

    private Vector3 _startLocalPos;
    private Vector3 _cameraStartPos;
    private Transform _camera;
    private bool _initialized = false;

    void Start()
    {
        // Do nothing here; initialization will happen in LateUpdate after one frame
    }

    void LateUpdate()
    {
        if (!_initialized)
        {
            GameObject camObj = GameObject.Find(cameraObjectName);
            if (camObj == null)
            {
                Debug.LogError($"PlayerRelativeParallax: Could not find camera GameObject named '{cameraObjectName}'", this);
                enabled = false;
                return;
            }
            _camera = camObj.transform;
            _cameraStartPos = _camera.position;
            _startLocalPos = transform.localPosition;
            _initialized = true;
            return; // Skip parallax this frame
        }
        if (_camera == null) return;
        Vector3 camOffset = _camera.position - _cameraStartPos;
        // Only apply X axis (ignore Y and Z)
        float parallaxOffsetX = camOffset.x * parallaxFactor;
        transform.localPosition = new Vector3(_startLocalPos.x + parallaxOffsetX, _startLocalPos.y, _startLocalPos.z);
    }
}
