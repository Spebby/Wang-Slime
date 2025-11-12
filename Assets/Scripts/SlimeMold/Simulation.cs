using System;
using System.Diagnostics.CodeAnalysis;
using SpebbyTools;
using UnityEngine;
using Tiles;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace SlimeMold {
    public class Simulation : MonoBehaviour {
        #region Shader Properties
        // Global properties
        static readonly int MASK = Shader.PropertyToID("_Mask");
        static readonly int WIDTH = Shader.PropertyToID("_Width");
        static readonly int HEIGHT = Shader.PropertyToID("_Height");
        static readonly int NUM_AGENTS = Shader.PropertyToID("_numAgents");
        static readonly int AGENTS = Shader.PropertyToID("_agents");
        
        // Collection properties
        static readonly int VALID_POSITIONS = Shader.PropertyToID("_ValidPositions");
        
        // Draw/Sim properties
        static readonly int TARGET_TEXTURE = Shader.PropertyToID("_TargetTexture");
        static readonly int TRAIL_MAP = Shader.PropertyToID("_TrailMap");
        static readonly int DIFFUSE_MAP = Shader.PropertyToID("_DiffuseMap");
        static readonly int DELTA_TIME = Shader.PropertyToID("_deltaTime");
        static readonly int TIME = Shader.PropertyToID("_time");
        static readonly int TRAIL_WEIGHT = Shader.PropertyToID("_trailWeight");
        static readonly int DECAY_RATE = Shader.PropertyToID("_decayRate");
        static readonly int DIFFUSE_RATE = Shader.PropertyToID("_diffuseRate");
        
        static readonly int MOVE_SPEED = Shader.PropertyToID("_moveSpeed");
        static readonly int TURN_SPEED = Shader.PropertyToID("_turnSpeed");
        static readonly int SENSOR_ANGLE_DEGREES = Shader.PropertyToID("_sensorAngleDegrees");
        static readonly int SENSOR_OFFSET_DST = Shader.PropertyToID("_sensorOffsetDst");
        static readonly int SENSOR_SIZE = Shader.PropertyToID("_sensorSize");
        #endregion
        WangTileGenerator _generator;

        [SerializeField] internal GlobalConfig config;
        [SerializeField, HideInInspector] ComputeShader spawnMask;
        [SerializeField, HideInInspector] ComputeShader sim;
        [SerializeField, HideInInspector] ComputeShader drawAgents;
        
        RenderTexture trailMap;
        RenderTexture diffusedMap;
        RenderTexture displayTexture;
        
        ComputeBuffer _agentBuffer;
        CommandBuffer _cmd;
        // ReSharper disable InconsistentNaming
        static int COLLECT_KERNEL, SIM_UPDATE_KERNEL, SIM_DIFFUSE_KERNEL, DRAW_KERNEL;
        // ReSharper restore InconsistentNaming

        bool _started;
        int _agentCount, _width, _height;

        void Awake() {
            _generator         = FindFirstObjectByType<WangTileGenerator>();
            _cmd               = new CommandBuffer();
            COLLECT_KERNEL     = spawnMask.FindKernel("Collect");
            SIM_UPDATE_KERNEL  = sim.FindKernel("Update");
            SIM_DIFFUSE_KERNEL = sim.FindKernel("Diffuse");
            DRAW_KERNEL        = drawAgents.FindKernel("Draw");
        }

        [Button("StartSimulation"), SerializeField] bool tester;
        
        
        // Start sim
        // ReSharper disable once UnusedMember.Global
        public void StartSimulation() {
            // clear from previous simulation?
            Release(_agentBuffer);
            
            Texture2D mask = _generator.BakeTexture();
            _width         = mask.width;
            _height        = mask.height;

            int threadGroupsX = Mathf.CeilToInt(_width / 8f);
            int threadGroupsY = Mathf.CeilToInt(_height / 8f);
            int maxValid      = Mathf.NextPowerOfTwo(_width * _height);

            InitRenderTexture(ref trailMap);
            InitRenderTexture(ref diffusedMap);
            InitRenderTexture(ref displayTexture);
            
            MeshRenderer render = transform.GetComponentInChildren<MeshRenderer>();
            render.material.mainTexture = displayTexture;
            render.transform.localScale = new Vector3(_generator._xTiles, _generator._yTiles, 1);
            
            sim.SetTexture(SIM_UPDATE_KERNEL, TRAIL_MAP, trailMap);
            sim.SetTexture(SIM_DIFFUSE_KERNEL, TRAIL_MAP, trailMap);
            sim.SetTexture(SIM_DIFFUSE_KERNEL, DIFFUSE_MAP, diffusedMap);

            
            Shader.SetGlobalTexture(MASK, mask);
            Shader.SetGlobalInt(WIDTH, _width);
            Shader.SetGlobalInt(HEIGHT, _height);
            
            // Bind and dispatch collection routine
            ComputeBuffer validBuffer = new(maxValid, sizeof(int)*2, ComputeBufferType.Append);
            validBuffer.SetCounterValue(0);

            spawnMask.SetBuffer(COLLECT_KERNEL, VALID_POSITIONS, validBuffer);
            spawnMask.Dispatch(COLLECT_KERNEL, threadGroupsX, threadGroupsY, 1);

            // Copy valid positions back to CPU
            ComputeBuffer countBuffer = new(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(validBuffer, countBuffer, 0);

            int[] countArray = new int[1];
            countBuffer.GetData(countArray);
            int validCount = countArray[0];

            Vector2Int[] validPositions = new Vector2Int[validCount];
            validBuffer.GetData(validPositions);
            Release(validBuffer, countBuffer);

            // Shuffle and pick N agents
            System.Random rng = new();
            for (int i = validCount - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                (validPositions[i], validPositions[j]) = (validPositions[j], validPositions[i]);
            }

            Vector2Int[] spawnPositions = new Vector2Int[Mathf.Min(config.MaxAgents, validCount)];
            Array.Copy(validPositions, spawnPositions, spawnPositions.Length);
            
            // we have valid positions, now spawn agents and simulate.
            _agentCount = spawnPositions.Length;
            Shader.SetGlobalInt(NUM_AGENTS, _agentCount);
            
            Agent[] agents = new Agent[_agentCount];
            for (int i = 0; i < _agentCount; i++) {
                agents[i] = new Agent(spawnPositions[i], Random.value * Mathf.PI * 2);
            }

            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent));
            _agentBuffer = new ComputeBuffer(_agentCount, stride);
            _agentBuffer.SetData(agents);
            
            Debug.Log($"Dispatching {_agentCount} agents");
            sim.SetBuffer(SIM_UPDATE_KERNEL, AGENTS, _agentBuffer);
            drawAgents.SetBuffer(DRAW_KERNEL, AGENTS, _agentBuffer);
            
            _started               =  true;
            _generator.OnMapUpdate += UpdateMask;
            return;

            void InitRenderTexture(ref RenderTexture rt) {
                if (rt) {
                    if (rt.IsCreated()) rt.Release();
                    DestroyImmediate(rt);
                }

                rt = new RenderTexture(_width, _height, 0, config.Format) {
                    enableRandomWrite = true,
                    filterMode        = config.FilterMode,
                    wrapMode          = TextureWrapMode.Clamp
                };
                rt.Create();
            }
        }
        
        void Update() {
            if (!_started) return;
            
            for (int i = 0; i < config.StepsPerFrame; i++) {
                // Assign settings
                sim.SetFloat(DELTA_TIME, Time.deltaTime);
                sim.SetFloat(TIME, Time.time);

                sim.SetFloat(TRAIL_WEIGHT, config.trailWeight);
                sim.SetFloat(DECAY_RATE, config.decayRate);
                sim.SetFloat(DIFFUSE_RATE, config.diffuseRate);

                sim.SetFloat(MOVE_SPEED, config.moveSpeed);
                sim.SetFloat(TURN_SPEED, config.turnSpeed);
                sim.SetFloat(SENSOR_ANGLE_DEGREES, config.sensorAngleSpacing);
                sim.SetFloat(SENSOR_OFFSET_DST, config.sensorOffsetDst);
                sim.SetFloat(SENSOR_SIZE, config.sensorSize);

                
                // Calculate Diffusion thread group size
                sim.GetKernelThreadGroupSizes(SIM_DIFFUSE_KERNEL, out uint xSize, out uint ySize, out uint _);
                int groupsX = Mathf.CeilToInt(_width  / (float)xSize);
                int groupsY = Mathf.CeilToInt(_height / (float)ySize);
              
                sim.GetKernelThreadGroupSizes(SIM_UPDATE_KERNEL, out uint updXSize, out uint _, out uint _);
                int agentGroups = Mathf.CeilToInt(_agentCount / (float)updXSize);
                sim.Dispatch(SIM_UPDATE_KERNEL, agentGroups, 1, 1);
                sim.Dispatch(SIM_DIFFUSE_KERNEL, groupsX, groupsY, 1);
                
                // Get Render Textures
                Graphics.Blit(diffusedMap, trailMap);
            }
        }
        
        void LateUpdate() {
            if (!_started) return;

            if (config.ShowAgentsOnly) {
                _cmd.SetRenderTarget(displayTexture);
                _cmd.ClearRenderTarget(true, true, Color.black);
                Graphics.ExecuteCommandBuffer(_cmd);

                {
                    drawAgents.GetKernelThreadGroupSizes(DRAW_KERNEL, out uint xSize, out uint _, out _);
                    int groupsX = Mathf.CeilToInt(_width / (float)xSize);
                    drawAgents.SetTexture(DRAW_KERNEL, TARGET_TEXTURE, displayTexture);
                    drawAgents.Dispatch(DRAW_KERNEL, groupsX, 1, 1);
                }

                return;
            }

            Graphics.Blit(trailMap, displayTexture);
        }

        void UpdateMask() {
            Texture2D mask = _generator.BakeTexture();
            Shader.SetGlobalTexture(MASK, mask);
            _width  = mask.width;
            _height = mask.height;

            Debug.Log("Updt");
        }

        void OnDestroy() {
            Release(_agentBuffer, _cmd);
        }

        static void Release(params IDisposable[] disposables) {
            foreach (IDisposable disposable in disposables) {
                disposable?.Dispose();
            }
        }
    }

    // intermediary to be marshalled to GPU
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public readonly struct Agent {
        public readonly Vector2 Position;
        public readonly float Angle;
        
        public Agent(Vector2 position, float angle) {
            Position     = position;
            Angle        = angle;
        }
    }
}