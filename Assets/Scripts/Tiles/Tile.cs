using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Tiles {
    [Flags]
    enum Directions : byte {
        North = 1 << 0,
        East  = 1 << 1,
        South = 1 << 2,
        West  = 1 << 3
    }

    // This is just a visual wrapper w/ rotate funciton built in
    [RequireComponent(typeof(SpriteRenderer)), RequireComponent(typeof(BoxCollider2D))]
    public class Tile : MonoBehaviour {
        public new SpriteRenderer renderer;
        public Action OnClick;
        
        void OnMouseDown() {
            OnClick?.Invoke();
        }
    }
    
    public static class TwoBitWangTile {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte MaskFromCardinals(bool north, bool south, bool east, bool west) {
            return (byte)((north ? Directions.North : 0)
                        | (east  ? Directions.East  : 0)
                        | (south ? Directions.South : 0)
                        | (west  ? Directions.West  : 0));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RotateClockwise(byte index) {
            // rotate edges clockwise
            int n       = (index & 8) >> 3;
            int e       = (index & 4) >> 2;
            int s       = (index & 2) >> 1;
            int w       = (index & 1);
            return (byte)((n << 2) | (e << 1) | (s) | (w << 3));
        }
    }
}