using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src.engine {
    public class Entity {
        public const int NumSteps = 4;
        public const int FlagShouldDelete      = (1 << 0);
        public const int FlagSolidHitbox       = (1 << 1);
        public const int FlagDisableCollision  = (1 << 2);
        public const int FlagAlternateCollCorr = (1 << 3);
        public Dictionary<string, object> Properties = new();
        public float PrevX { get; set; }
        public float PrevY { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float SpeedX { get; set; }
        public float SpeedY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public bool InAir { get; private set; }
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public int Flags { get; set; }
        public string Tag { get; set; }
        public bool DidUpdate { get; set; }
        public Tilemap Tilemap;
        public event EntityUpdate UpdateHandlers;
        public event EntityTexture TextureHandlers;
        public delegate void EntityUpdate(Entity entity);
        public delegate Texture2D EntityTexture(Entity entity);
        private bool CanJump = false;
        private float JumpSpeed = 0;
        private Entity platform = null;
        public void Update() {
            if (DidUpdate) return;
            PrevX = X;
            PrevY = Y;
            UpdateHandlers.InvokeAll(this);
            RunPhysics();
            DidUpdate = true;
        }
        public T GetProperty<T>(string name) => Properties.ContainsKey(name) ? (T)Properties[name] : default;
        public bool HasProperty(string name) => Properties.ContainsKey(name);
        public void SetProperty(string name, object value) {
            if (HasProperty(name)) Properties[name] = value;
            else Properties.Add(name, value);
        }
        public void AddProperty(string name, object value) {
            if (HasProperty(name)) return;
            SetProperty(name, value);
        }
        public T GetOrDefaultProperty<T>(string name, T def) {
            if (HasProperty(name)) return GetProperty<T>(name);
            return def;
        }
        public void RemoveProperty(string name) {
            if (!HasProperty(name)) return;
            Properties.Remove(name);
        }
        private void RunPhysX(float speedModifier) {
            X += SpeedX * speedModifier;
            float fromX = X - Width / 2;
            float fromY = Y - Height;
            float toX = X + Width / 2;
            float toY = Y;
            foreach (Entity entity in Main.CurrentScene.Entities) {
                if (entity == this) continue;
                if ((entity.Flags & FlagSolidHitbox) == 0) continue;
                if (entity.platform == this) continue;
                if (!IntersectsRectangle(
                    fromX, fromY, toX, toY,
                    entity.X - entity.Width / 2, entity.Y - entity.Height, entity.X + entity.Width / 2, entity.Y
                )) continue;
                // bro i fucking hate floats 💀☠️🔥🗣️
                if (X <= entity.X) X = entity.X - entity.Width / 2 - Width / 2 - 0.001f;
                else               X = entity.X + entity.Width / 2 + Width / 2 + 0.001f;
                SpeedX = 0;
            }
            fromX = X - Width / 2;
            fromY = Y - Height;
            toX = X + Width / 2;
            toY = Y;
            for (int x = (int)fromX; x <= toX; x++) {
                for (int y = (int)fromY; y <= toY; y++) {
                    if (!Tilemap.tileset.Tiles[Tilemap[x, y]].solid) continue;
                    if (!IntersectsRectangle(fromX, fromY, toX, toY, x, y, x + 1, y + 1)) continue;
                    // do this horseshit for tiles too
                    if (X <= x + 0.5) X = x     - Width / 2 - 0.001f;
                    else              X = x + 1 + Width / 2 + 0.001f;
                    SpeedX = 0;
                }
            }
        }
        private void RunPhysY(float speedModifier) {
            Y += SpeedY * speedModifier;
            float fromX = X - Width / 2;
            float fromY = Y - Height;
            float toX = X + Width / 2;
            float toY = Y;
            foreach (Entity entity in Main.CurrentScene.Entities) {
                if (entity == this) continue;
                if ((entity.Flags & FlagSolidHitbox) == 0) continue;
                if (entity.platform == this) continue;
                if (!IntersectsRectangle(
                    fromX, fromY, toX, toY,
                    entity.X - entity.Width / 2, entity.Y - entity.Height, entity.X + entity.Width / 2, entity.Y
                )) continue;
                if (Y <= entity.Y) {
                    Y = entity.Y - entity.Height;
                    InAir = false;
                    platform = entity;
                }
                else Y = entity.Y + Height;
                SpeedY = 0;
            }
            fromX = X - Width / 2;
            fromY = Y - Height;
            toX = X + Width / 2;
            toY = Y;
            for (int x = (int)fromX; x <= toX; x++) {
                for (int y = (int)fromY; y <= toY; y++) {
                    if (!Tilemap.tileset.Tiles[Tilemap[x, y]].solid) continue;
                    if (!IntersectsRectangle(fromX, fromY, toX, toY, x, y, x + 1, y + 1)) continue;
                    if (Y <= y + 0.5) {
                        Y = y;
                        InAir = false;
                    }
                    else Y = y + 1 + Height;
                    SpeedY = 0;
                }
            }
        }
        private delegate void PhysFunc(float speedModifier);
        public void RunPhysics() {
            if ((Flags & FlagDisableCollision) != 0) {
                X += SpeedX;
                Y += SpeedY;
                return;
            }
            InAir = true;
            if (platform != null) {
                platform.Update();
                X += platform.X - platform.PrevX;
                Y += platform.Y - platform.PrevY;
                platform = null;
            }
            PhysFunc Phys1 = RunPhysX;
            PhysFunc Phys2 = RunPhysY;
            if ((Flags & FlagAlternateCollCorr) != 0) {
                for (int i = 0; i < NumSteps; i++) {
                    Phys1(1f / NumSteps);
                    Phys1(0);
                }
                for (int i = 0; i < NumSteps; i++) {
                    Phys2(1f / NumSteps);
                    Phys2(0);
                }
            }
            else {
                for (int i = 0; i < NumSteps; i++) {
                    Phys1(1f / NumSteps);
                    Phys1(0);
                    Phys2(1f / NumSteps);
                    Phys2(0);
                }
            }
            float fromX = X - Width / 2;
            float fromY = Y - Height;
            float toX = X + Width / 2;
            float toY = Y;
            for (int x = (int)fromX; x <= toX; x++) {
                for (int y = (int)fromY; y <= toY; y++) {
                    if (!IntersectsRectangle(fromX, fromY, toX, toY, x, y, x + 1, y + 1)) continue;
                    Tilemap.tileset.Tiles[Tilemap[x, y]].InvokeTouchEvent(x, y, this);
                }
            }
            if (CanJump) SpeedY = -JumpSpeed;
            CanJump = false;
        }
        public void Despawn() {
            Flags |= FlagShouldDelete;
        }
        public void Jump(float speed) {
            JumpSpeed = speed;
            CanJump = true;
        }
        public Entity[] GetColliders() {
            List<Entity> entities = new();
            foreach (Entity entity in Main.CurrentScene.Entities) {
                if (entity == this) continue;
                if (IntersectsRectangle(
                           X -        Width / 2,        Y -        Height,        X +        Width / 2,        Y,
                    entity.X - entity.Width / 2, entity.Y - entity.Height, entity.X + entity.Width / 2, entity.Y
                )) entities.Add(entity);
            }
            return entities.ToArray();
        }
        public Texture2D GetTexture() {
            Texture2D texture = null;
            if (TextureHandlers == null) return null;
            Delegate[] delegates = TextureHandlers.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++) {
                texture = (Texture2D)delegates[i].DynamicInvoke(this);
                if (texture != null) break;
            }
            return texture;
        }
        public void Render(SpriteBatch batch, float originX, float originY, float scale = 1) {
            float x = originX + Tilemap.tileset.TileWidth * scale * X;
            float y = originY + Tilemap.tileset.TileHeight * scale * Y;
            Texture2D texture = GetTexture();
            if (texture == null) return;
            float w = texture.Width * scale;
            float h = texture.Height * scale;
            x -= w / 2;
            y -= h;
            SpriteEffects effect = SpriteEffects.None;
            float rotation = 0;
            if (FlipX && FlipY) rotation = 180;
            else if (FlipX) effect = SpriteEffects.FlipHorizontally;
            else if (FlipY) effect = SpriteEffects.FlipVertically;
            batch.Draw(texture, new Microsoft.Xna.Framework.Rectangle((int)(x + w / 2), (int)(y + h / 2), (int)w, (int)h), null, Main.GameColor, (float)(rotation / 180 * Math.PI), new Vector2(texture.Width, texture.Height) / 2, effect, 0);
        }
        public void PlaySound(string path) {
            SoundEffectInstance instance = SFXPlayer.Play(path);
            instance.Pan = Math.Clamp(X / Main.CurrentScene.Tilemap.width * 2 - 1, -1, 1);
        }
        private static bool IntersectsRectangle(
            float x1a, float y1a, float x2a, float y2a,
            float x1b, float y1b, float x2b, float y2b
        ) {
            RectangleF rect1 = new(x1a, y1a, x2a - x1a, y2a - y1a);
            RectangleF rect2 = new(x1b, y1b, x2b - x1b, y2b - y1b);
            return rect1.IntersectsWith(rect2);
        }
    }
}