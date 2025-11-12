using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WorldBackgroundSetter : MonoBehaviour {
    Camera _cam;
    [SerializeField] GlobalConfig config;

    void Awake() {
        _cam = GetComponent<Camera>();
    }
    
    void LateUpdate() {
        _cam.backgroundColor = config.WorldBackground;
    }
}
