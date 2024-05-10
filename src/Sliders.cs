using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace OneJump.src {
    public abstract class Sliders {
        protected Action after;
        protected static readonly List<Sliders> sliders = new();
        public static readonly Func<float, float> EasingLinear = (float x) => x;
        public static readonly Func<float, float> EasingQuadraticIn = (float x) => x * x;
        public static readonly Func<float, float> EasingQuadraticOut = (float x) => 1 - (1 - x) * (1 - x);
        public static readonly Func<float, float> EasingQuadraticInOut = (float x) => (float)(x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2);
        public static readonly Func<float, float> EasingCubicIn = (float x) => x * x * x;
        public static readonly Func<float, float> EasingCubicOut = (float x) => 1 - (1 - x) * (1 - x) * (1 - x);
        public static readonly Func<float, float> EasingCubicInOut = (float x) => (float)(x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2);
        public static readonly Func<float, float, float, float> FloatInterpolator = (float from, float to, float x) => (to - from) * x + from;
        public static readonly Func<Color, Color, float, Color> ColorInterpolator = (Color from, Color to, float x) => new(
            (byte)((to.R - from.R) * x + from.R),
            (byte)((to.G - from.G) * x + from.G),
            (byte)((to.B - from.B) * x + from.B),
            (byte)((to.A - from.A) * x + from.A)
        );
        public abstract bool UpdateSelf();
        public static void Update() {
            List<Sliders> remove = new();
            foreach (Sliders slider in sliders) {
                if (slider.UpdateSelf()) remove.Add(slider);
            }
            foreach (Sliders slider in remove) {
                slider.after?.Invoke();
                sliders.Remove(slider);
            }
        }
    }
    public class Sliders<P, T> : Sliders {
        private readonly object info;
        private readonly string property;
        private readonly P instance;
        private readonly T from;
        private readonly T to;
        private readonly Func<T, T, float, T> interpolator;
        private readonly Func<float, float> easing;
        private readonly float step;
        private float x;
        private Sliders(
            P instance, object info, string property,
            T from, T to, float duration,
            Func<T, T, float, T> interpolator,
            Func<float, float> easing,
            Action after
        ) {
            this.instance = instance;
            this.info = info;
            this.property = property;
            this.from = from;
            this.to = to;
            this.interpolator = interpolator;
            this.easing = easing;
            this.after = after;
            this.step = 1 / duration;
        }
        public static void Add(
            P instance, string property,
            T to, int duration,
            Func<T, T, float, T> interpolator,
            Func<float, float> easing,
            Action after = null
        ) {
            object info = typeof(P).GetProperty(property);
            info ??= typeof(P).GetField(property);
            if (info == null) return;
            T from = default;
            if (info is PropertyInfo pi) from = (T)pi.GetValue(instance);
            if (info is    FieldInfo fi) from = (T)fi.GetValue(instance);
            foreach (Sliders slider in sliders) {
                if (slider is Sliders<P, T> s) {
                    if (s.property == property) s.x = 1;
                }
            }
            sliders.Add(new Sliders<P, T>(instance, info, property, from, to, duration, interpolator, easing, after));
        }
        private void Apply() {
            if (info is PropertyInfo pi) pi.SetValue(instance, interpolator.Invoke(from, to, easing.Invoke(x)));
            if (info is    FieldInfo fi) fi.SetValue(instance, interpolator.Invoke(from, to, easing.Invoke(x)));
        }
        public override bool UpdateSelf() {
            Apply();
            x += step;
            if (x >= 1) {
                x = 1;
                Apply();
                return true;
            }
            return false;
        }
    }
}