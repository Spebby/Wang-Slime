using UnityEngine;


namespace Tiles {
    public static class WangTextureBaker {
        /// <summary>
        /// Bakes a tile map into a lightweight single-channel (R8) texture.
        /// Each pixel = 0 (air) or 1 (solid).
        /// </summary>
        /// <param name="tileset">Source image tileset.</param>
        /// <param name="map">Map data used to generate the image.</param>
        /// <param name="size">Individual tile scale. Must be >=1</param>
        public static Texture2D BakeSolidMask(WangTileCollection tileset, byte[,] map, int size = 1) {
            int width  = map.GetLength(0);
            int height = map.GetLength(1);

            // assume all tiles are same size
            Sprite refSprite = tileset.Tiles[0];
            int    tileW     = (int)refSprite.textureRect.width;
            int    tileH     = (int)refSprite.textureRect.height;

            // multiply by size for output resolution
            Texture2D output = new(width * tileW * size, height * tileH * size, TextureFormat.R8, false) {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[output.width * output.height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    byte   mask = map[x, y];
                    Sprite tile = tileset.Tiles[mask];
                    if (!tile) continue;

                    Texture2D tileTex = tile.texture;
                    tileTex.filterMode = FilterMode.Point;

                    Rect rect = tile.rect; // use rect so we don't trim transparent pixels
                    int  srcX = Mathf.RoundToInt(rect.x);
                    int  srcY = Mathf.RoundToInt(rect.y);
                    int  w    = Mathf.RoundToInt(rect.width);
                    int  h    = Mathf.RoundToInt(rect.height);

                    Color[] tilePixels = tileTex.GetPixels(srcX, srcY, w, h);

                    for (int ty = 0; ty < h; ty++) {
                        for (int tx = 0; tx < w; tx++) {
                            Color c     = tilePixels[ty * w + tx];
                            byte  value = (byte)(c.a > 0.5f ? 255 : 0);

                            // scale coordinates by size
                            int oxBase = x * tileW * size + tx * size;
                            int oyBase = y * tileH * size + ty * size;

                            // fill the scaled pixels
                            for (int sy = 0; sy < size; sy++) {
                                for (int sx = 0; sx < size; sx++) {
                                    int ox = oxBase + sx;
                                    int oy = oyBase + sy;
                                    pixels[oy * output.width + ox] = new Color32(value, 0, 0, 255);
                                }
                            }
                        }
                    }
                }
            }

            output.SetPixels32(pixels);
            output.Apply();
            return output;
        }
    }
}