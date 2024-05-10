using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace OneJump.src.ui {
    public class UIButton : IUIComponent {
        public string Text { get; set; } = "";
        public float Scale { get; set; } = 1;
        private Action OnClick { get; set; } = null; 
        private bool hovering = false;
        private bool prevHovering = false;
        private readonly string sound;
        public UIButton(string text, float scale = 1.0f, Action onClick = null, string sound = "sounds/ok.wav") {
            Text = text;
            Scale = scale;
            OnClick = onClick;
            this.sound = sound;
        }
        public void Render(SpriteBatch batch, Vector4 bounds) {
            float width  = (Text.Length * 5 + (Text.Length - 1)) * Scale;
            float height = 5 * Scale;
            float x = bounds.X + (bounds.Z - width ) / 2;
            float y = bounds.Y + (bounds.W - height) / 2;
            if (hovering) {
                batch.DrawRect(bounds.X, bounds.Y, bounds.Z, bounds.W, Main.GameColor);
                batch.DrawText(Text, x, y, Scale, Color.Black);
                return;
            }
            batch.DrawText(Text, x, y, Scale, Main.GameColor);
            batch.DrawRect(bounds.X, bounds.Y, bounds.Z, 3, Main.GameColor);
            batch.DrawRect(bounds.X, bounds.Y, 3, bounds.W, Main.GameColor);
            batch.DrawRect(bounds.X + bounds.Z - 3, bounds.Y, 3, bounds.W, Main.GameColor);
            batch.DrawRect(bounds.X, bounds.Y + bounds.W - 3, bounds.Z, 3, Main.GameColor);
        }
        public void Update(Vector4 bounds) {
            int x = Input.X;
            int y = Input.Y;
            hovering = x >= bounds.X && y >= bounds.Y && x < bounds.X + bounds.Z && y < bounds.Y + bounds.W;
            if (hovering && Input.ButtonPressed(Input.LeftButton)) {
                if (sound != null) SFXPlayer.Play(sound);
                OnClick?.Invoke();
            }
            if (hovering && !prevHovering) SFXPlayer.Play("sounds/select.wav");
            prevHovering = hovering;
        }
    }
    public class UIText : IUIComponent {
        public string Text { get; set; } = "";
        public float Scale { get; set; } = 1;
        public UIText(string text, float scale = 1.0f) {
            Text = text;
            Scale = scale;
        }
        public void Render(SpriteBatch batch, Vector4 bounds) {
            float width  = (Text.Length * 5 + (Text.Length - 1)) * Scale;
            float height = 5 * Scale;
            float x = bounds.X + (bounds.Z - width ) / 2;
            float y = bounds.Y + (bounds.W - height) / 2;
            batch.DrawText(Text, x, y, Scale, Main.GameColor);
        }
        public void Update(Vector4 bounds) {}
    }
    public class UISubGrid : IUIComponent {
        private readonly UIGrid grid;
        public UISubGrid(UIGrid grid) {
            this.grid = grid;
        }
        private static Viewport CreateViewport(Vector4 bounds) =>
            new((int)bounds.Z, (int)bounds.W, (int)(bounds.X + bounds.Z), (int)(bounds.Y + bounds.W));
        public void Render(SpriteBatch batch, Vector4 bounds) {
            grid.Render(batch, CreateViewport(bounds));
        }
        public void Update(Vector4 bounds) {
            grid.Update(CreateViewport(bounds));
        }
    }
    public class UIGame : IUIComponent {
        public void Update(Vector4 bounds) {
            Main.CurrentScene.Update();
        }
        public void Render(SpriteBatch batch, Vector4 bounds) {
            Main.CurrentScene.RenderLayers(batch, new Viewport((int)bounds.X, (int)bounds.Y, (int)bounds.Z, (int)bounds.W));
        }
    }
    public class UITransition : IUIComponent {
        public void Update(Vector4 bounds) {}
        public void Render(SpriteBatch batch, Vector4 bounds) {
            batch.Draw(Main.irisRenderTarget, new Vector2(bounds.X, bounds.Y), Main.GameColor);
        }
    }
    public class UIDebug : IUIComponent {
        public void Update(Vector4 bounds) {}
        public void Render(SpriteBatch batch, Vector4 bounds) {
            batch.DrawRect(bounds.X, bounds.Y, bounds.Z, bounds.W, Main.GameColor);
        }
    }
}