using UnityEngine;

public class LayerParallax : MonoBehaviour {
    [Tooltip("0 = static, 1 = locks exactly with camera")]
    [Range(0f,1f)]
    public float parallaxFactor = 0.5f;

    Vector3 _startLocalPos;
    Vector3 _startCamPos;
    Transform _cam;

    void Start() {
        _cam = Camera.main.transform;
        // record your offset relative to the chunk parent
        _startLocalPos = transform.localPosition;
        // record camera pos when scene begins
        _startCamPos   = _cam.position;
    }

    void LateUpdate() {
        // how far the camera moved since Start
        Vector3 camOffset = _cam.position - _startCamPos;
        // apply as a local shift on top of the chunk parent’s position
        transform.localPosition = _startLocalPos + camOffset * parallaxFactor;
    }
}
