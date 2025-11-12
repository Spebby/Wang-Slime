using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Random = System.Random;
using SpebbyTools;


namespace Tiles {
    public class WangTileGenerator : MonoBehaviour {
        [SerializeField] GlobalConfig config;
        [SerializeField] WangTileCollection tileset;
        [SerializeField, HideInInspector] Tile tilePrefab;

        [SerializeField, HideInInspector] Material sharedMaterial;
        
        Tile[,] _tileGrid;
        byte[,] _map;
        internal uint _xTiles;
        internal uint _yTiles;

        GameObject _container;
        static Random _rng;

        public Action OnMapUpdate;
        
        void Awake() {
            GenerateNewMap();
        }

        void Update() {
            sharedMaterial.color = config.TileColor;
        }

        public void GenerateNewMap() {
            // find size of screen
            Camera cam = Camera.main;

            // Calculate world-space size of the camera view
            float camHeight = cam!.orthographicSize * 2f;
            float camWidth  = camHeight * cam.aspect;

            // Convert world size into tile counts
            _xTiles = (uint)Mathf.CeilToInt(camWidth / config.TileScale);
            _yTiles = (uint)Mathf.CeilToInt(camHeight / config.TileScale);
            
            
            Debug.Log($"Canvas size: {camWidth}x{camHeight}");
            Debug.Log($"Tile grid: {_xTiles}x{_yTiles} (tile size: {config.TileScale}px)");
            
            _rng = new Random();
            if (_container) DestroyImmediate(_container);
            
            float tileSize = 1f * config.TileScale;
            Vector2 origin = new(
                -_xTiles * tileSize / 2f + tileSize / 2f,
                -_yTiles * tileSize / 2f + tileSize / 2f
            );
            
            _map           = GenerateMap(tileset, _xTiles, _yTiles, config.Porosity);
            _tileGrid      = new Tile[_xTiles, _yTiles];
            _container     = new GameObject("TileContainer") {
                tag = "TileContainer"
            };
            for (int i = 0; i < _xTiles; i++) {
                for (int j = 0; j < _yTiles; j++) {
                    Vector3 pos = new(
                        origin.x + i * tileSize,
                        origin.y + j * tileSize,
                        0f
                    );

                    // Instantiate at the correct position immediately
                    _tileGrid[i,j] = Instantiate(
                        tilePrefab,
                        pos,
                        Quaternion.identity,
                        _container.transform
                    );
                    _tileGrid[i, j].transform.localScale = new Vector3(tileSize, tileSize, tileSize);
                    _tileGrid[i,j].name = $"Tile[{i},{j}]";
                    _tileGrid[i, j].spriteRender.sprite   = tileset.Tiles[_map[i, j]];
                    _tileGrid[i, j].spriteRender.material = sharedMaterial;

                    int x = i;
                    int y = j;
                    _tileGrid[i, j].OnClick += () => {
                        // reg click + mask by one
                        byte newMask = (byte)((_map[x, y] + 1) & 0xf);
                        if (Input.GetKey(KeyCode.LeftShift)) {
                            newMask = TwoBitWangTile.RotateClockwise(_map[x, y]);
                        }
                        
                        RegenerateTileArea(x, y, newMask);
                    };
                }
            }
            
            OnMapUpdate?.Invoke();
        }

        static byte[,] GenerateMap(WangTileCollection tileset, uint width, uint height, float porosity) {
            if (!tileset || tileset.Tiles == null || tileset.BitMatchTiles == null)
                throw new ArgumentException("Tileset is not initialized correctly.");

            byte[,]       grid = new byte[width, height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Directions required = 0;
                    Directions excluded = 0;

                    // South Neighbor
                    if (0 < y) {
                        byte n = grid[x, y - 1];
                        // Does the north tile have a southern edge?
                        if ((n & (byte)Directions.North) != 0) required |= Directions.South;
                        else excluded                                   |= Directions.South;
                    }

                    // West Neighbor
                    if (0 < x) {
                        byte w = grid[x - 1, y];
                        // Does the western tile have an eastern edge?
                        if ((w & (byte)Directions.East) != 0) required |= Directions.West;
                        else excluded                                  |= Directions.West;
                    }

                    // get valid tile from precomputed list
                    List<byte> candidates   = tileset.BitMatchTiles[(byte)required, (byte)excluded];

                    if (candidates == null || candidates.Count == 0) {
                        // fallback to empty tile if no match
                        grid[x, y] = 0;
                        Debug.Log($"No candidates found for {required}, {excluded}");
                        continue;
                    }

                    grid[x, y] = WeightedRandomTile(candidates, porosity, _rng);
                }
            }

            return grid;
        }
      
        static readonly byte[] BIT_COUNT_LUT = {
            0, 1, 1, 2, 1, 2, 2, 3,
            1, 2, 2, 3, 2, 3, 3, 4
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountEdges(byte mask) => BIT_COUNT_LUT[mask & 0x0F];
        // https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel ^ this is sort of thing that would
        // be nice to do, but not necessary given scope.
        
        /// <summary>
        /// Weight based on porosity.
        /// porosity = 0 → solid preference (more edges)
        /// porosity = 1 → air preference (fewer edges)
        /// </summary>
        static float WeightByPorosity(byte mask, float porosity) {
            const float CONTRAST  = 5f;
            float       edgeRatio = CountEdges(mask )/ 4f; // 0 = empty, 1 = full

            // Bias curves — more exaggerated
            float airBias   = MathF.Pow(1f - edgeRatio, CONTRAST);
            float solidBias = MathF.Pow(edgeRatio, CONTRAST);

            // Blend based on porosity
            float bias = Mathf.Lerp(solidBias, airBias, porosity);
            return 1f + bias * 10f;
        }


        static byte WeightedRandomTile(List<byte> candidates, float porosity, Random rng) {
            float totalWeight                         = 0f;
            foreach (byte c in candidates) totalWeight += WeightByPorosity(c, porosity);

            float randomValue = (float)(rng.NextDouble() * totalWeight);
            foreach (byte c in candidates) {
                randomValue -= WeightByPorosity(c, porosity);
                if (randomValue <= 0f) return c;
            }
            
            return candidates[0]; // fallback
        }

        void RegenerateTileArea(int x, int y, byte newMask) {
            // update original tile
            _map[x, y] = newMask;
            _tileGrid[x, y].spriteRender.sprite = tileset.Tiles[_map[x, y]];

            // update west
            if (0 < x) {
                byte mask = RegenerateTile(x - 1, y);
                _map[x - 1, y] = mask;
                _tileGrid[x - 1, y].spriteRender.sprite = tileset.Tiles[mask];
            }

            // east
            if (x < _xTiles - 1) {
                byte mask = RegenerateTile(x + 1, y);
                _map[x + 1, y] = mask;
                _tileGrid[x + 1, y].spriteRender.sprite = tileset.Tiles[mask];
            }
            
            // south
            if (0 < y) {
                byte mask = RegenerateTile(x, y - 1);
                _map[x, y - 1] = mask;
                _tileGrid[x, y - 1].spriteRender.sprite = tileset.Tiles[mask];
            }
            
            // north
            if (y < _yTiles - 1) {
                byte mask = RegenerateTile(x, y + 1);
                _map[x, y + 1] = mask;
                _tileGrid[x, y + 1].spriteRender.sprite = tileset.Tiles[mask];
            }
            
            OnMapUpdate?.Invoke();
        }

        byte RegenerateTile(int x, int y) {
            int required = 0;
            int excluded = 0;
            
            // North neighbor
            if (y < _yTiles - 1) {
                byte n        = _map[x, y + 1];
                bool hasSouth = (n & (byte)Directions.South) != 0;
                if (hasSouth) required |= (byte)Directions.North;
                else excluded          |= (byte)Directions.North;
            }

            // East neighbor
            if (x < _xTiles - 1) {
                byte e       = _map[x + 1, y];
                bool hasWest = (e & (byte)Directions.West) != 0;
                if (hasWest) required |= (byte)Directions.East;
                else excluded         |= (byte)Directions.East;
            }

            // South neighbor
            if (0 < y) {
                byte s        = _map[x, y - 1];
                bool hasNorth = (s & (byte)Directions.North) != 0;
                if (hasNorth) required |= (byte)Directions.South;
                else excluded          |= (byte)Directions.South;
            }

            // West neighbor
            if (0 < x) {
                byte w       = _map[x - 1, y];
                bool hasEast = (w & (byte)Directions.East) != 0;
                if (hasEast) required |= (byte)Directions.West;
                else excluded         |= (byte)Directions.West;
            }
            

            List<byte> candidates = tileset.BitMatchTiles[required, excluded];
            return WeightedRandomTile(candidates, config.Porosity, _rng);
        }

        static string GetWeightsString(float porosity) {
            StringBuilder sb = new();
            sb.AppendLine($"Porosity = {porosity:F2}");
            sb.AppendLine("Mask\tEdges\tWeight");

            for (byte mask = 0; mask < 16; mask++) {
                int   edges  = CountEdges(mask);
                float weight = WeightByPorosity(mask, porosity);
                sb.AppendLine($"{mask,2}\t{edges,2}\t{weight:F3}");
            }

            return sb.ToString();
        }
        
        public Texture2D BakeTexture(string path = "") {
            Texture2D tex = WangTextureBaker.BakeSolidMask(tileset, _map, config.TileScale);
            
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(path)) return tex;
            byte[] pngData  = tex.EncodeToPNG();
            string fullPath = Path.Combine(path, "DebugBake.png");
            File.WriteAllBytes(fullPath, pngData);
#endif
            return tex;
        }
        
        
        [Button("GenerateNewMap"),   SerializeField] bool RegenerateMap;
        [Button("BakeTextureTest"),  SerializeField] bool BakeMapTexture;
        [Button("PrintTestWeights"), SerializeField] bool TestWeightsFunc;
        // ReSharper disable once UnusedMember.Local
        void PrintTestWeights() => Debug.Log(GetWeightsString(config.Porosity));
        // ReSharper disable once UnusedMember.Local
        void BakeTextureTest() => BakeTexture(Application.dataPath);
    }
}