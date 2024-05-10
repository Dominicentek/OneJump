using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src {
    public static class Utils {
        private static Texture2D white = null;
        public static void CreateWhitePixel(GraphicsDevice gd) {
            white = new(gd, 1, 1);
            white.SetData(new uint[]{ 0xFFFFFFFF });
        }
        public static void DrawText(this SpriteBatch batch, string text, float x, float y, float scale, Color color) {
            for (int i = 0; i < text.Length; i++) {
                int tx = text[i] % 16;
                int ty = text[i] / 16 - 2;
                batch.Draw(
                    Assets.GetAsset<Texture2D>("images/font.png"),
                    new Rectangle((int)(x + i * (6 * scale)), (int)y, (int)(5 * scale), (int)(5 * scale)),
                    new Rectangle(tx * 5, ty * 5, 5, 5),
                    color
                );
            }
        }
        public static void DrawRect(this SpriteBatch batch, float x, float y, float w, float h, Color color) {
            batch.Draw(white, new Rectangle((int)x, (int)y, (int)w, (int)h), color);
        }
        public static void DrawTriangle(this SpriteBatch batch, float x1, float y1, float x2, float y2, float x3, float y3, Color color) {
            VertexPositionColor[] vertices = new VertexPositionColor[3];
            vertices[0] = new(new(x1, y1, 0), color);
            vertices[1] = new(new(x2, y2, 0), color);
            vertices[2] = new(new(x3, y3, 0), color);
            Main.shader.CurrentTechnique.Passes[0].Apply();
            batch.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1);
        }
        public static void DrawCircle(this SpriteBatch batch, float x, float y, float r, int numVertices, Color color) {
            if (numVertices < 3) return;
            for (int i = 0; i < numVertices; i++) {
                float angle1 = (float)((i + 0) * 2 * Math.PI / numVertices);
                float angle2 = (float)((i + 1) * 2 * Math.PI / numVertices);
                float x1 = (float)Math.Cos(angle1) * r + x;
                float y1 = (float)Math.Sin(angle1) * r + y;
                float x2 = (float)Math.Cos(angle2) * r + x;
                float y2 = (float)Math.Sin(angle2) * r + y;
                batch.DrawTriangle(x1, y1, x2, y2, x, y, color);
            }
        }
        private static (float, float) LinePoint(float x, float y, float ox, float oy, float angle) {
            return (
                (float)(x * Math.Cos(angle) - y * Math.Sin(angle)) + ox,
                (float)(x * Math.Sin(angle) + y * Math.Cos(angle)) + oy
            );
        }
        public static void DrawLine(this SpriteBatch batch, float x1, float y1, float x2, float y2, Color color) {
            batch.DrawLine(x1, y1, x2, y2, 1.0f, color);
        }
        public static void DrawLineA(this SpriteBatch batch, float x, float y, float length, float angle, Color color) {
            batch.DrawLine(x, y, length, angle, 1.0f, color);
        }
        public static void DrawLine(this SpriteBatch batch, float x1, float y1, float x2, float y2, float thickness, Color color) {
            float angle = (float)Math.Atan2(y2 - y1, x2 - x1);
            float length = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            batch.DrawLineA(x1, y1, length, angle, thickness, color);
        }
        public static void DrawLineA(this SpriteBatch batch, float x, float y, float length, float angle, float thickness, Color color) {
            batch.Draw(white, new Vector2(x, y), null, color, angle, new Vector2(0, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0);
        }
        public static Color Alpha(this Color color, byte alpha) {
            return new(color.PackedValue) {
                A = alpha
            };
        }
        public static void InvokeAll(this Delegate ev, params object[] args) {
            if (ev == null) return;
            Delegate[] invocationList = ev.GetInvocationList();
            foreach (Delegate del in invocationList) {
                del.DynamicInvoke(args);
            }
        }
        public static void Fill<T>(this T[] arr, T val) {
            for (int i = 0; i < arr.Length; i++) arr[i] = val;
        }
        public static Color Brightness(this Color color, float brightness) {
            return new(
                (byte)(color.R * brightness),
                (byte)(color.G * brightness),
                (byte)(color.B * brightness),
                (byte)(color.A)
            );
        }
    }
}