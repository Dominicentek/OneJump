using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src.engine {
    public class TileBuilder {
        private readonly List<int> animFrames = new();
        private readonly List<Tile.TileTouch> tileTouchHandlers = new();
        private bool solid = false;
        private int animSpeed = 5;
        public TileBuilder AddAnimFrames(params int[] frames) {
            foreach (int frame in frames) {
                animFrames.Add(frame);
            }
            return this;
        }
        public TileBuilder AddEntityCollisionHandler(Tile.TileTouch handler) {
            tileTouchHandlers.Add(handler);
            return this;
        }
        public TileBuilder Solid() {
            solid = true;
            return this;
        }
        public TileBuilder AnimSpeed(int animSpeed) {
            this.animSpeed = animSpeed;
            return this;
        }
        public Tile Build() {
            Tile tile = new(solid, animFrames.ToArray());
            foreach (Tile.TileTouch handler in tileTouchHandlers) {
                tile.TileTouchEvent += handler;
            }
            tile.animSpeed = animSpeed;
            Tiles.TileList.Add(tile);
            return tile;
        }
    }
    public class Tiles {
        public static readonly List<Tile> TileList = new();
        public static Tile.TileTouch TTH_Spike() => (int x, int y, Entity entity) => {
            if (entity.Tag == "player") {
                Main.CurrentScene.Die(entity, "you died");
            }
        };
        public static Tile.TileTouch TTH_Coin() => (int x, int y, Entity entity) => {
            if (entity.Tag == "player") {
                Main.CurrentScene.Tilemap[x, y] = 0;
                entity.SetProperty("can_jump", true);
                entity.PlaySound("sounds/collect.wav");
                Sliders<Main, Color>.Add(null, "GameColor", new(0.0f, 1.0f, 0.0f, 1.0f), 30, Sliders.ColorInterpolator, Sliders.EasingCubicOut);
                Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", 1f, 30, Sliders.FloatInterpolator, Sliders.EasingCubicOut);
            }
        };
        public static Tile.TileTouch TTH_Fizzler() => (int x, int y, Entity entity) => {
            if (entity.Tag == "cube") {
                entity.PlaySound("sounds/vaporize.wav");
                entity.Despawn();
                Main.CurrentScene.SpawnDeathParticles(entity.X, entity.Y);
            }
            if (entity.Tag == "player") {
                if (entity.HasProperty("picked_up_cube")) {
                    Entity cube = entity.GetProperty<Entity>("picked_up_cube");
                    entity.Height -= cube.Height;
                    cube.PlaySound("sounds/vaporize.wav");
                    cube.Despawn();
                    entity.RemoveProperty("picked_up_cube");
                    Main.CurrentScene.SpawnDeathParticles(entity.X, entity.Y);
                }
            }
        };
        // LE_TileBegin
        public static readonly Tile Air = new TileBuilder()
            .AddAnimFrames(0)
            .Build();
        public static readonly Tile Wall = new TileBuilder()
            .AddAnimFrames(3)
            .Solid()
            .Build();
        public static readonly Tile WallTopLeft = new TileBuilder()
            .AddAnimFrames(1)
            .Solid()
            .Build();
        public static readonly Tile WallTop = new TileBuilder()
            .AddAnimFrames(2)
            .Solid()
            .Build();
        public static readonly Tile WallTopRight = new TileBuilder()
            .AddAnimFrames(6)
            .Solid()
            .Build();
        public static readonly Tile WallRight = new TileBuilder()
            .AddAnimFrames(7)
            .Solid()
            .Build();
        public static readonly Tile WallBottomRight = new TileBuilder()
            .AddAnimFrames(8)
            .Solid()
            .Build();
        public static readonly Tile WallBottom = new TileBuilder()
            .AddAnimFrames(12)
            .Solid()
            .Build();
        public static readonly Tile WallBottomLeft = new TileBuilder()
            .AddAnimFrames(13)
            .Solid()
            .Build();
        public static readonly Tile WallLeft = new TileBuilder()
            .AddAnimFrames(14)
            .Solid()
            .Build();
        public static readonly Tile WallCornerTopLeft = new TileBuilder()
            .AddAnimFrames(22)
            .Solid()
            .Build();
        public static readonly Tile WallCornerTopRight = new TileBuilder()
            .AddAnimFrames(23)
            .Solid()
            .Build();
        public static readonly Tile WallCornerBottomLeft = new TileBuilder()
            .AddAnimFrames(28)
            .Solid()
            .Build();
        public static readonly Tile WallCornerBottomRight = new TileBuilder()
            .AddAnimFrames(29)
            .Solid()
            .Build();
        public static readonly Tile Spike = new TileBuilder()
            .AddAnimFrames(4)
            .AddEntityCollisionHandler(TTH_Spike())
            .Build();
        public static readonly Tile PoisonSurface = new TileBuilder()
            .AddAnimFrames(5, 9, 10, 11)
            .AddEntityCollisionHandler(TTH_Spike())
            .Build();
        public static readonly Tile Poison = new TileBuilder()
            .AddAnimFrames(3)
            .AddEntityCollisionHandler(TTH_Spike())
            .Build();
        public static readonly Tile Coin = new TileBuilder()
            .AddAnimFrames(15, 16, 17, 16)
            .AddEntityCollisionHandler(TTH_Coin())
            .Build();
        public static readonly Tile FizzlerVertical = new TileBuilder()
            .AddAnimFrames(18, 19, 20, 21)
            .AddEntityCollisionHandler(TTH_Fizzler())
            .Build();
        public static readonly Tile FizzlerHorizontal = new TileBuilder()
            .AddAnimFrames(24, 25, 26, 27)
            .AddEntityCollisionHandler(TTH_Fizzler())
            .Build();
        public static readonly Tile PoisonBottom = new TileBuilder()
            .AddAnimFrames(33, 32, 31, 30)
            .AddEntityCollisionHandler(TTH_Spike())
            .Build();
        // LE_TileEnd
        public static Tileset CreateTileset() =>
            new("images/tileset.png", 6, 16, 16, TileList.ToArray());
    }
}