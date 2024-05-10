using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src.engine {
    public struct Wire {
        public byte ev;
        public List<(float, float)> line;
    }
    public class Scene {
        public static readonly float GAME_SCALE = 2f;
        private static readonly int NUM_BACKDROP_LAYERS = 5;
        private static readonly float MARGIN = 16f;
        private readonly byte[] data;
        private readonly List<Wire> wires = new();
        private bool requestReload = false;
        public readonly bool[] events = new bool[256];
        public List<Entity> Entities { get; } = new();
        public Tilemap Tilemap;
        public float BackdropScale { get; set; } = 1f;
        public float Scale = GAME_SCALE;
        public Scene(byte[] data) {
            this.data = data;
            ReloadInner();
        }
        public void Update() {
            if (Main.Paused) return;
            if (requestReload) {
                requestReload = false;
                ReloadInner();
            }
            Entities.Sort((Entity a, Entity b) => {
                return (int)((a.Y - b.Y) * 1000);
            });
            foreach (Entity entity in Entities) entity.DidUpdate = false;
            foreach (Entity entity in Entities) entity.Update();
            for (int i = 0; i < Entities.Count; i++) {
                if ((Entities[i].Flags & Entity.FlagShouldDelete) == 0) continue;
                Entities.RemoveAt(i--);
            }
        }
        private void DrawWire(SpriteBatch batch, Wire wire, int originX, int originY, Color color) {
            for (int j = 0; j < wire.line.Count - 1; j++) {
                batch.DrawLine(
                    (wire.line[j    ].Item1 + MARGIN) * Scale * Tilemap.tileset.TileWidth  + originX,
                    (wire.line[j    ].Item2 + MARGIN) * Scale * Tilemap.tileset.TileHeight + originY,
                    (wire.line[j + 1].Item1 + MARGIN) * Scale * Tilemap.tileset.TileWidth  + originX,
                    (wire.line[j + 1].Item2 + MARGIN) * Scale * Tilemap.tileset.TileHeight + originY,
                    Scale, color
                );
            }
        }
        public void RenderLayers(SpriteBatch batch, Viewport? viewport = null) {
            viewport ??= batch.GraphicsDevice.Viewport;
            Viewport vp = (Viewport)viewport;
            List<Tuple<float, Color>> layers = new();
            float step = (BackdropScale - 1) / (NUM_BACKDROP_LAYERS - 1);
            float curr = 1;
            float color = 0.5f;
            float colorStep = (0.1f - color) / (NUM_BACKDROP_LAYERS - 1);
            for (int i = 0; i < NUM_BACKDROP_LAYERS; i++) {
                float colorMul = i == 0 ? 1f : color;
                layers.Add(new(curr * Scale, Main.GameColor.Brightness(colorMul)));
                curr += step;
                color += colorStep;
            }
            layers.Reverse();
            for (int i = 0; i < layers.Count; i++) {
                int width  = (int)(Main.gameRenderTarget.Width  * layers[i].Item1);
                int height = (int)(Main.gameRenderTarget.Height * layers[i].Item1);
                int originX = (vp.Width  - width ) / 2;
                int originY = (vp.Height - height) / 2;
                if (i == layers.Count - 1) {
                    List<Wire> wiresOff = new();
                    List<Wire> wiresOn  = new();
                    foreach (Wire wire in wires) {
                        (events[wire.ev] ? wiresOn : wiresOff).Add(wire);
                    }
                    foreach (Wire wire in wiresOff) {
                        DrawWire(batch, wire, originX, originY, Main.GameColor.Brightness(0.25f));
                    }
                    foreach (Wire wire in wiresOn) {
                        DrawWire(batch, wire, originX, originY, Main.GameColor.Brightness(0.75f));
                    }
                }
                batch.Draw(Main.gameRenderTarget, new Rectangle(originX, originY, width, height), layers[i].Item2);
            }
        }
        public RenderTarget2D Render(SpriteBatch batch) {
            RenderTarget2D target = new(batch.GraphicsDevice,
                (int)((Tilemap.width  + MARGIN * 2) * Tilemap.tileset.TileWidth ) + 1,
                (int)((Tilemap.height + MARGIN * 2) * Tilemap.tileset.TileHeight) + 1,
            false, SurfaceFormat.Vector4, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            Color orig = Main.GameColor;
            Main.GameColor = Color.White;
            batch.GraphicsDevice.SetRenderTarget(target);
            batch.GraphicsDevice.Clear(Color.Transparent);
            batch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
            float offsetX = MARGIN * Tilemap.tileset.TileWidth;
            float offsetY = MARGIN * Tilemap.tileset.TileHeight;
            Tilemap.Render(batch, offsetX, offsetY, 1);
            foreach (Entity entity in Entities) {
                entity.Render(batch, offsetX, offsetY, 1);
            }
            batch.End();
            Main.GameColor = orig;
            return target;
        }
        public void Reload() {
            requestReload = true;
        }
        private void ReloadInner() {
            events.Fill(false);
            Entities.Clear();
            BackdropScale = 1f;
            ByteArrayReader reader = new(data);
            Tilemap = new(Main.Tileset, reader.SInt(), reader.SInt());
            for (int y = 0; y < Tilemap.height; y++) {
                for (int x = 0; x < Tilemap.width; x++) {
                    Tilemap[x, y] = reader.UByte();
                }
            }
            int numEntities = reader.SInt();
            float irisX = Tilemap.width / 2f, irisY = Tilemap.height / 2f;
            for (int i = 0; i < numEntities; i++) {
                Entity entity = AddEntity(EntityBuilders.Entities[reader.UByte()], reader.Float(), reader.Float());
                while (true) {
                    string name = reader.String();
                    if (name == "") break;
                    entity.Properties.Add(name, reader.UByte() switch {
                        0 => reader.UByte  (),
                        1 => reader.UShort (),
                        2 => reader.SByte  (),
                        3 => reader.SShort (),
                        4 => reader.SInt   (),
                        5 => reader. Float (),
                        _ => null
                    });
                }
                if (entity.Tag == "player") {
                    irisX = entity.X;
                    irisY = entity.Y - entity.Height / 2;
                }
            }
            int numWires = reader.SInt();
            for (int i = 0; i < numWires; i++) {
                Wire wire = new() { ev = reader.UByte(), line = new() };
                int numPoints = reader.SInt();
                for (int j = 0; j < numPoints; j++) {
                    wire.line.Add((reader.Float(), reader.Float()));
                }
                wires.Add(wire);
            }
            if (Main.IrisText == "" || Main.IrisActive) return;
            Delay.Add(() => {
                Sliders<Scene, float>.Add(this, "BackdropScale", 1f, 1, Sliders.FloatInterpolator, Sliders.EasingLinear);
                Sliders<Main, Color>.Add(null, "GameColor", new(0f, 1f, 0f, 1f), 20, Sliders.ColorInterpolator, Sliders.EasingCubicOut);
                Main.Iris(irisX, irisY, Main.IrisText, 30, false, null);
            }, 5);
        }
        public void CalculateIrisParams(out float x, out float y, out float r) {
            float originX = (Main.Viewport.Width - Tilemap.width * Tilemap.tileset.TileWidth * Scale) / 2;
            float originY = (Main.Viewport.Height - Tilemap.height * Tilemap.tileset.TileHeight * Scale) / 2;
            x = originX + Main.IrisPos.X * Tilemap.tileset.TileWidth  * Scale;
            y = originY + Main.IrisPos.Y * Tilemap.tileset.TileHeight * Scale;
            float tlx = 0;
            float tly = 0;
            float brx = Main.Viewport.Width;
            float bry = Main.Viewport.Height;
            double tld = Math.Pow(x - tlx, 2) + Math.Pow(y - tly, 2);
            double trd = Math.Pow(x - brx, 2) + Math.Pow(y - tly, 2);
            double bld = Math.Pow(x - tlx, 2) + Math.Pow(y - bry, 2);
            double brd = Math.Pow(x - brx, 2) + Math.Pow(y - bry, 2);
            r = (float)Math.Sqrt(Math.Max(tld, Math.Max(trd, Math.Max(bld, brd))));
        }
        public Entity AddEntity(EntityBuilder builder, float x, float y) {
            Entity entity = builder.Build();
            entity.X = x;
            entity.Y = y;
            entity.Tilemap = Tilemap;
            Entities.Add(entity);
            return entity;
        }
        public delegate bool Condition(Entity entity);
        public (Entity, float) NearestEntityWithCondition(Entity entity, Condition cond) {
            float x = entity.X;
            float y = entity.Y - entity.Height / 2;
            float minDist = 1000000;
            Entity nearest = null;
            foreach (Entity e in Entities) {
                if (e == entity) continue;
                if (!cond(e)) continue;
                float ex = e.X;
                float ey = e.Y - e.Height / 2;
                float dist = (float)Math.Sqrt((ex - x) * (ex - x) + (ey - y) * (ey - y));
                if (minDist > dist) {
                    minDist = dist;
                    nearest = e;
                }
            }
            return (nearest, minDist);
        }
        public (Entity, float) NearestEntityWithTag(Entity entity, string tag) => NearestEntityWithCondition(entity, (Entity e) => e.Tag == tag);
        public int this[int x, int y] => Tilemap[x, y];
        public void SpawnDeathParticles(float x, float y) {
            Delay.Add(() => {
                for (int i = 0; i < 25; i++) {
                    AddEntity(EntityBuilders.DeathParticle, x, y);
                }
            }, 1);
        }
        public Entity EntityWithTag(string tag) {
            foreach (Entity entity in Entities) {
                if (entity.Tag == tag) return entity;
            }
            return null;
        }
        public void Die(Entity entity, string message) {
            entity.Tag = "player_died";
            entity.PlaySound("sounds/die.wav");
            entity.Despawn();
            SpawnDeathParticles(entity.X, entity.Y - entity.Height / 2);
            Sliders<Scene, float>.Add(this, "BackdropScale", 1.25f, 20, Sliders.FloatInterpolator, Sliders.EasingCubicOut);
            Sliders<Main, Color>.Add(null, "GameColor", new(1f, 0f, 0f, 1f), 20, Sliders.ColorInterpolator, Sliders.EasingCubicOut);
            Main.Iris(entity.X, entity.Y - entity.Height / 2, message, 30, true, () => {
                Reload();
            });
        }
    }
}