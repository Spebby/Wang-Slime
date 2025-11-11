using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = System.Random;
using SpebbyTools;
using UnityEditor;


namespace Tiles {
    public class WangTileGenerator : MonoBehaviour {
        [SerializeField] WangTileCollection tileset;
        [SerializeField] Tile tilePrefab;
        [Range(1, 128)] public int pixelsPerUnit = 32;
        [SerializeField] public Color tint = Color.white;
        
        Tile[,] _tileGrid;
        byte[,] _map;
        
        
        // internal
        Vector2Int _canvasSize;

        uint _xTiles;
        uint _yTiles;

        GameObject _container;
        static Random _rng;

        
        [Button("GenerateNewMap"),  SerializeField] bool RegenerateMap; // dummy var
        [Button("BakeTexture"),  SerializeField] bool BakeMapTexture; // dummy var
        
        void Awake() {
            GenerateNewMap();
        }

        void GenerateNewMap() {
            // find size of screen
            _canvasSize                   = new Vector2Int(Screen.width, Screen.height);
            _xTiles = (uint)Mathf.CeilToInt((float)_canvasSize.x / pixelsPerUnit);
            _yTiles = (uint)Mathf.CeilToInt((float)_canvasSize.y / pixelsPerUnit);
            
            Debug.Log($"Canvas size: {_canvasSize.x}x{_canvasSize.y}");
            Debug.Log($"Tile grid: {_xTiles}x{_yTiles} (tile size: {pixelsPerUnit}px)");
            
            _rng = new Random();
            if (_container) Destroy(_container);
            
            const float TILE_SIZE = 1f;
            Vector2 origin = new(
                -_xTiles * TILE_SIZE / 2f + TILE_SIZE / 2f,
                -_yTiles * TILE_SIZE / 2f + TILE_SIZE / 2f
            );
            
            _map      = GenerateMap(tileset, _xTiles, _yTiles);
            _tileGrid = new Tile[_xTiles, _yTiles];
            _container = new GameObject("TileContainer");
            for (int i = 0; i < _xTiles; i++) {
                for (int j = 0; j < _yTiles; j++) {
                    Vector3 pos = new(
                        origin.x + i * TILE_SIZE,
                        origin.y + j * TILE_SIZE,
                        0f
                    );

                    // Instantiate at the correct position immediately
                    _tileGrid[i,j] = Instantiate(
                        tilePrefab,
                        pos,
                        Quaternion.identity,
                        _container.transform
                    );
                    _tileGrid[i,j].name = $"Tile[{i},{j}]";
                    _tileGrid[i, j].renderer.sprite = tileset.Tiles[_map[i, j]];
                    _tileGrid[i, j].renderer.color  = tint;

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
        }

        static byte[,] GenerateMap(WangTileCollection tileset, uint width, uint height) {
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

                    grid[x, y] = WeightedRandomTile(candidates, WeightMoreAir, _rng);
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
        
        static float WeightMoreAir(byte mask) {
            int edges = CountEdges(mask);
            return 4 - edges + 1; // more weight to fewer edges, +1 to avoid zero
        }

        static float WeightMoreSolid(byte mask) {
            int edges = CountEdges(mask);
            return edges + 1; // more weight to more edges
        }

        static byte WeightedRandomTile(List<byte> candidates, Func<byte, float> weightFunc, Random rng) {
            float totalWeight                         = 0f;
            foreach (byte c in candidates) totalWeight += weightFunc(c);

            float randomValue = (float)(rng.NextDouble() * totalWeight);
            foreach (byte c in candidates) {
                randomValue -= weightFunc(c);
                if (randomValue <= 0f) return c;
            }
            
            return candidates[0]; // fallback
        }

        void RegenerateTileArea(int x, int y, byte newMask) {
            // update original tile
            _map[x, y] = newMask;
            _tileGrid[x, y].renderer.sprite = tileset.Tiles[_map[x, y]];

            // update west
            if (0 < x) {
                byte mask = RegenerateTile(x - 1, y);
                _map[x - 1, y] = mask;
                _tileGrid[x - 1, y].renderer.sprite = tileset.Tiles[mask];
            }

            // east
            if (x < _xTiles - 1) {
                byte mask = RegenerateTile(x + 1, y);
                _map[x + 1, y] = mask;
                _tileGrid[x + 1, y].renderer.sprite = tileset.Tiles[mask];
            }
            
            // south
            if (0 < y) {
                byte mask = RegenerateTile(x, y - 1);
                _map[x, y - 1] = mask;
                _tileGrid[x, y - 1].renderer.sprite = tileset.Tiles[mask];
            }
            
            // north
            if (y < _yTiles - 1) {
                byte mask = RegenerateTile(x, y + 1);
                _map[x, y + 1] = mask;
                _tileGrid[x, y + 1].renderer.sprite = tileset.Tiles[mask];
            }
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
            return WeightedRandomTile(candidates, WeightMoreAir, _rng);
        }

        void BakeTexture() {
#if UNITY_EDITOR
            Texture2D tex = WangTextureBaker.BakeSolidMask(tileset, _map);
            byte[] pngData  = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, "DebugBake.png");
            File.WriteAllBytes(fullPath, pngData);
#endif
        }
        
        void OnDrawGizmos() {
            if (_canvasSize == Vector2Int.zero) return;

            Gizmos.color = Color.green;
            for (int x = 0; x <= _xTiles; x++)
                Gizmos.DrawLine(new Vector3(x * pixelsPerUnit, 0, 0), new Vector3(x * pixelsPerUnit, _yTiles * pixelsPerUnit, 0));

            for (int y = 0; y <= _yTiles; y++)
                Gizmos.DrawLine(new Vector3(0, y * pixelsPerUnit, 0), new Vector3(_xTiles * pixelsPerUnit, y * pixelsPerUnit, 0));
        }
    }
}