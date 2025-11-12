using UnityEngine;


[RequireComponent(typeof(Camera))]
public class CameraAutoResize : MonoBehaviour {
    [SerializeField] float baseHeight = 10f; // The vertical size you want at any aspect ratio
    Camera cam;

    void Start() {
        cam        = GetComponent<Camera>();
        baseHeight = cam.orthographicSize * 2f;
        UpdateCamera();
    }

    void Update() {
        UpdateCamera();
    }

    void UpdateCamera() {
        float aspect = (float)Screen.width / Screen.height;

        // Set orthographic size based on height
        cam.orthographicSize = baseHeight / 2f;
    }
}
