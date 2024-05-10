using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OneJump.src.ui {
    internal enum SizeType {
        Pixel,
        Fraction,
    }
    internal record struct UIComponentSize(SizeType Type, int Size);
    internal record struct UIGridComponent(IUIComponent Component, int Rfrom, int Cfrom, int Rto, int Cto);
    public interface IUIComponent {
        void Update(Vector4 bounds);
        void Render(SpriteBatch batch, Vector4 bounds);
    }
    public class UIGrid {
        private readonly List<UIComponentSize> rows = new();
        private readonly List<UIComponentSize> columns = new();
        private readonly List<UIGridComponent> components = new();
        private readonly UIDebug debug = new();
        private int spacing = 0;
        public UIGrid AddRow(int size) {
            rows.Add(new(SizeType.Fraction, size));
            return this;
        }
        public UIGrid AddColumn(int size) {
            columns.Add(new(SizeType.Fraction, size));
            return this;
        }
        public UIGrid AddRowPx(int size) {
            rows.Add(new(SizeType.Pixel, size));
            return this;
        }
        public UIGrid AddColumnPx(int size) {
            columns.Add(new(SizeType.Pixel, size));
            return this;
        }
        public UIGrid Spacing(int spacing) {
            this.spacing = spacing;
            return this;
        }
        public UIGrid AddComponent(IUIComponent component, int rfrom, int rto, int cfrom, int cto) {
            components.Add(new UIGridComponent(component, rfrom, cfrom, rto, cto));
            return this;
        }
        public UIGrid AddDebugComponents() {
            for (int r = 0; r < rows.Count; r++) {
                for (int c = 0; c < columns.Count; c++) {
                    components.Add(new UIGridComponent(debug, r, c, r, c));
                }
            }
            return this;
        }
        private Vector4 CalculateComponentBounds(UIGridComponent component, Viewport screenSize) {
            float[] r = new float[rows.Count];
            float[] c = new float[columns.Count];
            int rSect = 0;
            int cSect = 0;
            Vector2 size = new(screenSize.Width - spacing, screenSize.Height - spacing);
            for (int i = 0; i < rows.Count; i++) {
                if (rows[i].Type == SizeType.Pixel) {
                    r[i] = rows[i].Size;
                    size.Y -= rows[i].Size;
                } 
                else rSect += rows[i].Size;
                size.Y -= spacing;
            }
            for (int i = 0; i < columns.Count; i++) {
                if (columns[i].Type == SizeType.Pixel) {
                    c[i] = columns[i].Size;
                    size.X -= columns[i].Size;
                } 
                else cSect += columns[i].Size;
                size.X -= spacing;
            }
            for (int i = 0; i < rows.Count; i++) {
                if (rows[i].Type != SizeType.Fraction) continue;
                r[i] = (float)rows[i].Size / rSect * size.Y;
            }
            for (int i = 0; i < columns.Count; i++) {
                if (columns[i].Type != SizeType.Fraction) continue;
                c[i] = (float)columns[i].Size / cSect * size.X;
            }
            Vector4 bounds = new();
            for (int i = 0; i <= component.Rto; i++) {
                if (i >= component.Rfrom) bounds.W += r[i];
                else bounds.Y += r[i];
            }
            for (int i = 0; i <= component.Cto; i++) {
                if (i >= component.Cfrom) bounds.Z += c[i];
                else bounds.X += c[i];
            }
            bounds.X += spacing * (component.Cfrom + 1) + screenSize.X;
            bounds.Y += spacing * (component.Rfrom + 1) + screenSize.Y;
            bounds.Z += spacing * (component.Cto - component.Cfrom);
            bounds.W += spacing * (component.Rto - component.Rfrom);
            return bounds;
        }
        public void Update(Viewport? screenSize = null) {
            foreach (UIGridComponent component in components) {
                component.Component.Update(CalculateComponentBounds(component, screenSize ?? Main.Viewport));
            }
        }
        public void Render(SpriteBatch batch, Viewport? screenSize = null) {
            foreach (UIGridComponent component in components) {
                component.Component.Render(batch, CalculateComponentBounds(component, screenSize ?? Main.Viewport));
            }
        }
    }
}