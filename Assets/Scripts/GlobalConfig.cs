using UnityEngine;
using UnityEngine.Experimental.Rendering;


[CreateAssetMenu(fileName = "GlobalConfig", menuName = "Scriptable Objects/GlobalConfig")]
public class GlobalConfig : ScriptableObject {
    public Color WorldBackground = Color.black;
    
    [Header("Slime Simulation")]
    public int StepsPerFrame = 1;
    public int MaxAgents = 1024;
    public Color SlimeColor = Color.white;
    public FilterMode FilterMode = FilterMode.Point;
    public GraphicsFormat Format = GraphicsFormat.R16G16B16A16_SFloat;
    public bool ShowAgentsOnly = false;
    
    [Header("Slime Agent Settings")]
    public float trailWeight = 1f;
    public float decayRate = 1;
    public float diffuseRate = 1;
    [Min(0)] public float moveSpeed = 10f;
    [Min(0)] public float turnSpeed = 5f;
    [Min(0)] public float sensorAngleSpacing = 30f;
    [Min(0)] public float sensorOffsetDst = 35;
    [Min(1)] public int sensorSize = 1;
    
    
    [Header("Wang Tiles")]
    public Color TileColor = Color.grey;
    [Range(0, 1f)] public float Porosity;
    [Range(1, 4)] public int TileScale = 1;
}
