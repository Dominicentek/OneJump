using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;

namespace OneJump.src {
    public class SFXPlayer {
        private static readonly List<SoundEffectInstance> playing = new();
        public static SoundEffectInstance Play(string path) {
            SoundEffectInstance instance = Assets.GetAsset<SoundEffect>(path).CreateInstance();
            playing.Add(instance);
            instance.Play();
            return instance;
        }
        public static void DisposeFinished() {
            List<SoundEffectInstance> finished = new();
            foreach (SoundEffectInstance instance in playing) {
                if (instance.State == SoundState.Stopped) finished.Add(instance);
            }
            foreach (SoundEffectInstance instance in finished) {
                instance.Dispose();
                playing.Remove(instance);
            }
        }
    }
}