using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src.engine {
    public class Tile {
        public int[] frames;
        public int animSpeed;
        public bool solid;
        public event TileTouch TileTouchEvent;
        public Tile(bool solid, params int[] frames) {
            this.frames = frames;
            this.solid = solid;
        }
        public delegate void TileTouch(int x, int y, Entity entity);
        public void InvokeTouchEvent(int x, int y, Entity entity) => TileTouchEvent.InvokeAll(x, y, entity);
        public int GetTextureIndex() => frames[Main.GlobalTimer / animSpeed % frames.Length];
    }
    public class Tilemap {
        private readonly int[] tilemap;
        public readonly Tileset tileset;
        public readonly int width;
        public readonly int height;
        public Tilemap(Tileset tileset, int width, int height) {
            this.tileset = tileset;
            this.width = width;
            this.height = height;
            tilemap = new int[width * height];
        }
        public int this[int x, int y] {
            get {
                if (x < 0 || y < 0 || x >= width || y >= height) return 00;
                return tilemap[y * width + x];
            }
            set {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                tilemap[y * width + x] = value;
            }
        }
        public void Render(SpriteBatch batch, float drawX, float drawY, float scale = 1, int cullX = 0, int cullY = 0, int cullW = -1, int cullH = -1) {
            if (cullW == -1) cullW = width;
            if (cullH == -1) cullH = height;
            float tw = tileset.TileWidth * scale;
            float th = tileset.TileHeight * scale;
            for (int x = cullX; x < cullW; x++) {
                for (int y = cullY; y < cullH; y++) {
                    int index = tileset.Tiles[this[x, y]].GetTextureIndex();
                    int tx = index % tileset.TilesInRow;
                    int ty = index / tileset.TilesInRow;
                    batch.Draw(Assets.GetAsset<Texture2D>(tileset.Texture),
                        new Rectangle((int)(drawX + x * tw), (int)(drawY + y * th), (int)tw, (int)th),
                        new Rectangle(tx * tileset.TileWidth, ty * tileset.TileHeight, tileset.TileWidth, tileset.TileHeight),
                        Main.GameColor
                    );
                }
            }
        }
    }
    public class Tileset {
        public string Texture { get; set; }
        public int TilesInRow { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public ReadOnlyCollection<Tile> Tiles { get; set; }
        public Tileset(string texture, int tilesInRow, int tileWidth, int tileHeight, params Tile[] tiles) {
            Tiles = new(tiles);
            Texture = texture;
            TilesInRow = tilesInRow;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
        }
    }
}