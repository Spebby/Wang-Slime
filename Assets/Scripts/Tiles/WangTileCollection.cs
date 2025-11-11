using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;


namespace Tiles {
    [CreateAssetMenu(fileName = "New Wang Tileset", menuName = "Tiles/Wang Tileset")]
    public class WangTileCollection : ScriptableObject {
        public Sprite[] northSprites;
        public Sprite[] eastSprites;
        public Sprite[] southSprites;
        public Sprite[] westSprites;
        public Sprite empty;
        
        [Header("All Unique Sprites (auto-generated)")]
        [SerializeField, ReadOnly] internal Sprite[] Tiles;
        [SerializeField, ReadOnly] internal List<byte>[,] BitMatchTiles;
        
#if UNITY_EDITOR
        void OnValidate() {
            // Combine all sprites and remove duplicates
            List<Sprite> unsorted = northSprites
                                   .Concat(eastSprites)
                                   .Concat(southSprites)
                                   .Concat(westSprites)
                                   .Append(empty)
                                   .Where(s => s)
                                   .Distinct()
                                   .ToList();

            Tiles = new Sprite[unsorted.Count];
            foreach (Sprite tile in unsorted) {
                int mask                              = 0;
                if (northSprites.Contains(tile)) mask |= 1 << 0;
                if (eastSprites.Contains(tile))  mask |= 1 << 1;
                if (southSprites.Contains(tile)) mask |= 1 << 2;
                if (westSprites.Contains(tile))  mask |= 1 << 3;
                Tiles[mask] = tile;
            }
            
            // generate the bit match tileset.
            BitMatchTiles = new List<byte>[Tiles.Length,Tiles.Length];
            for (int required = 0; required < 16; required++) {
                for (int excluded = 0; excluded < 16; excluded++) {
                    if ((required & excluded) != 0) continue; // let it be empty
                    
                    // otherwise...
                    BitMatchTiles[required,excluded] = new List<byte>();
                    for (byte mask = 0; mask < 16; mask++) {
                        bool hasRequired = (mask & required) == required;
                        bool hasExcluded = (mask & excluded) != 0;

                        if (hasRequired && !hasExcluded) BitMatchTiles[required, excluded].Add(mask);
                    }
                }
            }
            
            // Mark as dirty so the change shows in the editor
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}