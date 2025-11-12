using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;


namespace Tiles {
    [CreateAssetMenu(fileName = "New Wang Tileset", menuName = "Tiles/Wang Tileset")]
    public class WangTileCollection : ScriptableObject, ISerializationCallbackReceiver {
        public Sprite[] northSprites;
        public Sprite[] eastSprites;
        public Sprite[] southSprites;
        public Sprite[] westSprites;
        public Sprite empty;
        
        [Header("All Unique Sprites (auto-generated)")]
        [SerializeField] internal Sprite[] Tiles;
        internal List<byte>[,] BitMatchTiles;

        [SerializeField] List<DByteList> BitMatchTilesSerialised;

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

            Debug.Log($"Wang Tiles Initialised, {Tiles.Length} tiles\n{BitMatchTiles.Length}");

            // Mark as dirty so the change shows in the editor
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        
        public void OnBeforeSerialize() {
            // Example: Flatten 2D array to List of Lists
            if (BitMatchTiles == null) return;
            BitMatchTilesSerialised.Clear();
            for (int i = 0; i < BitMatchTiles.GetLength(0); i++) {
                DByteList byteList = new();
                for (int j = 0; j < BitMatchTiles.GetLength(1); j++) {
                    byteList.DBList.Add(new ByteList { BList = BitMatchTiles[i, j] });
                }
                BitMatchTilesSerialised.Add(byteList);
            }
        }

        public void OnAfterDeserialize() {
            // Example: Rebuild the 2D array from List of Lists
            if (BitMatchTilesSerialised.Count <= 0) return;
            BitMatchTiles = new List<byte>[BitMatchTilesSerialised.Count, BitMatchTilesSerialised[0].DBList.Count];
            for (int i = 0; i < BitMatchTilesSerialised.Count; i++) {
                for (int j = 0; j < BitMatchTilesSerialised[i].DBList.Count; j++) {
                    BitMatchTiles[i, j] = BitMatchTilesSerialised[i].DBList[j].BList;
                }
            }
            Debug.Log($"Wang Tiles Deserialized {BitMatchTiles.Length}");
        }
    }
    
    // Unity pretends to not know how to serialise a multi-dimensional list, so here's the workaround.
    [System.Serializable]
    public class DByteList {
        public List<ByteList> DBList = new();
    }
    
    [System.Serializable]
    public class ByteList {
        public List<byte> BList = new();
    }
}