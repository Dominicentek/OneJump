using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneJump.src {
    public class Delay {
        private static readonly List<Delay> delays = new();
        private int delay = 0;
        private readonly Action action;
        private Delay(Action action, int frames) {
            this.action = action;
            this.delay = frames;
        }
        public static void Update() {
            List<Delay> remove = new();
            foreach (Delay delay in delays) {
                delay.delay--;
                if (delay.delay <= 0) remove.Add(delay);
            }
            foreach (Delay delay in remove) {
                delay.action?.Invoke();
                delays.Remove(delay);
            }
        }
        public static void Add(Action action, int frames) {
            delays.Add(new(action, frames));
        }
    }
}